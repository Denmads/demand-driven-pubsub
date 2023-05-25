namespace ActorBackend.Transformations.Steps
{
    public class MulStep<T> : ITransformStep<T>
    {
        private IInput<T> inputA; 
        private IInput<T> inputB;

        public MulStep(IInput<T> inputA, IInput<T> inputB)
        {
            this.inputA = inputA;
            this.inputB = inputB;
        }

        public override T Execute(ExecutionContext ctx)
        {
            var objA = (object)inputA.GetValue(ctx)!;
            var objB = (object)inputB.GetValue(ctx)!;

            if (typeof(T) == typeof(int))
            {
                int valA = (int)objA;
                int valB = (int)objB;
                var res = valA * valB;

                ctx.SetPrev(res.ToString(), "int");
                return (T)(object)res;
            }
            else if (typeof(T) == typeof(float))
            {
                float valA = (float)objA;
                float valB = (float)objB;
                var res = valA * valB;

                ctx.SetPrev(res.ToString(), "float");
                return (T)(object)res;
            }

            return default(T)!;
        }
    }
}
