using ActorBackend.Config;
using ActorBackend.Data;
using ActorBackend.Utils;
using Google.Protobuf.WellKnownTypes;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Proto;
using Proto.Cluster;
using Proto.Cluster.PubSub;
using System.Data;
using static ActorBackend.Actors.SubscriptionQueryResponse.Types;

namespace ActorBackend.Actors
{
    public class SubscribtionGrain : SubscribtionGrainBase
    {
        private const string UPDATE_TOPIC = "metadata-update";

        private AppConfig config;
        private IMqttClient mqttClient;

        private SubscribeQueryInfo queryInfo;

        private string clientId;
        private string clientActorIdentity;
        private string subscribtionId;
        private string subscriptionTopic;

        private TransformerGrainClient? transformerGrain = null;

        private List<DataSet> dataSets = new List<DataSet>();

        public SubscribtionGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());
        }

        public override Task OnStarted()
        {
            Context.Cluster().Subscribe(UPDATE_TOPIC, Context.ClusterIdentity()!);
            return base.OnStarted();
        }

        public override Task OnStopping()
        {
            Context.Cluster().Unsubscribe(UPDATE_TOPIC, Context.ClusterIdentity()!);
            return base.OnStopping();
        }

        public override async Task OnReceive()
        {
            if (Context.Message is MetaDataUpdate)
            {
                queryInfo.Info.ClientActorIdentity = Context.ClusterIdentity()!.Identity;
                var neo4jQuery = new Neo4jQuery { SubscribeInfo = queryInfo, Rerun = true };
                await Context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER)
                    .ResolveQuery(neo4jQuery, CancellationToken.None);
            }
        }

        public override Task Create(SubscriptionGrainCreateInfo request)
        {
            clientId = request.ClientId;
            clientActorIdentity = request.ClientActorIdentity;
            subscribtionId = request.SubscribtionId;
            queryInfo = request.QueryInfo;
            subscriptionTopic = request.SubscriptionTopic;
            
            if (request.Transformations != null)
            {
                transformerGrain = Context.Cluster().GetTransformerGrain(Context.ClusterIdentity()!.Identity + ".Transformer");
                transformerGrain.Create(
                    new TransformerGrainCreateInfo { 
                        Transformations = request.Transformations, 
                        NodeCollection = request.Query.NodeCollections.ElementAt(0) 
                    },
                    CancellationToken.None
                    );
            }


            foreach (var collection in request.Query.NodeCollections)
            {
                var dataset = new DataSet(collection, mqttClient, request.SubscriptionTopic, subscribtionId, transformerGrain);
                dataset.NotifyOfDependentStateChange(clientActorIdentity, true, Context);

                dataSets.Add(dataset);
            }

            return Task.CompletedTask;
        }

        public override Task NotifyDependenciesOfStateChange(DependentStateChangedMessage request)
        {
            foreach (var dataSet in dataSets)
            {
                dataSet.NotifyOfDependentStateChange(clientActorIdentity, request.State == State.Alive, Context);
            }

            return Task.CompletedTask;
        }

        public override Task QueryResult(QueryResponse request)
        {
            //Gets called when the metadata model updates and the subscribtion reruns the query

            //Check if any existing datasets should be removed
            List<DataSet> toRemove = new List<DataSet>();
            foreach (var ds in dataSets)
            {
                if (request.SubscribeResponse.NodeCollections.All(ns => !ds.MatchesCollection(ns))) {
                    toRemove.Add(ds);
                }
            }
            toRemove.ForEach(ds => dataSets.Remove(ds));


            //Checks if any collection does not exist
            foreach (var collection in request.SubscribeResponse.NodeCollections)
            {
                var exists = dataSets.Any(ds => ds.MatchesCollection(collection));
                if (!exists)
                {
                    var dataset = new DataSet(collection, mqttClient, subscriptionTopic, subscribtionId, transformerGrain);
                    dataset.NotifyOfDependentStateChange(clientActorIdentity, true, Context);
                    dataSets.Add(dataset);
                }
            }


            return Task.CompletedTask;
        }
    }

    internal class DataSet
    {
        private DataNodeCollection nodeCollection;
        private Dictionary<string, string> lastValues = new Dictionary<string, string>();

        private IMqttClient mqttClient;

        private string sendTopic;
        private string subId;

        private TransformerGrainClient? transformer;

        public DataSet(DataNodeCollection nodeCollection, IMqttClient mqttClient, string topic, string subId, TransformerGrainClient? transformer)
        {
            this.nodeCollection = nodeCollection;
            this.mqttClient = mqttClient;
            this.sendTopic = topic;
            this.subId = subId;
            this.transformer = transformer;

            mqttClient.ApplicationMessageReceivedAsync += MessageReceived;

            foreach (var dataNode in nodeCollection.Nodes)
            {
                mqttClient.SubscribeAsync(dataNode.Value.Topic);
            }
        }

        private Task MessageReceived(MqttApplicationMessageReceivedEventArgs args)
        {
            var value = args.ApplicationMessage.ConvertPayloadToString();

            foreach (var dataNode in nodeCollection.Nodes)
            {
                if (args.ApplicationMessage.Topic == dataNode.Value.Topic)
                {
                    lastValues[dataNode.Key] = value;

                    SendUpdatedDataSet(dataNode.Key);

                    break;
                }
            }

            return Task.CompletedTask;
        }

        private async void SendUpdatedDataSet(string changedNode)
        {
            var dataJson = new { 
                SubscriptionId = subId,
                ChangedNode = changedNode,
                Data = new Dictionary<string, DataNode>()
            };
            foreach (var dataNode in nodeCollection.Nodes)
            {
                var node = new DataNode { Value = lastValues.GetValueOrDefault(dataNode.Key, ""), DataType = dataNode.Value.DataType };
                dataJson.Data.Add(dataNode.Key, node);
            }

            if (transformer != null)
            {
                var tData = new Data();
                foreach (var kvp in dataJson.Data)
                {
                    tData.Data_.Add(kvp.Key, kvp.Value.ToProtoNode());
                }

                var transformedData = await transformer.TransformData(tData, CancellationToken.None)!;
                dataJson.Data.Clear();

                foreach (var kvp in transformedData!.Data_)
                {
                    dataJson.Data.Add(kvp.Key, DataNode.FromProtoNode(kvp.Value));
                }
            }

            var queryResponse = $"data-set<>{JsonConvert.SerializeObject(dataJson)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(sendTopic)
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            

            await mqttClient.PublishAsync(applicationMessage);
        }

        public void NotifyOfDependentStateChange(string clientActorIdentity, bool alive, IContext context)
        {
            var needToNotify = from n in nodeCollection.Nodes 
                               group n by new { OwningClient = n.Value.OwningActorIdentity, Topic=n.Value.Topic} into g
                               select new {g.Key.OwningClient, g.Key.Topic};

            foreach (var notifyInfo in needToNotify)
            {
                var message = new DependencyMessage { ClientActorIdentity = clientActorIdentity, PublishTopic= notifyInfo.Topic, SubscriptionId = subId};

                if (alive)
                    context.GetClientGrain(notifyInfo.OwningClient).StartPublishDependency(message, CancellationToken.None);
                else
                    context.GetClientGrain(notifyInfo.OwningClient).StopPublishDependency(message, CancellationToken.None);
            }
        }

        public bool MatchesCollection(DataNodeCollection other)
        {
            var topics = nodeCollection.Nodes.Select(nd => nd.Value.Topic);
            return other.Nodes.All(n => topics.Contains(n.Value.Topic));
        }

        class DataNode
        {

            public string Value { get; set; }
            public string DataType { get; set; }



            public Data.Types.Node ToProtoNode()
            {
                return new Data.Types.Node { DataType = DataType, Value = Value };
            }

            public static DataNode FromProtoNode(Data.Types.Node node)
            {
                return new DataNode { DataType = node.DataType, Value = node.Value };
            }
        }
    }
}
