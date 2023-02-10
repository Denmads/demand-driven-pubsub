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

        private Task HandlePublishQuery(string message)
        {
            PublishQuery query = JsonConvert.DeserializeObject<PublishQuery>(message)!;

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

        private Dictionary<int, string> pendingQueries = new Dictionary<int, string>();
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

            pendingQueries.Add(query.RequestId, query.SubscribtionId);

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

        public override Task QueryResult(QueryResponse request)
        {
            string subscribeTopic = MqttTopicHelper.GenerateMqttTopic();
            if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.PublishResponse)
            {
                HandlePublishResponse(request.PublishResponse);
            }
            else if (request.QueryTypeCase == QueryResponse.QueryTypeOneofCase.SubscribeResponse)
            {
                HandleSubscribeResponse(request, subscribeTopic);
            }

            MqttApplicationMessage applicationMessage = CreateQueryResponseMessage(request, subscribeTopic);
            mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            return Task.CompletedTask;
        }

        private void HandlePublishResponse(PublishQueryResponse response)
        {

        }

        private void HandleSubscribeResponse(QueryResponse response, string topic)
        {
            var subId = pendingQueries.GetValueOrDefault(response.RequestId, "");
            if (subId == null)
            {
                //What to do
            }

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            Context.Cluster().GetSubscribtionGrain(subId!).Create(
                new SubscriptionGrainCreateInfo { ClientActorIdentity = Context.ClusterIdentity()!.Identity,
                                                  ClientId = clientId, SubscribtionId = subId, SubscriptionTopic = topic, Query = response.SubscribeResponse},
                CancellationToken.None
            );
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
        }
    }
}
