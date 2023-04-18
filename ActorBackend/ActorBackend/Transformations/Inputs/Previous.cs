namespace ActorBackend.Transformations.Inputs
{
    public class Previous<T> : IInput<T>
    {
        private string prevDataType;

        public Previous(string prevDataType)
        {
            this.prevDataType = prevDataType;
        }

        public override T GetValue(ExecutionContext ctx)
        {
            var val = ctx.PrevValue;
            return (T)Convert.ChangeType(val, typeof(T));
        }

        public override string GetType()
        {
            return prevDataType;
        }
    }
}
