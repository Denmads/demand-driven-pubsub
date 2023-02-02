namespace ActorBackend.Config
{
    public class MqttConfig
    {
        public string Host { get; set; }
        public int Port { get; set; }
        public string TopicPrefix { get; set; }
    }
}
