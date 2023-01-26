using ActorBackend.Config;
using MQTTnet.Client;

namespace ActorBackend.HealthMonitoring
{
    public class ClientConnectionState
    {
        public enum State
        {
            Alive, Dead, Unknown
        }

        public State CurrentState { get; private set; }

        private IMqttClient mqttClient;
        private AppConfig config;
        private string clientId;

        private System.Timers.Timer checkTimer;

        private DateTime lastResponseTime;

        public ClientConnectionState(IMqttClient mqttClient, AppConfig config, string clientId)
        {
            CurrentState = State.Alive;
            this.mqttClient = mqttClient;
            this.config = config;
            this.clientId = clientId;

            lastResponseTime = DateTime.UtcNow;

            checkTimer = new System.Timers.Timer(2000);
            checkTimer.Elapsed += CheckTimer_Elapsed;
            checkTimer.Start();

            SubscribeToHeartbeatResponse();
        }

        private void SubscribeToHeartbeatResponse()
        {
            mqttClient.SubscribeAsync(config.MQTT.TopicPrefix + $"/{clientId}" + config.Backend.HealthMonitor.HeartbeatTopic);
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                lastResponseTime = DateTime.UtcNow;
                CurrentState = State.Alive;

                return Task.CompletedTask;
            };
        }

        private void CheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var diff = DateTime.UtcNow - lastResponseTime;

            if (diff.TotalSeconds > config.Backend.HealthMonitor.MinimumTimeForDeadClient)
            {
                CurrentState = State.Dead;
            }
        }
    }
}
