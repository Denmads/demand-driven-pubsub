using ActorBackend.Config;
using Proto;

namespace ActorBackend.Actors
{
    public class ClientManagerGrain : ClientManagerBase
    {
        private AppConfig config;

        public ClientManagerGrain(IContext context, AppConfig config) : base(context)
        {
            this.config = config;
        }

        public override Task ClientConnected(ClientConnectedMessage request)
        {
            throw new NotImplementedException();
        }
    }
}
