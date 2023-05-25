namespace ActorBackend.Transformations.Steps
{
    public class DivStep<T> : ITransformStep<T>
    {
        private IInput<T> inputA; 
        private IInput<T> inputB;

        public DivStep(IInput<T> inputA, IInput<T> inputB)
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

                if (valB == 0)
                {
                    throw new DivideByZeroException("Cannot divide by zero");
                }

                float res = valA / valB;

                if (res % 1 == 0) //int
                {
                    ctx.SetPrev(((int)res).ToString(), "int");
                    return (T)(object)res;
                }
                else //float
                {
                    ctx.SetPrev(res.ToString(), "float");
                    return (T)(object)res;
                }

            }
            else if (typeof(T) == typeof(float))
            {
                float valA = (float)objA;
                float valB = (float)objB;

                if (valB == 0)
                {
                    throw new DivideByZeroException("Cannot divide by zero");
                }

                float res = valA / valB;

                if (res % 1 == 0) //int
                {
                    ctx.SetPrev(((int)res).ToString(), "int");
                    return (T)(object)res;
                }
                else //float
                {
                    ctx.SetPrev(res.ToString(), "float");
                    return (T)(object)res;
                }
            }

            return default(T)!;
        }
    }
}
