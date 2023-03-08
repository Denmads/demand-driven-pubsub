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
        private Dictionary<string, Dictionary<string, ClientGrainClient>> dependents = new Dictionary<string, Dictionary<string, ClientGrainClient>>();
        
        private void NotifyDependentsOfStateChange(State state)
        {
            foreach (var pub in dependents)
            {
                foreach(var client in pub.Value)
                {
                    var message = new DependencyStateChangeMessage { ClientIdentity = clientId, ClientActorIdentity = identity.Identity, PublishTopic = publishTopics[pub.Key].Topic, State = state };

                    client.Value.NotifyOfDependencyStateChange(message, CancellationToken.None);
                }
            }
        }
        
        public override Task StartPubDependency(DependencyMessage request)
        {
            string pubId = request.IdTypeCase == DependencyMessage.IdTypeOneofCase.Topic ? topicToPubId[request.Topic] : request.PublishId;
            ClientGrainClient client = Context.Cluster().GetClientGrain(request.ClientActorIdentity);

            dependents[pubId][request.ClientActorIdentity] = client;

            if (dependents[pubId].Count == 1)
            {
                //Gone active
                publishTopics[pubId].Active = true;

                NotifyClientOfPublishStateChange(pubId, true);
            }

            return Task.CompletedTask;
        }

        public override Task StopPubDependency(DependencyMessage request)
        {
            string pubId = request.IdTypeCase == DependencyMessage.IdTypeOneofCase.Topic ? topicToPubId[request.Topic] : request.PublishId;

            dependents[pubId].Remove(request.ClientActorIdentity);

            if (dependents[pubId].Count == 0)
            {
                //Gone inactive
                publishTopics[pubId].Active = false;

                NotifyClientOfPublishStateChange(pubId, false);
            }

            return Task.CompletedTask;
        }

        private void NotifyClientOfPublishStateChange(string pubId, bool active)
        {
            var json = new
            {
                PublishId = pubId,
                Active = active
            };

            var queryResponse = $"publish-state-change<>{JsonConvert.SerializeObject(json)}";

            var applicationMessage = new MqttApplicationMessageBuilder()
                .WithTopic(MqttTopicHelper.ClientUpdates(clientId!))
                .WithPayload(queryResponse)
                .WithQualityOfServiceLevel(MQTTnet.Protocol.MqttQualityOfServiceLevel.AtLeastOnce)
                .Build();
            
            mqttClient.PublishAsync(applicationMessage);
        }

        public override Task NotifyOfDependencyStateChange(DependencyStateChangeMessage request)
        {
            string? subId = dependencyList.GetSubIdByPubTopic(request.ClientActorIdentity, request.PublishTopic);

            var json = new
            {
                SubscribtionId = subId,
                ClientId = request.ClientIdentity
            };

            var queryResponse = $"dependency-${(request.State == State.Dead ? "died" : "resurrected")}<>{JsonConvert.SerializeObject(json)}";

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
