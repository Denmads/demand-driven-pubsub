using ActorBackend.Config;
using ActorBackend.HealthMonitoring;
using MQTTnet;
using MQTTnet.Client;
using Proto;
using Proto.Cluster;
using System.Text;
using Newtonsoft.Json;
using ActorBackend.Data;
using ActorBackend.Utils;
using System;

namespace ActorBackend.Actors.Client
{
    public partial class ClientGrain : ClientGrainBase
    {
        public override Task StartPublishDependency(DependencyMessage request)
        {
            publishes[request.PublishTopic].AddDependency(request.ClientActorIdentity, request.SubscriptionId);
            
            return Task.CompletedTask;
        }

        public override Task StopPublishDependency(DependencyMessage request)
        {
            publishes[request.PublishTopic].RemoveDependency(request.ClientActorIdentity, request.SubscriptionId);

            return Task.CompletedTask;
        }

        public override Task NotifyOfDependencyStateChanged(DependencyStateChangedMessage request)
        {
            var json = new
            {
                SubscribtionId = request.SubscriptionId,
                ClientId = request.ClientId
            };

            var queryResponse = $"dependency-{(request.State == State.Dead ? "died" : "resurrected")}<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientUpdates(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();

            mqttClient.PublishAsync(applicationMessage);

            return Task.CompletedTask;
        }
    }
}
