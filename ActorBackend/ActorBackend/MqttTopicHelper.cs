﻿using ActorBackend.Config;

namespace ActorBackend
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
            return config.MQTT.TopicPrefix + $"/{clientId}" + config.Backend.HealthMonitor.HeartbeatTopic;
        }

        public static string ClientResponse(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/response";
        }

        public static string ClientQuery(string clientId)
        {
            return config.MQTT.TopicPrefix + $"/{clientId}/query";
        }
    }
}
