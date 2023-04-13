using ActorBackend.Config;
using Proto;
using Proto.Cluster;

namespace ActorBackend.Actors
{
    public class QueryResolverGrain : QueryResolverGrainBase
    {
        private AppConfig config;

        private const int MAX_QUERIES_MAIL_BOX = 10;

        private Dictionary<string, int> actorQueryCount = new Dictionary<string, int>();
        private int queryGrainCount = 0;

        public QueryResolverGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
        }

        private bool ensuredAdmin = false;
        private Neo4jQuery CreateEnsureAdminUserExistsRequest()
        {
            Neo4jQuery neo4jQuery = new Neo4jQuery();
            neo4jQuery.CreateAdminUserInfo = new CreateAdminUserQueryInfo
            {
                User = new User
                {
                    Username = "admin",
                    Password = "admin"
                }
            };
            
            return neo4jQuery;
        }

        public override Task QueryResolved(QueryResolvedResponse request)
        {
            actorQueryCount[request.QueryActorIdentity] = actorQueryCount[request.QueryActorIdentity] - 1;
            return Task.CompletedTask;
        }

        public override Task ResolveQuery(Neo4jQuery request)
        {
            Neo4jQueryGrainClient client = FindQueryClient();

            if (!ensuredAdmin)
            {
                ensuredAdmin = true;
                var adminReq = CreateEnsureAdminUserExistsRequest();
                client.ResolveQuery(adminReq, CancellationToken.None);
                Thread.Sleep(1000);
            }

            client.ResolveQuery(request, CancellationToken.None);

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
