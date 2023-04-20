namespace ActorBackend.Transformations.Inputs
{
    public class Constant<T> : IInput<T>
    {
        private T value;
        private string type;

        public Constant(T value, string type)
        {
            this.value = value;
            this.type = type;
        }

        public override T GetValue(ExecutionContext ctx)
        {
            return value;
        }

        public override string GetType()
        {
            return type;
        }
    }
}
