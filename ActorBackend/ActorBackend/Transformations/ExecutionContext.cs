using ActorBackend.Actors;
using DataValues = ActorBackend.Actors.Data;

namespace ActorBackend.Transformations
{
    public class ExecutionContext
    {
        public DataValues Data { get; private set; }

        private (string, string) prevInfo;
        public string PrevValue { get { return prevInfo.Item1; } }
        public string PrevType { get { return prevInfo.Item2; } }

        public ExecutionContext(DataValues data)
        {
            Data = data;
        }

        public void SetPrev(string value, string dataType)
        {
            prevInfo = (value, dataType);
        }
    }
}
