﻿using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
using System.Text;
using Newtonsoft.Json;
using ActorBackend.Data;

namespace ActorBackend.Actors.Client
{
    public partial class ClientGrain : ClientGrainBase
    {

        private AppConfig config;
        private IMqttClient mqttClient;

        private ClusterIdentity identity;

        private string? clientId = null;
        private ClientConnectionState connectionState;

        private ILogger logger;
        private QueryResolverGrainClient queryResolver;


        public ClientGrain(IContext context, ClusterIdentity identity, AppConfig config) : base(context)
        {
            this.config = config;
            this.identity = identity;

            queryResolver = context.Cluster().GetQueryResolverGrain(SingletonActorIdentities.QUERY_RESOLVER);

            CreateAndConnectMqttClient();
        }

        private void CreateAndConnectMqttClient()
        {
            MqttFactory factory = new MqttFactory();
            mqttClient = factory.CreateMqttClient();

            var mqttClientOptions = factory.CreateClientOptionsBuilder()
                .WithCleanSession()
                .WithClientId(Guid.NewGuid().ToString())
                .WithTcpServer(
                    config.MQTT.Host ?? "localhost",
                    config.MQTT.Port
            ).Build();

            mqttClient.ConnectAsync(mqttClientOptions, CancellationToken.None);

        }

        public override Task OnStopping()
        {
            mqttClient.Dispose();

            return base.OnStopping();
        }

        public override Task Connect(ClientConnectInfo request)
        {
            clientId = request.ClientId;
            logger = Log.CreateLogger($"client/{clientId}");
            connectionState = new ClientConnectionState(mqttClient, request.ConnectionTimeout, clientId);

            var heartbeat = CalculateHeartbeatIntervalInSeconds(request.ConnectionTimeout);

            var json = new { HeartbeatInterval = heartbeat };

            var message = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientResponse(clientId))
                .WithPayload(Encoding.ASCII.GetBytes($"connect-ack<>{JsonConvert.SerializeObject(json)}"))
                .Build();
            mqttClient.PublishAsync(message);

            SetupMqttSubscribtions();

            logger.LogInformation("Client Connected.");
            return Task.CompletedTask;
        }

        private int CalculateHeartbeatIntervalInSeconds(int connectionTimeout)
        {
            return (int)Math.Floor(connectionTimeout / 5d);
        }

        private void SetupMqttSubscribtions()
        {
            mqttClient.ApplicationMessageReceivedAsync += MqttClient_ApplicationMessageReceivedAsync;

            mqttClient.SubscribeAsync(MqttTopicHelper.ClientQuery(clientId!));
        }

        private async Task MqttClient_ApplicationMessageReceivedAsync(MqttApplicationMessageReceivedEventArgs args)
        {
            if (args.ApplicationMessage.Topic == MqttTopicHelper.ClientQuery(clientId!))
            {
                logger.LogInformation("Received Query");

                var message = args.ApplicationMessage.ConvertPayloadToString(); //Message format | <publish/subscribe>:json
                if (message != null)
                {

                    if (message.StartsWith("publish"))
                    {
                        await HandlePublishQuery(message.Split("<>")[1]);
                    }
                    else if (message.StartsWith("subscribe"))
                    {
                        await HandleSubcribeQuery(message.Split("<>")[1]);
                    }
                }
            }
        }
    }
}