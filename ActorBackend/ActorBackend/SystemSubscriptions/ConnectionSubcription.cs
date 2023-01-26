using ActorBackend.Actors;
using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;

namespace ActorBackend.SystemSubscribtions
{
    public class ConnectionSubcription : SystemSubscription
    {
        public ConnectionSubcription(AppConfig config, ILogger logger, ActorSystem actorSystem, IMqttClient mqttClient) 
            : base(config.Backend.HealthMonitor.HeartbeatTopic + "/response", config, logger, actorSystem, mqttClient)
        {
        }

        public override Task OnMessage(MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellationToken)
        {
            //var mes = args.ApplicationMessage.ConvertPayloadToString(); //Contains client id
            //logger.LogDebug($"Received message heartbeat: {mes}");

            //actorSystem.Cluster().GetClientGrain(mes).NotifyOfHeartbeatResponse(cancellationToken);

            return Task.CompletedTask;
        }
    }
}
