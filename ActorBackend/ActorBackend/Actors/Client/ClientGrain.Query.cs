using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
using System.Text;
using Newtonsoft.Json;
using ActorBackend.Data;

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

#pragma warning disable CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed
            queryResolver.ResolveQuery(new Neo4jQuery { SubscribeInfo = queryInfo }, CancellationToken.None);
#pragma warning restore CS4014 // Because this call is not awaited, execution of the current method continues before the call is completed

            return Task.CompletedTask;
        }

        private MqttApplicationMessage CreateQueryResponseMessage(TopicResponse response)
        {
            var json = new {
                response.RequestId,
                response.Topic
            };

            var queryResponse = $"query-result<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId!))
                .WithPayload(queryResponse)
                .Build();
            return applicationMessage;
        }

        public override Task QueryResult(TopicResponse request)
        {
            MqttApplicationMessage applicationMessage = CreateQueryResponseMessage(request);
            mqttClient.PublishAsync(applicationMessage, CancellationToken.None);

            return Task.CompletedTask;
        }
    }
}
