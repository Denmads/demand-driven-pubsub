namespace ActorBackend.Config
{
    public class BackendConfig
    {
        public HealthMonitorConfig HealthMonitor { get; set; }


        public string Host { get; set; }
        public int Port { get; set; }
        public string ClusterName { get; set; }

        public bool EnableActorFrameworkLogging { get; set; }
        public string ActorFrameworkMinimumLogLevel { get; set; }
    }
}
