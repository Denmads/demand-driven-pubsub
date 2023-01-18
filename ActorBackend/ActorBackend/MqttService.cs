using ActorBackend.Config;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using System.Text.Json;

namespace ActorBackend
{
    public class MqttService : IHostedService, IDisposable
    {
        private readonly ILogger logger;
        private MqttFactory mqttFactory;

        private AppConfig config;
        private IMqttClient mqttClient;

        public MqttService(IOptions<AppConfig> config, ILogger<MqttService> logger)
        {
            this.config = config.Value;
            this.logger = logger;

            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            logger.LogInformation($"Connecting to broker: {config.MQTT.Host}:{config.MQTT.Port}");

            var mqttClientOptions = mqttFactory.CreateClientOptionsBuilder()
                .WithCleanSession()
                .WithClientId("DDPS")
                .WithTcpServer(
                    config.MQTT.Host ?? "localhost", 
                    int.Parse(config.MQTT.Port ?? "1883")
                ).Build();

            var response = await mqttClient.ConnectAsync(mqttClientOptions, cancellationToken);

            logger.LogInformation("The MQTT Client is connected.");

            string jsonResp = JsonSerializer.Serialize(response, new JsonSerializerOptions
            {
                WriteIndented = true
            });
            logger.LogInformation(jsonResp);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            var options = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
            await mqttClient.DisconnectAsync(options, cancellationToken);
        }
        public void Dispose()
        {
            mqttClient.Dispose();
        }
    }
}
