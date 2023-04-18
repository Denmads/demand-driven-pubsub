namespace ActorBackend.Transformations
{
    public abstract class ITransformStep<TResult> : ITransformStep
    {
        public abstract bool Verify();

        public abstract TResult Execute(ExecutionContext ctx);

        object ITransformStep.Execute(ExecutionContext ctx)
        {
            return this.Execute(ctx)!;
        }
    }

    public interface ITransformStep
    {
        public bool Verify();

        public object Execute(ExecutionContext ctx);
    }
}
