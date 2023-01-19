using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;

namespace ActorBackend.SystemSubscribtions
{
    public abstract class SystemSubscription
    {
        public string Topic { get; set; }

        protected AppConfig config;
        protected ILogger logger;
        protected ActorSystem actorSystem;
        protected IMqttClient mqttClient;

        protected SystemSubscription(string topicAfterPrefix, AppConfig config, ILogger logger, ActorSystem actorSystem, IMqttClient mqttClient)
        {
            Topic = config.MQTT.TopicPrefix + topicAfterPrefix;
            this.config = config;
            this.logger = logger;
            this.actorSystem = actorSystem;
            this.mqttClient = mqttClient;
        }

        public abstract Task OnMessage(MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellationToken);
    }
}
