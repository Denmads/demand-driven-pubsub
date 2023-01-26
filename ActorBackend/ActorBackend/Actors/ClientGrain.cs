using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
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

        public ClientGrain(IContext context, ClusterIdentity identity, AppConfig config) : base(context)
        {
            this.config = config;
            this.identity = identity;

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

            //DEBUG - REMOVE
            var timer = new Timer(2000);
            timer.Elapsed += (_, _) =>
            {
                logger.LogWarning($"Client({clientId}): {connectionState.CurrentState}");
            };
            timer.Start();

            return Task.CompletedTask;
        }

    }
}
