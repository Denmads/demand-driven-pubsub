using Neo4jClient;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    public class Neo4jQueryGrain : Neo4jQueryGrainBase
    {
        IRawGraphClient neo4jClient;

        public Neo4jQueryGrain(IContext context, IGraphClientFactory graphClientFactory) : base(context)
        {
                neo4jClient = (IRawGraphClient)graphClientFactory.CreateAsync().Result;
                neo4jClient.ConnectAsync();
        }

        public override async Task ResolvePublishQuery(PublishQueryInfo request)
        {
            var mqttTopic = GenerateMqttTopic();
            var modifiedCypher = request.CypherQuery + $" SET {request.StreamNode}.dataType = '{request.DataType}' SET {request.StreamNode}.topic = '{mqttTopic}'";

            var query = new Neo4jClient.Cypher.CypherQuery(modifiedCypher, new Dictionary<string, object>(), Neo4jClient.Cypher.CypherResultMode.Set, "neo4j");

            await neo4jClient.ExecuteCypherAsync(query);

            var result = new TopicResponse { RequestId = request.RequestId, Topic = mqttTopic };

            await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                .QueryResult(result, CancellationToken.None);
        }

        private string GenerateMqttTopic()
        {
            return Guid.NewGuid().ToString();
        }

        public override Task ResolveSubscribeQuery(SubscribeQueryInfo request)
        {
            throw new NotImplementedException();
        }

    }
}
