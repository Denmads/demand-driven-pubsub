using Microsoft.Extensions.Options;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote.GrpcNet;
using System.Runtime.CompilerServices;

namespace ActorBackend.Config
{
    public static class ActorSystemConfiguration
    {
        public static void AddActorSystem(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(provider => {
                IOptions<AppConfig> config = provider.GetService<IOptions<AppConfig>>()!;

                var actorSystemConfiguration = ActorSystemConfig.Setup();

                var remoteConfig = GrpcNetRemoteConfig.BindTo(
                    config.Value.Backend.Host, config.Value.Backend.Port
                );

                var clusterConfig = ClusterConfig.Setup(
                    clusterName: config.Value.Backend.ClusterName,
                    clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),
                    identityLookup: new PartitionIdentityLookup()
                );

                return new ActorSystem(actorSystemConfiguration)
                            .WithServiceProvider(provider)
                            .WithRemote(remoteConfig)
                            .WithCluster(clusterConfig);
            });
        }
    }
}
