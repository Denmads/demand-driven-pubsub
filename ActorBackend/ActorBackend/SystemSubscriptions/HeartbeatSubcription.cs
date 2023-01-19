using ActorBackend.Actors;
using ActorBackend.Config;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;

namespace ActorBackend.SystemSubscribtions
{
    public class HeartbeatSubcription : SystemSubscription
    {
        private const string GRAIN_IDENTITY = "health-monitor";

        private HealthMonitorGrainClient heartbeatClient;

        public HeartbeatSubcription(AppConfig config, ILogger logger, ActorSystem actorSystem, IMqttClient mqttClient) 
            : base("/system/heartbeat/response", config, logger, actorSystem, mqttClient)
        {
            heartbeatClient = actorSystem.Cluster().GetHealthMonitorGrain(GRAIN_IDENTITY);
        }

        public override Task OnMessage(MqttApplicationMessageReceivedEventArgs args, CancellationToken cancellationToken)
        {
            var mes = args.ApplicationMessage.ConvertPayloadToString(); //Contains client id
            logger.LogDebug($"Received message heartbeat: {mes}");

            heartbeatClient.NotifyOfHeartbeatResponse(
                new HeartbeatReceivedMessage { ClientIdentity = mes },
                cancellationToken
            );

            return Task.CompletedTask;
        }
    }
}
