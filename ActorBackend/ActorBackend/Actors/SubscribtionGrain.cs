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
        }

        public override Task Create(SubscriptionGrainCreateInfo request)
        {
            clientId = request.ClientId;
            clientActorIdentity = request.ClientActorIdentity;
            subscribtionId = request.SubscribtionId;

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());

            foreach (var dataNodeCollection in request.Query.NodeCollections)
            {
                SetupCollection(request.Query, request.SubscribtionId);
            }


            return Task.CompletedTask;
        }

        private void SetupCollection(SubscriptionQueryResponse response, string topic)
        {

            foreach (var collection in response.NodeCollections)
            {
                dataSets.Add(new DataSet(collection, mqttClient, topic, subscribtionId));
            }
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
                    lastValues.Add(dataNode.Key, value);

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
                .Build();
            

            mqttClient.PublishAsync(applicationMessage);
        }

        class DataNode
        {
            public string Value { get; set; }
            public string DataType { get; set; }
        }
    }
}
