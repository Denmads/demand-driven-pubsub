using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;

namespace ActorBackend.SystemSubscribtions
{
    public class HeartbeatSubcription : ISystemSubscription
    {
        public string Topic { get; set; }

        private ILogger logger;

        public HeartbeatSubcription(AppConfig config, ILogger<HeartbeatSubcription> logger)
        {
            Topic = config.MQTT.TopicPrefix + "/system/heartbeat/response";
            this.logger = logger;
        }

        public Task OnMessage(MqttApplicationMessageReceivedEventArgs args)
        {
            return Task.CompletedTask;
        }
    }
}
