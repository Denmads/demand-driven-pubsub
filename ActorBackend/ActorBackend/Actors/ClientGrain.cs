using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
using System.Text;
using Timer = System.Timers.Timer;

namespace ActorBackend.Actors
{
    public class ClientGrain : ClientGrainBase
    {

        private AppConfig config;
        private IMqttClient mqttClient;

        private ClusterIdentity identity;

        private string? clientId = null;
        private ClientConnectionState connectionState;

        private ILogger logger;
        private QueryResolverGrainClient queryResolver;


        public ClientGrain(IContext context, ClusterIdentity identity, AppConfig config) : base(context)
        {
            this.config = config;
            this.identity = identity;

            queryResolver = context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER);

            CreateAndConnectMqttClient();
        }

        private void CreateAndConnectMqttClient()
        {
            MqttFactory factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var mqttClientOptions = factory.CreateClientOptionsBuilder()
                .WithCleanSession()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(
                    config.MQTT.Host ?? "localhost",
                    int.Parse(config.MQTT.Port ?? "1883")
            ).Build();

            mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        }
        public override Task Connect(ClientConnectInfo request)
        {
            clientId = request.ClientId;
            logger = Proto.Log.CreateLogger($"client/{clientId}");
            connectionState = new ClientConnectionState(mqttClient, config, clientId);

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId))
                .WithPayload(Encoding.ASCII.GetBytes($"ConnectAck;{config.Backend.HealthMonitor.HeartbeatIntervalMilli}"))
                .Build();
            mqttClient.PublishAsync(message);

            SetupMqttSubscribtions();

            logger.LogInformation("Client Connected.");
            return Task.CompletedTask;
        }

        private void SetupMqttSubscribtions()
        {
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == MqttTopicHelper.ClientQuery(clientId!))
                {
                    logger.LogInformation("Received Query");
                }


                return Task.CompletedTask;
            };

            mqttClient.SubscribeAsync(MqttTopicHelper.ClientQuery(clientId!));
        }

    }
}
