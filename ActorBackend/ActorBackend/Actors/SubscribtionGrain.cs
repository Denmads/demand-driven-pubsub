using ActorBackend.Config;
using ActorBackend.Utils;
using Google.Protobuf.WellKnownTypes;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Proto;
using static ActorBackend.Actors.SubscriptionQueryResponse.Types;

namespace ActorBackend.Actors
{
    public class SubscribtionGrain : SubscribtionGrainBase
    {
        private AppConfig config;
        private IMqttClient mqttClient;

        private string clientId;
        private string clientActorIdentity;
        private string subscribtionId;

        private List<DataSet> dataSets = new List<DataSet>();

        public SubscribtionGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());
        }

        public override Task Create(SubscriptionGrainCreateInfo request)
        {
            clientId = request.ClientId;
            clientActorIdentity = request.ClientActorIdentity;
            subscribtionId = request.SubscribtionId;
            

            foreach (var collection in request.Query.NodeCollections)
            {
                var dataset = new DataSet(collection, mqttClient, request.SubscriptionTopic, subscribtionId);
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
    }

    internal class DataSet
    {
        private DataNodeCollection nodeCollection;
        private Dictionary<string, string> lastValues = new Dictionary<string, string>();

        private IMqttClient mqttClient;

        private string sendTopic;
        private string subId;

        public DataSet(DataNodeCollection nodeCollection, IMqttClient mqttClient, string topic, string subId)
        {
            this.nodeCollection = nodeCollection;
            this.mqttClient = mqttClient;
            this.sendTopic = topic;
            this.subId = subId;

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

                    SendUpdatedDataSet();

                    break;
                }
            }

            return Task.CompletedTask;
        }

        private void SendUpdatedDataSet()
        {
            var dataJson = new { SubscriptionId = subId, Data = new Dictionary<string, DataNode>()};

            foreach (var dataNode in nodeCollection.Nodes)
            {
                var node = new DataNode { Value = lastValues.GetValueOrDefault(dataNode.Key, ""), DataType = dataNode.Value.DataType };
                dataJson.Data.Add(dataNode.Key, node);
            }

            var queryResponse = $"´data-set<>{JsonConvert.SerializeObject(dataJson)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(sendTopic)
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            

            mqttClient.PublishAsync(applicationMessage);
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

        class DataNode
        {
            public string Value { get; set; }
            public string DataType { get; set; }
        }
    }
}
