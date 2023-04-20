namespace ActorBackend.Transformations.Steps
{
    public class LenFuncStep : ITransformStep<int>
    {
        private IInput<string> inputA;

        public LenFuncStep(IInput<string> inputA)
        {
            this.inputA = inputA;
        }

        public override int Execute(ExecutionContext ctx)
        {
            var res = inputA.GetValue(ctx).Length;
            ctx.SetPrev(res.ToString(), "int");

            return res;
        }
    }
}
