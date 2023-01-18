using ActorBackend.Config;

namespace ActorBackend
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.Configure<AppConfig>(builder.Configuration.GetSection("AppConfig"));
            builder.Services.AddHostedService<MqttService>();
            var app = builder.Build();

            app.MapGet("/", () => "Hello World!");

            app.Run();
        }
    }
}