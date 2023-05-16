using ActorBackend.Transformations.Inputs;
using ActorBackend.Transformations.Steps;

namespace ActorBackend.Transformations
{
    public class TransformationFactory
    {
        private ParsingContext context;

        public TransformationFactory(ParsingContext context)
        {
            this.context = context;
        }

        public ParsingContext GetContext()
        {
            return context;
        }

        private IInput CreateInput(string toParse)
        {
            if (toParse == "prev")
            {
                if (context.IsPrevAllowed())
                    return context.CreatePrevInput();

                throw new InvalidOperationException("Prev is not allowed here!");
            }
            else if (context.isNodeName(toParse))
            {
                return context.CreateVariableInput(toParse);
            }
            else
            {
                if (toParse == "false" || toParse == "true")
                {
                    return new Constant<bool>(bool.Parse(toParse), "bool");
                }
                else if (int.TryParse(toParse, out var res))
                {
                    return new Constant<int>(res, "int");
                }
                else if (float.TryParse(toParse, out var res2))
                {
                    return new Constant<float>(res2, "float");
                }
                else
                {
                    return new Constant<string>(toParse, "string");
                }
            }
        }

        public ITransformStep CreateAddStep(string step)
        {
            var tokens = step.Split("+");
            var inputs = tokens.Select(t => CreateInput(t)).ToList();

            if (inputs.All(inp => inp.GetType() == "int"))
            {
                return new AddStep<int>((IInput<int>)inputs[0], (IInput<int>)inputs[1]);
            }
            else if (inputs.All(inp => inp.GetType() == "float"))
            {
                return new AddStep<float>((IInput<float>)inputs[0], (IInput<float>)inputs[1]);
            }
            else if (inputs.All(inp => inp.GetType() == "string"))
            {
                return new AddStep<string>((IInput<string>)inputs[0], (IInput<string>)inputs[1]);
            }
            else
            {
                throw new InvalidDataException("Invalid types for add operation.");
            }
        }

        public ITransformStep CreateSubStep(string step)
        {
            var tokens = step.Split("-");
            var inputs = tokens.Select(t => CreateInput(t)).ToList();

            if (inputs.All(inp => inp.GetType() == "int"))
            {
                return new SubStep<int>((IInput<int>)inputs[0], (IInput<int>)inputs[1]);
            }
            else if (inputs.All(inp => inp.GetType() == "float"))
            {
                return new SubStep<float>((IInput<float>)inputs[0], (IInput<float>)inputs[1]);
            }
            else
            {
                throw new InvalidDataException("Invalid types for sub operation.");
            }
        }

        public ITransformStep CreateMulStep(string step)
        {
            var tokens = step.Split("*");
            var inputs = tokens.Select(t => CreateInput(t)).ToList();

            if (inputs.All(inp => inp.GetType() == "int"))
            {
                return new MulStep<int>((IInput<int>)inputs[0], (IInput<int>)inputs[1]);
            }
            else if (inputs.All(inp => inp.GetType() == "float"))
            {
                return new MulStep<float>((IInput<float>)inputs[0], (IInput<float>)inputs[1]);
            }
            else
            {
                throw new InvalidDataException("Invalid types for mul operation.");
            }
        }

        public ITransformStep CreateDivStep(string step)
        {
            var tokens = step.Split("/");
            var inputs = tokens.Select(t => CreateInput(t)).ToList();

            if (inputs.All(inp => inp.GetType() == "int"))
            {
                return new DivStep<int>((IInput<int>)inputs[0], (IInput<int>)inputs[1]);
            }
            else if (inputs.All(inp => inp.GetType() == "float"))
            {
                return new DivStep<float>((IInput<float>)inputs[0], (IInput<float>)inputs[1]);
            }
            else
            {
                throw new InvalidDataException("Invalid types for div operation.");
            }
        }

        public ITransformStep CreateLenFuncStep(string step)
        {
            var token = step.Substring(4, step.Length-5);
            var input = CreateInput(token);

            if (input.GetType() == "string")
            {
                return new LenFuncStep((IInput<string>)input);
            }
            else
            {
                throw new InvalidDataException("Invalid types for div operation.");
            }
        }
    }
}
