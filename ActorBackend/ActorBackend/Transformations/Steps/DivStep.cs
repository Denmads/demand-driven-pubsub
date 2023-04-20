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
            if (typeof(T) == typeof(int))
            {
                int valA = (int)Convert.ChangeType(inputA.GetValue(ctx), typeof(int))!;
                int valB = (int)Convert.ChangeType(inputB.GetValue(ctx), typeof(int))!;

                if (valB == 0)
                {
                    throw new DivideByZeroException("Cannot divide by zero");
                }

                float res = valA / valB;

                if (res % 1 == 0) //int
                {
                    ctx.SetPrev(((int)res).ToString(), "int");
                    return (T)Convert.ChangeType(res, typeof(T));
                }
                else //float
                {
                    ctx.SetPrev(res.ToString(), "float");
                    return (T)Convert.ChangeType(res, typeof(T));
                }

            }
            else if (typeof(T) == typeof(float))
            {
                float valA = (float)Convert.ChangeType(inputA.GetValue(ctx), typeof(float))!;
                float valB = (float)Convert.ChangeType(inputB.GetValue(ctx), typeof(float))!;

                if (valB == 0)
                {
                    throw new DivideByZeroException("Cannot divide by zero");
                }

                float res = valA / valB;

                if (res % 1 == 0) //int
                {
                    ctx.SetPrev(((int)res).ToString(), "int");
                    return (T)Convert.ChangeType(res, typeof(T));
                }
                else //float
                {
                    ctx.SetPrev(res.ToString(), "float");
                    return (T)Convert.ChangeType(res, typeof(T));
                }
            }

            return default(T)!;
        }
    }
}
