using ActorBackend.Actors;
using ActorBackend.Actors.Client;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;
using Proto.Cluster.Partition;
using Proto.Cluster.Testing;
using Proto.DependencyInjection;
using Proto.Remote;
using Proto.Remote.GrpcNet;
using System.Runtime.CompilerServices;

namespace ActorBackend.Config
{
    public static class ActorSystemConfiguration
    {
        public static void AddActorSystem(this IServiceCollection serviceCollection)
        {
            serviceCollection.AddSingleton(provider => {
                AppConfig config = provider.GetService<IOptions<AppConfig>>()!.Value;
                IDriver driver = provider.GetService<IDriver>()!;

                var actorSystemConfiguration = ActorSystemConfig.Setup();

                var remoteConfig = GrpcNetRemoteConfig
                .BindTo(
                    config.Backend.Host, config.Backend.Port
                )
                .WithProtoMessages(MessagesReflection.Descriptor);

                var clusterConfig = ClusterConfig.Setup(
                    clusterName: config.Backend.ClusterName,
                    clusterProvider: new TestProvider(new TestProviderOptions(), new InMemAgent()),

                    //https://proto.actor/docs/cluster/identity-lookup-net/
                    identityLookup: new PartitionIdentityLookup()
                )
                .WithClusterKind(
                    kind: ClientGrainActor.Kind,
                    prop: Props.FromProducer(() =>
                        new ClientGrainActor((context, clusterIdentity) =>
                            new ClientGrain(context, clusterIdentity, config)
                        ))
                )
                .WithClusterKind(
                    kind: QueryResolverGrainActor.Kind,
                    prop: Props.FromProducer(() => 
                        new QueryResolverGrainActor((context, clusterIdentity) => 
                            new QueryResolverGrain(context, config)
                        ))
                )
                .WithClusterKind(
                    kind: Neo4jQueryGrainActor.Kind,
                    prop: Props.FromProducer(() =>
                        new Neo4jQueryGrainActor((context, clusterIdentity) =>
                            new Neo4jQueryGrain(context, driver)
                        ))
                )
                .WithClusterKind(
                    kind: SubscribtionGrainActor.Kind,
                    prop: Props.FromProducer(() =>
                        new SubscribtionGrainActor((context, clusterIdentity) =>
                            new SubscribtionGrain(context, config)
                        ))
                );


                //Debuggin of actor framework
                if (config.Backend.EnableActorFrameworkLogging)
                {
                    var logLevel = config.Backend.ActorFrameworkMinimumLogLevel switch
                    {
                        "Information" => LogLevel.Information,
                        _ => LogLevel.Debug,
                    };

                    Proto.Log.SetLoggerFactory(LoggerFactory.Create(l => l.AddConsole().SetMinimumLevel(logLevel)));

                    //var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                    //Proto.Log.SetLoggerFactory(loggerFactory);

                    
                }

                return new ActorSystem(actorSystemConfiguration)
                            .WithServiceProvider(provider)
                            .WithRemote(remoteConfig)
                            .WithCluster(clusterConfig);
            });
        }
    }
}
