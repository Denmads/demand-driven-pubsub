using ActorBackend.Actors;
using ActorBackend.Utils;
using MQTTnet;
using MQTTnet.Client;
using Newtonsoft.Json;
using Proto;
using Proto.Cluster;

namespace ActorBackend.HealthMonitoring
{
    public class PublishState
    {
        private string clientId;
        private IMqttClient mqttClient;
        private string pubId;

        private IContext context;
        public string PublishId { get { return pubId; } }

        private bool active;
        public bool Active { get { return active; } }

        private List<Tuple<string, string>> listeners = new List<Tuple<string, string>>();

        public PublishState(string clientId, IMqttClient mqttClient, string pubId, IContext context)
        {
            this.clientId = clientId;
            this.mqttClient = mqttClient;
            this.pubId = pubId;
            this.context = context;
            active = false;
        }

        public void AddDependency(string clientActorIdentity, string subId)
        {
            listeners.Add(new Tuple<string, string>(clientActorIdentity, subId));

            if (listeners.Count == 1)
            {
                onActiveChange(true);
            }
        }

        public void RemoveDependency(string clientActorIdentity, string subId)
        {
            listeners.Remove(new Tuple<string, string>(clientActorIdentity, subId));

            if (listeners.Count == 0)
            {
                onActiveChange(false);
            }
        }

        private void onActiveChange(bool active)
        {
            this.active = active;

            var json = new
            {
                PublishId = pubId,
                Active = active
            };

            var queryResponse = $"publish-state-change<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientUpdates(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            mqttClient.PublishAsync(applicationMessage);
        }


        public void NotifyOfClientConnectionStateChange(bool alive)
        {
            foreach (var listener in listeners)
            {
                var message = new DependencyStateChangedMessage { ClientId = clientId, SubscriptionId = listener.Item2, State = alive ? State.Alive : State.Dead };

                context.Cluster().GetClientGrain(listener.Item1).NotifyOfDependencyStateChanged(message, CancellationToken.None);
            }
        }
    }
}
