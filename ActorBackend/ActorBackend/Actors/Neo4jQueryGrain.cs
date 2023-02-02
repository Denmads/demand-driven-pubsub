using ActorBackend.Data;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    using SubscribtionQueryResult = List<Dictionary<string, StreamNode>>;

    public class Neo4jQueryGrain : Neo4jQueryGrainBase
    {
        private Neo4j.Driver.ISession neo4jSession;

        public Neo4jQueryGrain(IContext context, IDriver neo4jDriver) : base(context)
        {
            neo4jSession = neo4jDriver.Session();
        }

        public override async Task ResolvePublishQuery(PublishQueryInfo request)
        {
            var mqttTopic = MqttTopicHelper.GenerateMqttTopic();
            var modifiedCypher = request.CypherQuery + $" SET {request.StreamNode}.dataType = '{request.DataType}' SET {request.StreamNode}.topic = '{mqttTopic}'";

            neo4jSession.ExecuteWrite<object>(tx =>
            {
                tx.Run(modifiedCypher);
                return null;
            });

            var result = new TopicResponse { RequestId = request.RequestId };
            result.Topics.Add(mqttTopic);

            await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                .QueryResult(result, CancellationToken.None);
        }

        

        public override Task ResolveSubscribeQuery(SubscribeQueryInfo request)
        {
            
            var modifiedCypher = request.CypherQuery + " RETURN " + String.Join(", ", request.TargetNodes.ToArray());

            var result = neo4jSession.ExecuteRead<SubscribtionQueryResult>(tx =>
            {
                var result = tx.Run(modifiedCypher);

                SubscribtionQueryResult res = new SubscribtionQueryResult();
                foreach (var record in result)
                {
                    Dictionary<string, StreamNode> nodes = new Dictionary<string, StreamNode>();
                    foreach (var node in record.Values)
                    {
                        var n = node.Value as INode;
                        nodes.Add(node.Key, new StreamNode { Topic = n!.Properties["topic"].As<string>(), DataType = n.Properties["dataType"].As<string>() });
                    }
                    res.Add(nodes);
                }

                return res;
            });

            
            var logger = Proto.Log.CreateLogger<Neo4jQueryGrain>();
            result.ForEach(dict =>
            {
                foreach (var kvp in dict)
                {
                    logger.LogInformation(kvp.ToString());
                }
            });

            return Task.CompletedTask;
        }

    }
}
