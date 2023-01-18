using ActorBackend.Config;
using ActorBackend.SystemSubscribtions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using System.Text.Json;

namespace ActorBackend
{
    public class MqttService : IHostedService, IDisposable
    {
        private MqttFactory mqttFactory;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        private AppConfig config;
        private IMqttClient mqttClient;

        private Dictionary<string, ISystemSubscription> subscriptions;

        public MqttService(IOptions<AppConfig> config, ILoggerFactory loggerFactory)
        {
            this.config = config.Value;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<MqttService>();

            subscriptions = new Dictionary<string, ISystemSubscription>();

            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();

            CreateSubscriptions();
        }

        private void CreateSubscriptions()
        {
            var sub = new HeartbeatSubcription(config, loggerFactory.CreateLogger<HeartbeatSubcription>());
            subscriptions.Add(sub.Topic, sub);
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectMqttClient(cancellationToken);

            foreach (var subscription in subscriptions.Values)
            {
                mqttClient.ApplicationMessageReceivedAsync += args =>
                {
                    if (subscriptions.ContainsKey(args.ApplicationMessage.Topic))
                    {
                        var sub = subscriptions!.GetValueOrDefault(args.ApplicationMessage.Topic, null);
                        if (sub != null)
                        {
                            return sub.OnMessage(args);
                        }
                    }

                    return Task.CompletedTask;
                };

                await mqttClient.SubscribeAsync(subscription.Topic);
                logger.LogInformation($"System subscribed to topic: {subscription.Topic}");
            }
        }

        private async Task ConnectMqttClient(CancellationToken cancellationToken)
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
            foreach (var subscription in subscriptions.Values)
            {
                await mqttClient.UnsubscribeAsync(subscription.Topic);
                logger.LogInformation($"System unsubscribed from topic: {subscription.Topic}");
            }

            var options = mqttFactory.CreateClientDisconnectOptionsBuilder().Build();
            await mqttClient.DisconnectAsync(options, cancellationToken);
        }
        public void Dispose()
        {
            mqttClient.Dispose();
        }
    }
}
