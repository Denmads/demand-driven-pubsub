namespace ActorBackend.Transformations.Inputs
{
    public class Variable<T> : IInput<T>
    {
        private string varName;
        private string type;

        public Variable(string varName, string type)
        {
            this.varName = varName;
            this.type = type;
        }

        public override T GetValue(ExecutionContext ctx)
        {
            var val = ctx.Data.Data_[varName].Value;

            if (string.IsNullOrEmpty(val) && type != "string")
            {
                throw new InvalidOperationException("Cannot execute transformations on empty value");
            }

            return (T)Convert.ChangeType(val, typeof(T));
        }

        public override string GetType()
        {
            return type;
        }
    }
}
