﻿using ActorBackend.Data;
using ActorBackend.Utils;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors.Neo4jGrain
{
    using SubscribtionQueryResult = List<Dictionary<string, StreamNode>>;

    public partial class Neo4jQueryGrain : Neo4jQueryGrainBase
    {
        private Neo4j.Driver.ISession neo4jSession;

        public Neo4jQueryGrain(IContext context, IDriver neo4jDriver) : base(context)
        {
            neo4jSession = neo4jDriver.Session();
        }

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
        }

        public async Task ResolveSubscribeQuery(SubscribeQueryInfo request)
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

            var queryResult = new SubscriptionQueryResponse();
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


            await Context.Cluster().GetClientGrain(request.Info.ClientActorIdentity)
                .QueryResult(new QueryResponse { RequestId = request.Info.RequestId, SubscribeResponse = queryResult }, CancellationToken.None);
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
    }
}
