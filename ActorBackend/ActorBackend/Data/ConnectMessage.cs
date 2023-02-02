namespace ActorBackend.Data
{
    public class ConnectMessage
    {
        public string ClientId { get; set; }
        public int ConnectionTimeout { get; set; }
    }
}
