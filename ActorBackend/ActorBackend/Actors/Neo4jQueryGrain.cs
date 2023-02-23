using ActorBackend.Data;
using ActorBackend.Utils;
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
            try
            {
                var mqttTopic = MqttTopicHelper.GenerateMqttTopic();
                var modifiedCypher = request.CypherQuery + $" SET {request.StreamNode}.dataType = '{request.DataType}' SET {request.StreamNode}.topic = '{mqttTopic}'";

                neo4jSession.ExecuteWrite<object>(tx =>
                {
                    tx.Run(modifiedCypher);
                    return null;
                });

                var result = new PublishQueryResponse { Topic = mqttTopic };
            
                await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                    .QueryResult(new QueryResponse { RequestId = request.RequestId, PublishResponse = result}, CancellationToken.None);

            }
            catch (Exception ex)
            {
                var error = new ErrorResponse { Message = ex.Message };
                await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                    .QueryResult(new QueryResponse { RequestId = request.RequestId, ErrorResponse = error }, CancellationToken.None);
            }
            finally
            {
                await Context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER)
                    .QueryResolved(new QueryResolvedResponse { QueryActorIdentity = Context.ClusterIdentity()!.Identity }, CancellationToken.None);
            }
        }

        

        public override async Task ResolveSubscribeQuery(SubscribeQueryInfo request)
        {
            try {
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

                var queryResult = new SubscriptionQueryResponse();
                result.ForEach(dict =>
                {
                    var collection = new SubscriptionQueryResponse.Types.DataNodeCollection();
                    foreach (var node in dict)
                    {
                        collection.Nodes.Add(node.Key, new SubscriptionQueryResponse.Types.DataNode { Topic = node.Value.Topic, DataType = node.Value.DataType });
                    }

                    queryResult.NodeCollections.Add(collection);
                });


                await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                    .QueryResult(new QueryResponse { RequestId = request.RequestId, SubscribeResponse = queryResult}, CancellationToken.None);
            }
            catch (Exception ex)
            {
                var error = new ErrorResponse { Message = ex.Message };
                await Context.Cluster().GetClientGrain(request.ClientActorIdentity)
                    .QueryResult(new QueryResponse { RequestId = request.RequestId, ErrorResponse = error }, CancellationToken.None);
            }
            finally
            {
                await Context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER)
                    .QueryResolved(new QueryResolvedResponse { QueryActorIdentity = Context.ClusterIdentity()!.Identity }, CancellationToken.None);
            }
        }

    }
}
