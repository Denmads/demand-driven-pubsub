using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
using System.Text;
using Newtonsoft.Json;
using ActorBackend.Data;
using ActorBackend.Utils;
using ActorBackend.Transformations;

namespace ActorBackend.Actors.Client
{
    public partial class ClientGrain : ClientGrainBase
    {
        private Dictionary<int, string> pendingPubQueries = new Dictionary<int, string>();
        private Dictionary<int, SubscribeQuery> pendingSubQueries = new Dictionary<int, SubscribeQuery>();
        private Dictionary<string, PublishState> publishes = new Dictionary<string, PublishState>();
        private Dictionary<string, string> subscribeTopics = new Dictionary<string, string>();

        private Task HandlePublishQuery(string message)
        {
            PublishQuery query = JsonConvert.DeserializeObject<PublishQuery>(message)!;

            // If the publish already exists
            var publish = publishes.FirstOrDefault(pair => pair.Value.PublishId == query.PublishId);
            if (!publish.Equals(new KeyValuePair<string, PublishState>()))
            {
                var applicationMessage = CreateQueryExistsResponseMessage(query.RequestId);
                mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                return Task.CompletedTask;
            }

            pendingPubQueries.Add(query.RequestId, query.PublishId);

            var publishQuery = new PublishQueryInfo
            {
                Info = new RequestInfo
                {
                    ClientActorIdentity = identity.Identity,
                    RequestId = query.RequestId,
                },
                CypherQuery = query.CypherQuery,
                StreamNode = query.TargetNode,
                DataType = query.DataType
            };
            publishQuery.Roles.AddRange(query.Roles);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queryResolver.ResolveQuery(new Neo4jQuery { PublishInfo = publishQuery }, CancellationToken.None);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        
        private Task HandleSubcribeQuery(string message)
        {
            SubscribeQuery query = JsonConvert.DeserializeObject<SubscribeQuery>(message)!;

            // If a subscription already exists
            var subscription = subscribeTopics.FirstOrDefault(s => s.Key == query.SubscriptionId);
            if (!subscription.Equals(new KeyValuePair<string, string>()))
            {
                var applicationMessage = CreateQueryExistsResponseMessage(query.RequestId);
                mqttClient.PublishAsync(applicationMessage, CancellationToken.None);
                return Task.CompletedTask;
            }


            var user = query.Account == null ? new User() : new User {
                Username = query.Account,
                Password = PasswordUtil.DecodeBase64(query.AccountPassword)
            };

            var queryInfo = new SubscribeQueryInfo
            {
                Info = new RequestInfo
                {
                    ClientActorIdentity = identity.Identity,
                    RequestId = query.RequestId,
                    Operator = user
                },
                CypherQuery = query.CypherQuery
            };

            query.TargetNodes.ForEach((n) =>
            {
                queryInfo.TargetNodes.Add(n);

            });

            pendingSubQueries.Add(query.RequestId, query);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queryResolver.ResolveQuery(new Neo4jQuery { SubscribeInfo = queryInfo }, CancellationToken.None);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        private MqttApplicationMessage CreateMqttResponseMessage(string messageType, object json, string topic)
        {
            var queryResponse = $"{messageType}<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(topic)
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            return applicationMessage;
        }

        private MqttApplicationMessage CreateQueryExistsResponseMessage(int requestId)
        {
            var json = new
            {
                RequestId = requestId
            };

            return CreateMqttResponseMessage(
                ResponseMessageType.QUERY_EXISTS, json, 
                MqttTopicHelper.ClientResponse(clientId!)
            );
        }

        private MqttApplicationMessage CreateQuerySuccessResponseMessage(int requestId)
        {
            var json = new
            {
                RequestId = requestId
            };

            return CreateMqttResponseMessage(
                ResponseMessageType.QUERY_SUCCESS, json,
                MqttTopicHelper.ClientResponse(clientId!)
            );
        }

        private MqttApplicationMessage CreateQueryResponseMessage(int requestId, string topic)
        {
            var json = new {
                RequestId = requestId,
                Topic = topic
            };

            return CreateMqttResponseMessage(
                ResponseMessageType.QUERY_RESULT, json,
                MqttTopicHelper.ClientResponse(clientId!)
            );
        }

        private MqttApplicationMessage CreateQueryErrorResponseMessage(int requestId, ErrorResponse response)
        {
            var json = new
            {
                RequestId = requestId,
                response.Message
            };

            return CreateMqttResponseMessage(
                ResponseMessageType.QUERY_ERROR, json,
                MqttTopicHelper.ClientResponse(clientId!)
            );
        }

        public override Task QueryResult(QueryResponse request)
        {
            MqttApplicationMessage applicationMessage = null;
            if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.PublishResponse)
            {
                HandlePublishResponse(request.RequestId, request.PublishResponse);
                applicationMessage = CreateQueryResponseMessage(request.RequestId, request.PublishResponse.Topic);
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.SubscribeResponse)
            {
                string subscribeTopic = MqttTopicHelper.GenerateMqttTopic();
                var success = HandleSubscribeResponse(request.RequestId, request.SubscribeResponse, subscribeTopic);

                if (success)
                    applicationMessage = CreateQueryResponseMessage(request.RequestId, subscribeTopic);
                else
                    applicationMessage = CreateQueryErrorResponseMessage(request.RequestId, new ErrorResponse { Message = "An error occurred while trying to execute the subscribe query." });
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.ErrorResponse)
            {
                applicationMessage = CreateQueryErrorResponseMessage(request.RequestId, request.ErrorResponse);
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.SuccessResponse)
            {
                applicationMessage = CreateQuerySuccessResponseMessage(request.RequestId);
            }

            mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            return Task.CompletedTask;
        }

        private void HandlePublishResponse(int requestId, PublishQueryResponse response)
        {
            publishes[response.Topic] = new PublishState(clientId!, mqttClient, pendingPubQueries[requestId], Context);
            pendingPubQueries.Remove(requestId);
        }

        private bool HandleSubscribeResponse(int requestId, SubscriptionQueryResponse response, string topic)
        {
            var subQuery = pendingSubQueries!.GetValueOrDefault(requestId, null);
            if (subQuery == null)
            {
                return false;
            }

            subscribeTopics[subQuery.SubscriptionId] = topic;
            pendingSubQueries.Remove(requestId);


            var info = new SubscriptionGrainCreateInfo
            {
                ClientActorIdentity = Context.ClusterIdentity()!.Identity,
                ClientId = clientId,
                SubscribtionId = subQuery.SubscriptionId,
                SubscriptionTopic = topic,
                Query = response,
                QueryInfo = response.Query
            };

            if (subQuery.Transformations != null)
            {
                info.Transformations = subQuery.Transformations.ToSpecification();
            }
            else
            {
                info.Transformations = null;
            }

            string subGrainId = $"{clientId}.{subQuery.SubscriptionId}";
            Context.Cluster().GetSubscribtionGrain(subGrainId).Create(info, CancellationToken.None);

            return true;
        }
    }

    internal class ResponseMessageType
    {
        public const string QUERY_RESULT = "query-result";
        public const string QUERY_SUCCESS = "query-success";
        public const string QUERY_EXISTS = "query-exists";
        public const string QUERY_ERROR = "query-error";
    }
}
