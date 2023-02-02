namespace ActorBackend.Config
{
    public class AppConfig
    {
        public MqttConfig MQTT { get; set; }
        public Neo4jConfig Neo4j { get; set; }
        public BackendConfig Backend { get; set; }
    }
}
