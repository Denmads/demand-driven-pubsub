using ActorBackend.Actors;
using ActorBackend.Config;
using Microsoft.Extensions.Options;
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
            builder.Services.AddActorSystem();
            builder.Services.AddHostedService<ActorSystemClusterHostedService>();
            builder.Services.AddHostedService<MqttService>();
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            var config = app.Services.GetRequiredService<IOptions<AppConfig>>();

            var system = app.Services.GetRequiredService<ActorSystem>();
            system.Root.SpawnNamed(
                Props.FromProducer(
                    () => new HealthMonitorGrainActor((context, clusterIdentity) => new HealthMonitorGrain(context, config.Value))
                ),
                "health-monitor"
            );

            app.Run();
        }
    }
}