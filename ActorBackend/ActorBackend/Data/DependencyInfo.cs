using ActorBackend.Actors;

namespace ActorBackend.Data
{
    public class DependencyInfo
    {
        public List<Tuple<string, string>> SubIds { get; set; } = new List<Tuple<string, string>>();
        public ClientGrainClient Client { get; set; }
    }
}
