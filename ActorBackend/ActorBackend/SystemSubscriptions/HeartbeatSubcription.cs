using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;

namespace ActorBackend.SystemSubscribtions
{
    public class HeartbeatSubcription : SystemSubscription
    {
        public HeartbeatSubcription(AppConfig config, ILogger logger, ActorSystem actorSystem, IMqttClient mqttClient) 
            : base("/system/heartbeat/response", config, logger, actorSystem, mqttClient)
        {}

        public override Task OnMessage(MqttApplicationMessageReceivedEventArgs args)
        {
            var mes = args.ApplicationMessage.ConvertPayloadToString();

            logger.LogDebug($"Received message heartbeat: {mes}") ;

            return Task.CompletedTask;
        }
    }
}
