using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using System.Threading;

namespace ActorBackend.Actors
{
    public class ClientGrain : ClientBase
    {
        private AppConfig config;
        private IMqttClient mqttClient;

        private string? clientId = null;


        public ClientGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
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
        public override Task CreateClient(CreateClientInfo request)
        {
            clientId = request.ClientId;

            return Task.CompletedTask;
        }

        public override Task NotifyOfHeartbeatResponse()
        {
            throw new NotImplementedException();
        }

        public override Task SendHeartbeat()
        {
            throw new NotImplementedException();
        }

    }
}
