using ActorBackend.Actors;
using ActorBackend.Data;
using Proto;
using Proto.Cluster;

namespace ActorBackend.HealthMonitoring
{
    public class ClientDependencyList
    {
        private Dictionary<string, DependencyInfo> dependencies = new Dictionary<string, DependencyInfo>();

        private IContext ctx;
        private string actorIdentity;

        public ClientDependencyList(IContext ctx, string actorIdentity)
        {
            this.ctx = ctx;
            this.actorIdentity = actorIdentity;
        }

        public void AddDependency(string pubTopic, string subId, string clientActorIdentity)
        {
            if (!dependencies.ContainsKey(clientActorIdentity))
            {
                dependencies[clientActorIdentity] = new DependencyInfo() { Client = ctx.Cluster().GetClientGrain(clientActorIdentity) };
            }

            dependencies[clientActorIdentity].SubIds.Add(new Tuple<string, string>(subId, pubTopic));
        }

        public void NotifyOfDeadConnection()
        {
            foreach (var dependency in dependencies.Values)
            {
                foreach (var info in dependency.SubIds)
                {
                    dependency.Client.StopPubDependency(new DependencyMessage { ClientActorIdentity = actorIdentity, Topic = info.Item2 }, CancellationToken.None);
                }
            }
        }

        public void NotifyOfAliveConnection()
        {
            foreach (var dependency in dependencies.Values)
            {
                foreach (var info in dependency.SubIds)
                {
                    dependency.Client.StartPubDependency(new DependencyMessage { ClientActorIdentity = actorIdentity, PublishId = info.Item2 }, CancellationToken.None);
                }
            }
        }

        public string? GetSubIdByPubTopic(string clientActorIdentity, string topic)
        {
            var t = dependencies[clientActorIdentity].SubIds.FirstOrDefault((t) =>
            {
                return t.Item2 == topic;
            }, null);

            if (t != null)
                return t.Item1;

            return null;
        }
    }
}
