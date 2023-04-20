using ActorBackend.Data;
using ActorBackend.Utils;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;

namespace ActorBackend.Actors.Neo4jGrain
{
    using SubscribtionQueryResult = List<Dictionary<string, StreamNode>>;

    public partial class Neo4jQueryGrain : Neo4jQueryGrainBase
    {

        public async Task ResolvePublishQuery(PublishQueryInfo request)
        {
            var mqttTopic = MqttTopicHelper.GenerateMqttTopic();
            var modifiedCypher = request.CypherQuery + $" SET {request.StreamNode}.dataType = '{request.DataType}'";
            modifiedCypher += $" SET {request.StreamNode}.topic = '{mqttTopic}'";
            modifiedCypher += $" SET {request.StreamNode}.actor = '{request.Info.ClientActorIdentity}'";
            modifiedCypher += $" SET {request.StreamNode}.roles = '{string.Join(";", request.Roles)}'";

            ExecuteCypher(modifiedCypher, write: true);


            var result = new PublishQueryResponse { Topic = mqttTopic };
            await Context.Cluster().GetClientGrain(request.Info.ClientActorIdentity)
                .QueryResult(new QueryResponse { RequestId = request.Info.RequestId, PublishResponse = result }, CancellationToken.None);

            await protoPublisher.Publish("metadata-update", new MetaDataUpdate());
        }

        public async Task ResolveSubscribeQuery(SubscribeQueryInfo request, bool rerun)
        {
            var modifiedCypher = request.CypherQuery + " RETURN " + string.Join(", ", request.TargetNodes.ToArray());

            var result = ExecuteCypher(modifiedCypher);

            SubscribtionQueryResult res = new SubscribtionQueryResult();
            foreach (var record in result!)
            {
                if (!HasRequiredRoles(request.Info.Operator, record))
                    continue; //The user does not have all rights required for this dataset, then skip it

                Dictionary<string, StreamNode> nodes = new Dictionary<string, StreamNode>();
                foreach (var node in record.Values)
                {
                    var n = node.Value as INode;
                    nodes.Add(node.Key, new StreamNode
                    {
                        Topic = n!.Properties["topic"].As<string>(),
                        DataType = n.Properties["dataType"].As<string>(),
                        OwningClientActorIdentity = n!.Properties["actor"].As<string>()
                    });
                }
                res.Add(nodes);
            }

            if (res.Count > 1)
                EnsureSameDataSchemaAcrossDataSets(res);

            var queryResult = new SubscriptionQueryResponse() { Query = request};
            res.ForEach(dict =>
            {
                var collection = new SubscriptionQueryResponse.Types.DataNodeCollection();
                foreach (var node in dict)
                {
                    collection.Nodes.Add(node.Key, new SubscriptionQueryResponse.Types.DataNode
                    {
                        Topic = node.Value.Topic,
                        DataType = node.Value.DataType,
                        OwningActorIdentity = node.Value.OwningClientActorIdentity
                    });
                }

                queryResult.NodeCollections.Add(collection);
            });

            

            var response = new QueryResponse { RequestId = request.Info.RequestId, SubscribeResponse = queryResult };
            if (rerun)
                await Context.Cluster().GetSubscribtionGrain(request.Info.ClientActorIdentity)
                    .QueryResult(response, CancellationToken.None);
            else
                await Context.Cluster().GetClientGrain(request.Info.ClientActorIdentity)
                    .QueryResult(response, CancellationToken.None);
        }

        private bool HasRequiredRoles(User user, IRecord record)
        {
            var nodes = record.Values;
            var roleLists = from n in nodes
                            select ((INode)n.Value).Properties["roles"].As<string>().Split(";").ToList();

            var requiredRoles = roleLists.SelectMany(x => x)
                                    .Where(r => r.Length > 0)
                                    .ToArray();

            return HasRoles(user, requiredRoles);
        }

        private void EnsureSameDataSchemaAcrossDataSets(SubscribtionQueryResult res)
        {
            var firstDataset = res[0];

            foreach (var kvp in firstDataset)
            {
                var allSameType = res.All(ds =>
                {
                    return ds[kvp.Key].DataType == kvp.Value.DataType;
                });

                if (!allSameType)
                    throw new InvalidDataException($"All datatypes for the key '{kvp.Key}' is not the same.");
            }
        }
    }
}
