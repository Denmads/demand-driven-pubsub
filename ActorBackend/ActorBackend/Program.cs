using ActorBackend.Actors;
using ActorBackend.Config;
using Microsoft.Extensions.Options;
using Neo4j.Driver;
using Proto;
using Proto.Cluster;

namespace ActorBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
            builder.Services.AddSingleton(provider =>
            {
                var config = provider.GetService<IOptions<AppConfig>>()!.Value;

                var uri = $"bolt://{config.Neo4j.Host}:{config.Neo4j.Port}";

                return GraphDatabase.Driver(new Uri(uri), AuthTokens.Basic(config.Neo4j.User, config.Neo4j.Password));

            });
            builder.Services.AddActorSystem();
            builder.Services.AddHostedService<ActorSystemClusterHostedService>();
            //builder.Services.AddHostedService<MqttService>();
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            var config = app.Services.GetRequiredService<IOptions<AppConfig>>();
            MqttTopicHelper.config = config.Value;

            var system = app.Services.GetRequiredService<ActorSystem>();
            system.Root.SpawnNamed(
                Props.FromProducer(
                    () => new ClientManagerGrainActor((context, clusterIdentity) => new ClientManagerGrain(context, config.Value))
                ),
                SingletonActorIdentities.CLIENT_MANAGER
            );

            app.Run();
        }
    }
}