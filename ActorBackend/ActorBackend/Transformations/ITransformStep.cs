namespace ActorBackend.Transformations
{
    public abstract class ITransformStep<TResult> : ITransformStep
    {
        public abstract TResult Execute(ExecutionContext ctx);

        object ITransformStep.Execute(ExecutionContext ctx)
        {
            return this.Execute(ctx)!;
        }
    }

    public interface ITransformStep
    {
        public object Execute(ExecutionContext ctx);
    }
}
