using ActorBackend.Config;
using ActorBackend.SystemSubscribtions;
using Microsoft.Extensions.Options;
using MQTTnet;
using MQTTnet.Client;
using MQTTnet.Server;
using Proto;
using System.Text.Json;

namespace ActorBackend
{
    public class MqttService : IHostedService, IDisposable
    {
        private MqttFactory mqttFactory;
        private readonly ILoggerFactory loggerFactory;
        private readonly ILogger logger;

        private ActorSystem actorSystem;

        private AppConfig config;
        private IMqttClient mqttClient;

        private Dictionary<string, SystemSubscription> subscriptions;

        public MqttService(IOptions<AppConfig> config, ILoggerFactory loggerFactory, ActorSystem actorSystem)
        {
            this.config = config.Value;
            this.loggerFactory = loggerFactory;
            this.logger = loggerFactory.CreateLogger<MqttService>();
            this.actorSystem = actorSystem;

            subscriptions = new Dictionary<string, SystemSubscription>();

            mqttFactory = new MqttFactory();
            mqttClient = mqttFactory.CreateMqttClient();

            CreateSubscriptions();
        }

        private void CreateSubscriptions()
        {

        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            await ConnectMqttClient(cancellationToken);

            //Add event handler for subscriptions
            mqttClient.ApplicationMessageReceivedAsync += args =>
            {
                logger.LogDebug($"Received '{args.ApplicationMessage.ConvertPayloadToString()}' on topic '{args.ApplicationMessage.Topic}'");
                if (subscriptions.ContainsKey(args.ApplicationMessage.Topic))
                {

                    var sub = subscriptions!.GetValueOrDefault(args.ApplicationMessage.Topic, null);
                    if (sub != null)
                    {
                        return sub.OnMessage(args, cancellationToken);
                    }
                }

                return Task.CompletedTask;
            };

            foreach (var subscription in subscriptions.Values)
            {
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
                    config.MQTT.Port
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
