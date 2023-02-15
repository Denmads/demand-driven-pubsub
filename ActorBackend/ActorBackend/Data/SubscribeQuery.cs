namespace ActorBackend.Data
{
    public class SubscribeQuery
    {
        public int RequestId { get; set; }
        public string SubscriptionId { get; set; }
        public string CypherQuery { get; set; }
        public List<string> TargetNodes { get; set; }
    }
}
