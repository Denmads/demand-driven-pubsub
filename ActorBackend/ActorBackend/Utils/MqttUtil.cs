using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto.Cluster.PubSub;

namespace ActorBackend.Utils
{
    public static class MqttUtil
    {
        private static MqttFactory factory = new MqttFactory();
        private static AppConfig config;

        public static void Initialize(AppConfig config)
        {
            MqttUtil.config = config;
            MqttTopicHelper.config = config;
        }

        public static IMqttClient CreateConnectedClient(string? clientId = null, bool cleanSession = true)
        {
            if (config == null)
                throw new InvalidOperationException("MqttUtil not initialized. Make sure to call 'Initialize' before the main logic runs.");

            var client = factory.CreateMqttClient();

            var connectOptionsBuilder = factory.CreateClientOptionsBuilder()
                .WithTcpServer(
                    config.MQTT.Host ?? "localhost",
                    config.MQTT.Port
                )
                .WithCleanSession(cleanSession);

            if (clientId != null)
                connectOptionsBuilder.WithClientId(clientId);

            client.ConnectAsync(connectOptionsBuilder.Build(), CancellationToken.None);

            return client;
        }
    }
}
