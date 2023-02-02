using ActorBackend.Config;
using Neo4jClient;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    public class QueryResolverGrain : QueryResolverGrainBase
    {
        private AppConfig config;

        private const int MAX_QUERIES_MAIL_BOX = 10;

        private Dictionary<string, int> actorQueryCount= new Dictionary<string, int>();
        private int queryGrainCount = 0;

        public QueryResolverGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
        }

        public override Task QueryResolved(QueryResolvedResponse request)
        {
            throw new NotImplementedException();
        }

        public override Task ResolveQuery(Neo4jQuery request)
        {
            Neo4jQueryGrainClient client = FindQueryClient();

            if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.PublishInfo)
            {
                client.ResolvePublishQuery(request.PublishInfo, CancellationToken.None);
            }
            else if (request.QueryTypeCase == Neo4jQuery.QueryTypeOneofCase.SubscribeInfo)
            {
                client.ResolveSubscribeQuery(request.SubscribeInfo, CancellationToken.None);
            }

            return Task.CompletedTask;
        }

        private Neo4jQueryGrainClient FindQueryClient()
        {
            if (actorQueryCount.Count == 0)
            {
                var identity = CreateNeo4jQueryGrain();
                return Context.Cluster().GetNeo4jQueryGrain(identity);
            }

            var lowestKvp = actorQueryCount.MinBy(kvp => kvp.Value);

            if (lowestKvp.Value > MAX_QUERIES_MAIL_BOX)
            {
                var identity = CreateNeo4jQueryGrain();
                return Context.Cluster().GetNeo4jQueryGrain(identity);
            }
            
            return Context.Cluster().GetNeo4jQueryGrain(lowestKvp.Key);
        }

        private string CreateNeo4jQueryGrain()
        {
            queryGrainCount++;
            var identity = $"query-grain-{queryGrainCount}";
            actorQueryCount.Add(identity, 0);
            return identity;
        }
    }
}
