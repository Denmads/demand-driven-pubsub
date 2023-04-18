﻿namespace ActorBackend.Transformations.Steps
{
    public class AddStep<T> : ITransformStep<T>
    {
        private IInput<T> inputA; 
        private IInput<T> inputB;

        public AddStep(IInput<T> inputA, IInput<T> inputB)
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
                var res = valA + valB;

                ctx.SetPrev(res.ToString(), "int");
                return (T)Convert.ChangeType(res, typeof(T));
            }
            else if (typeof(T) == typeof(float))
            {
                float valA = (float)Convert.ChangeType(inputA.GetValue(ctx), typeof(float))!;
                float valB = (float)Convert.ChangeType(inputB.GetValue(ctx), typeof(float))!;
                var res = valA + valB;

                ctx.SetPrev(res.ToString(), "float");
                return (T)Convert.ChangeType(res, typeof(T));
            }
            else if (typeof(T) == typeof(string))
            {
                string valA = (string)Convert.ChangeType(inputA.GetValue(ctx), typeof(string))!;
                string valB = (string)Convert.ChangeType(inputB.GetValue(ctx), typeof(string))!;
                var res = valA + valB;

                ctx.SetPrev(res.ToString(), "string");
                return (T)Convert.ChangeType(res, typeof(T));
            }

            return default(T)!;
        }

        public override bool Verify()
        {
            throw new NotImplementedException();
        }
    }
}
