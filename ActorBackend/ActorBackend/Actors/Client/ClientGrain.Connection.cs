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

        private AppConfig config;
        private IMqttClient mqttClient;

        private ClusterIdentity identity;

        private string? clientId = null;
        private ClientConnectionState connectionState;

        private ILogger logger;
        private QueryResolverGrainClient queryResolver;

        private bool created = false;
        private int heartbeatInterval = 0;

        public ClientGrain(IContext context, ClusterIdentity identity, AppConfig config) : base(context)
        {
            this.config = config;
            this.identity = identity;

            queryResolver = context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER);

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());
        }

        public override Task OnStopping()
        {
            var json = new
            {
                Reason="Server Stopped"
            };

            var queryResponse = $"disconnect<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            mqttClient.PublishAsync(applicationMessage);
            mqttClient.Dispose();

            return base.OnStopping();
        }

        public override Task Connect(ClientConnectInfo request)
        {
            string messageType = "connect-ack";
            //var json;
            object json;
            if (!created)
            {
                created = true;
                clientId = request.ClientId;
                logger = Log.CreateLogger($"client/{clientId}");
                connectionState = new ClientConnectionState(mqttClient, request.ConnectionTimeout, clientId);
                connectionState.onConnectionDied = () =>
                {
                    logger.LogInformation($"connection died");
                    
                    //Notify dependents
                    foreach (var publish in publishes)
                    {
                        publish.Value.NotifyOfClientConnectionStateChange(false);
                    }

                    //Notify dependencies
                    foreach (var subId in subscribeTopics.Keys)
                    {
                        string subGrainId = $"{clientId}.{subId}";
                        var message = new DependentStateChangedMessage { State = State.Dead };
                        Context.GetSubscribtionGrain(subGrainId).NotifyDependenciesOfStateChange(message, CancellationToken.None);
                    }
                };

                connectionState.onConnectionResurrected = () =>
                {
                    logger.LogInformation($"connection ressurected");

                    //Notify dependents
                    foreach (var publish in publishes)
                    {
                        publish.Value.NotifyOfClientConnectionStateChange(false);
                    }

                    //Notify dependencies
                    foreach (var subId in subscribeTopics.Keys)
                    {
                        string subGrainId = $"{clientId}.{subId}";
                        var message = new DependentStateChangedMessage { State = State.Alive };
                        Context.GetSubscribtionGrain(subGrainId).NotifyDependenciesOfStateChange(message, CancellationToken.None);
                    }
                };

                logger.LogInformation("Connecting client");

                heartbeatInterval = CalculateHeartbeatIntervalInSeconds(request.ConnectionTimeout);
                json = new { HeartbeatInterval = heartbeatInterval };

                SetupMqttSubscribtions();
            }
            else
            {
                logger.LogInformation("Reconnecting client");
                messageType = "reconnect-ack";
                var publishList = from p in publishes select new { Id=p.Value.PublishId, Topic=p.Key, Active=p.Value.Active };
                var subscribeList = from p in subscribeTopics select new { Id = p.Key, Topic = p.Value };
                json = new
                {
                    HeartbeatInterval = heartbeatInterval,
                    Publishes = publishList,
                    Subscriptions = subscribeList
                };
            }


            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId!))
                .WithPayload(Encoding.ASCII.GetBytes($"{messageType}<>{JsonConvert.SerializeObject(json)}"))
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            mqttClient.PublishAsync(message);


            logger.LogInformation("Client Connected.");
            return Task.CompletedTask;
        }

        private int CalculateHeartbeatIntervalInSeconds(int connectionTimeout)
        {
            return (int)Math.Floor(connectionTimeout / 5d);
        }

        private void SetupMqttSubscribtions()
        {
            mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            mqttClient.SubscribeAsync(MqttTopicHelper.ClientQuery(clientId!));
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            if (args.ApplicationMessage.Topic == MqttTopicHelper.ClientQuery(clientId!))
            {
                logger.LogInformation("Received Query");

                var message = args.ApplicationMessage.ConvertPayloadToString(); //Message format | <publish/subscribe>:json
                if (message != null)
                {

                    if (message.StartsWith("publish"))
                    {
                        await HandlePublishQuery(message.Split("<>")[1]);
                    }
                    else if (message.StartsWith("subscribe"))
                    {
                        await HandleSubcribeQuery(message.Split("<>")[1]);
                    }
                }
            }
        }
    }
}
