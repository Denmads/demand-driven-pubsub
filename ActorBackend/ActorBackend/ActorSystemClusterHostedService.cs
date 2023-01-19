using Proto;
using Proto.Cluster;

namespace ActorBackend
{
    public class ActorSystemClusterHostedService : IHostedService
    {
        private ILogger logger;
        private ActorSystem actorSystem;

        public ActorSystemClusterHostedService(ActorSystem actorSystem, ILogger<ActorSystemClusterHostedService> logger)
        {
            this.actorSystem = actorSystem;
            this.logger = logger;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Starting Proto.Actor Cluster Member");

            await actorSystem.Cluster().StartMemberAsync();
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation("Stopping Proto.Actor Cluster Member");

            await actorSystem.Cluster().ShutdownAsync();
        }
    }
}
