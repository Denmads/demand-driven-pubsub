namespace ActorBackend.Transformations
{
    public abstract class IInput<T> : IInput
    {
        public abstract T GetValue(ExecutionContext ctx);

        public new abstract string GetType();

        object IInput.GetValue(ExecutionContext ctx)
        {
            return this.GetValue(ctx)!;
        }
    }

    public interface IInput
    {
        public object GetValue(ExecutionContext ctx);

        public string GetType();
    }
}
