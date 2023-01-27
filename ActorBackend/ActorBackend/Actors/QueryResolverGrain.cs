using ActorBackend.Config;
using Proto;

namespace ActorBackend.Actors
{
    public class QueryResolverGrain : QueryResolverGrainBase
    {
        private AppConfig config;
        public QueryResolverGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
        }

        public override Task ResolvePublishQuery(PublishQueryInfo request)
        {
            throw new NotImplementedException();
        }

        public override Task ResolveSubscribeQuery(SubscribeQueryInfo request)
        {
            throw new NotImplementedException();
        }
    }
}
