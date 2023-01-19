using ActorBackend.Config;
using Proto;
using System.Timers;
using Timer = System.Timers.Timer;

namespace ActorBackend.Actors
{
    public class HealthMonitorGrain : HealthMonitorGrainBase
    {
        private ILogger logger;
        private AppConfig config;

        private IDictionary<string, DateTime> clients;
        private Timer timer;

        public HealthMonitorGrain(IContext context, AppConfig config) : base(context)
        {
            logger = Proto.Log.CreateLogger<HealthMonitorGrain>();
            this.config = config;
            clients = new Dictionary<string, DateTime>();

            timer = new Timer(config.Backend.HealthMonitor.HeartbeatIntervalMilli);
            timer.Elapsed += CheckForDeadClients;

            logger.LogInformation("Health Monitor Grain started");
        }

        private void CheckForDeadClients(object? sender, ElapsedEventArgs e)
        {
            foreach (var client in clients)
            {
                var timeSinceLastHeartbeat = DateTime.Now - client.Value;
                if (timeSinceLastHeartbeat.TotalSeconds > config.Backend.HealthMonitor.MinimumTimeForDeadClient)
                {
                    clients.Remove(client.Key);
                    //Do something
                }
            }
        }

        public override Task NotifyOfHeartbeatResponse(HeartbeatReceivedMessage request)
        {
            clients.Add(request.ClientIdentity, DateTime.Now);

            return Task.CompletedTask;
        }
    }
}
