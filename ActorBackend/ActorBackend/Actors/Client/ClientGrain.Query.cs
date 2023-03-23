﻿using ActorBackend.Config;
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
        private Dictionary<string, PublishState> publishes = new Dictionary<string, PublishState>();
        private Dictionary<string, string> subscribeTopics = new Dictionary<string, string>();

        private Task HandlePublishQuery(string message)
        {
            PublishQuery query = JsonConvert.DeserializeObject<PublishQuery>(message)!;

            pendingQueries.Add(query.RequestId, query.PublishId);

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

            var queryInfo = new SubscribeQueryInfo
            {
                Info = new RequestInfo
                {
                    ClientActorIdentity = identity.Identity,
                    RequestId = query.RequestId,
                    Operator = new User { 
                        Username = query.Account,
                        Password = query.AccountPassword
                    }
                },
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

        private MqttApplicationMessage CreateQueryErrorResponseMessage(int requestId, ErrorResponse response)
        {
            var json = new
            {
                RequestId = requestId,
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

        private MqttApplicationMessage CreateQuerySuccessResponseMessage(int requestId)
        {
            var json = new
            {
                RequestId = requestId
            };

            var queryResponse = $"query-success<>{JsonConvert.SerializeObject(json)}";

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
                var success = HandleSubscribeResponse(request.RequestId, request.SubscribeResponse, subscribeTopic);

                if (success)
                    applicationMessage = CreateQueryResponseMessage(request, subscribeTopic);
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
            publishes[response.Topic] = new PublishState(clientId!, mqttClient, pendingQueries[requestId], Context);
            pendingQueries.Remove(requestId);
        }

        private bool HandleSubscribeResponse(int requestId, SubscriptionQueryResponse response, string topic)
        {
            var subId = pendingQueries.GetValueOrDefault(requestId, "");
            if (subId == null)
            {
                return false;
            }

            subscribeTopics[pendingQueries[requestId]] = topic;
            pendingQueries.Remove(requestId);

            string subGrainId = $"{clientId}.{subId}";

            Context.Cluster().GetSubscribtionGrain(subGrainId).Create(
                new SubscriptionGrainCreateInfo { ClientActorIdentity = Context.ClusterIdentity()!.Identity,
                                                  ClientId = clientId, SubscribtionId = subId, SubscriptionTopic = topic, Query = response},
                CancellationToken.None
            );

            return true;
        }
    }
}
