using ActorBackend.Config;
using ActorBackend.Data;
using ActorBackend.Utils;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
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

            mqttClient = MqttUtil.CreateConnectedClient(Guid.NewGuid().ToString());
            SubsbribeToNewConnections();
        }
        private void SubsbribeToNewConnections()
        {
            mqttClient.ApplicationMessageReceivedAsync += async args =>
            {
                var messageTokens = args.ApplicationMessage.ConvertPayloadToString().Split("<>");
                ConnectMessage message = JsonConvert.DeserializeObject<ConnectMessage>(messageTokens[1])!;

                logger.LogInformation($"New Client: {message.ClientId}");

                await Context.Cluster().GetClientGrain(message.ClientId).Connect(new ClientConnectInfo() { ClientId = message.ClientId, ConnectionTimeout = message.ConnectionTimeout}, CancellationToken.None);
            };
            mqttClient.SubscribeAsync(MqttTopicHelper.ClientManagerConnect());

            logger.LogInformation($"Client Manager Started");
        }
    }
}
