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

namespace ActorBackend.Actors.Client
{
    public partial class ClientGrain : ClientGrainBase
    {
        private Dictionary<int, string> pendingQueries = new Dictionary<int, string>();
        private Dictionary<string, string> publishTopics = new Dictionary<string, string>();
        private Dictionary<string, string> subscribeTopics = new Dictionary<string, string>();

        private Task HandlePublishQuery(string message)
        {
            PublishQuery query = JsonConvert.DeserializeObject<PublishQuery>(message)!;

            pendingQueries.Add(query.RequestId, query.PublishId);

            var publishQuery = new PublishQueryInfo
            {
                ClientActorIdentity = identity.Identity,
                RequestId = query.RequestId,
                CypherQuery = query.CypherQuery,
                StreamNode = query.TargetNode,
                DataType = query.DataType
            };
#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queryResolver.ResolveQuery(new Neo4jQuery { PublishInfo = publishQuery }, CancellationToken.None);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        
        private Task HandleSubcribeQuery(string message)
        {
            SubscribeQuery query = JsonConvert.DeserializeObject<SubscribeQuery>(message)!;

            var queryInfo = new SubscribeQueryInfo
            {
                ClientActorIdentity = identity.Identity,
                RequestId = query.RequestId,
                CypherQuery = query.CypherQuery
            };
            query.TargetNodes.ForEach((n) =>
            {
                queryInfo.TargetNodes.Add(n);
            });

            pendingQueries.Add(query.RequestId, query.SubscriptionId);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queryResolver.ResolveQuery(new Neo4jQuery { SubscribeInfo = queryInfo }, CancellationToken.None);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        private MqttApplicationMessage CreateQueryResponseMessage(QueryResponse response, string topic)
        {
            var json = new {
                response.RequestId,
                Topic = topic
            };

            var queryResponse = $"query-result<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            return applicationMessage;
        }

        private MqttApplicationMessage CreateQueryErrorResponseMessage(ErrorResponse response)
        {
            var json = new
            {
                response.Message
            };

            var queryResponse = $"query-error<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            return applicationMessage;
        }

        public override Task QueryResult(QueryResponse request)
        {
            MqttApplicationMessage applicationMessage = null;
            if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.PublishResponse)
            {
                HandlePublishResponse(request.RequestId, request.PublishResponse);
                applicationMessage = CreateQueryResponseMessage(request, request.PublishResponse.Topic);
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.SubscribeResponse)
            {
                string subscribeTopic = MqttTopicHelper.GenerateMqttTopic();
                HandleSubscribeResponse(request.RequestId, request.SubscribeResponse, subscribeTopic);
                applicationMessage = CreateQueryResponseMessage(request, subscribeTopic);
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.ErrorResponse)
            {
                applicationMessage = CreateQueryErrorResponseMessage(request.ErrorResponse);
            }

            mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            return Task.CompletedTask;
        }

        private void HandlePublishResponse(int requestId, PublishQueryResponse response)
        {
            publishTopics[pendingQueries[requestId]] = response.Topic;
            pendingQueries.Remove(requestId);
        }

        private void HandleSubscribeResponse(int requestId, SubscriptionQueryResponse response, string topic)
        {
            var subId = pendingQueries.GetValueOrDefault(requestId, "");
            if (subId == null)
            {
                //What to do
            }

            subscribeTopics[pendingQueries[requestId]] = topic;
            pendingQueries.Remove(requestId);

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Context.Cluster().GetSubscribtionGrain(subId!).Create(
                new SubscriptionGrainCreateInfo { ClientActorIdentity = Context.ClusterIdentity()!.Identity,
                                                  ClientId = clientId, SubscribtionId = subId, SubscriptionTopic = topic, Query = response},
                CancellationToken.None
            );
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}
