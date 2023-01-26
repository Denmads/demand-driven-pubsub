using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    public class ClientManagerGrain : ClientManagerGrainBase
    {
        private AppConfig config;
        private IMqttClient mqttClient;

        private ILogger logger;

        public ClientManagerGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
            logger = Proto.Log.CreateLogger<ClientManagerGrain>();

            CreateAndConnectMqttClient();
        }

        private async void CreateAndConnectMqttClient()
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

            mqttClient.ConnectedAsync += args =>
            {
                SubsbribeToNewConnections();

                return Task.CompletedTask;
            };
            
            await mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        }
        private void SubsbribeToNewConnections()
        {
            mqttClient.ApplicationMessageReceivedAsync += async args =>
            {
                var clientId = args.ApplicationMessage.ConvertPayloadToString();
                logger.LogInformation($"New Client: {clientId}");

                await Context.Cluster().GetClientGrain(clientId).Connect(new ClientConnectInfo() { ClientId = clientId}, CancellationToken.None);
            };
            mqttClient.SubscribeAsync(config.MQTT.TopicPrefix + "/clientmanager");

            logger.LogInformation($"Client Manager Started");
        }
    }
}
