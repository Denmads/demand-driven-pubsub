namespace ActorBackend.Config
{
    public class HealthMonitorConfig
    {
        public int HeartbeatIntervalMilli { get; set; }

        public int MinimumTimeForDeadClient { get; set; } //Seconds

        public string HeartbeatTopic { get; set; }
    }
}
