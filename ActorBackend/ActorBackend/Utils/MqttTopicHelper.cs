using ActorBackend.Config;

namespace ActorBackend.Utils
{
    public static class MqttTopicHelper
    {
        public static AppConfig config { get; set; }

        public static string ClientManagerConnect()
        {
            return config.MQTT.TopicPrefix + "/clientmanager/connect";
        }

        public static string ClientHeartbeat(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/heartbeat";
        }

        public static string ClientResponse(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/response";
        }

        public static string ClientQuery(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/query";
        }

        public static string ClientUpdates(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/updates";
        }

        public static string GenerateMqttTopic()
        {
            return config.MQTT.TopicPrefix + "/" + Guid.NewGuid().ToString();
        }
    }
}
