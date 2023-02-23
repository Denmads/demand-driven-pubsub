namespace ActorBackend.Data
{
    public class PublishQuery
    {
        public int RequestId { get; set; }
        public string PublishId { get; set; }
        public string CypherQuery { get; set; }
        public string TargetNode { get; set; }
        public string DataType { get; set; }

    }
}
