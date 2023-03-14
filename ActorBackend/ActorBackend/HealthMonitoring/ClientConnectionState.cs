using ActorBackend.Utils;
using MQTTnet.Client;

namespace ActorBackend.HealthMonitoring
{
    public class ClientConnectionState
    {
        public enum State
        {
            Alive, Dead, Unknown
        }

        public Action? onConnectionDied { get; set; } = null;
        public Action? onConnectionResurrected { get; set; } = null;


        public State CurrentState { get; private set; }

        private IMqttClient mqttClient;
        private int connectionTimeoutSeconds;
        private string clientId;


        private System.Timers.Timer checkTimer;

        private DateTime lastResponseTime;

        public ClientConnectionState(IMqttClient mqttClient, int connectionTimeoutSeconds, string clientId)
        {
            CurrentState = State.Alive;
            this.mqttClient = mqttClient;
            this.connectionTimeoutSeconds = connectionTimeoutSeconds;
            this.clientId = clientId;

            lastResponseTime = DateTime.UtcNow;

            checkTimer = new System.Timers.Timer(2000);
            checkTimer.Elapsed += CheckTimer_Elapsed;
            checkTimer.Start();

            SubscribeToHeartbeatResponse();
        }

        private void SubscribeToHeartbeatResponse()
        {
            mqttClient.SubscribeAsync(
                MqttTopicHelper.ClientHeartbeat(clientId), 
                MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce);

            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                if (args.ApplicationMessage.Topic == MqttTopicHelper.ClientHeartbeat(clientId))
                {
                    if (CurrentState == State.Dead && onConnectionResurrected != null)
                    {
                        onConnectionResurrected();
                    }

                    lastResponseTime = DateTime.UtcNow;
                    CurrentState = State.Alive;
                }

                return Task.CompletedTask;
            };
        }

        private void CheckTimer_Elapsed(object? sender, System.Timers.ElapsedEventArgs e)
        {
            var diff = DateTime.UtcNow - lastResponseTime;

            if (diff.TotalSeconds > connectionTimeoutSeconds)
            {
                CurrentState = State.Dead;

                if (onConnectionDied != null)
                    onConnectionDied();
            }
        }
    }
}
