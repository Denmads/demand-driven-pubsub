using MQTTnet;
using MQTTnet.Client;

namespace ActorBackend.SystemSubscribtions
{
    public interface ISystemSubscription
    {
        public string Topic { get; set; }
        Task OnMessage(MqttApplicationMessageReceivedEventArgs args);
    }
}
