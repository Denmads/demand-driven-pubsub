using ActorBackend.Data;
using ActorBackend.Transformations.Steps;
using Neo4j.Driver;

namespace ActorBackend.Transformations
{
    public static class TransformationStepParser
    {
        public static Dictionary<string, List<ITransformStep>> Parse(TransformationDescription description, ParsingContext context)
        {
            var factory = new TransformationFactory(context);

            var steps = new Dictionary<string, List<ITransformStep>>();
            foreach (var transform in description.Changes)
            {
                steps[transform.StoredIn] = ParseSteps(transform.Steps, factory);

                if (steps[transform.StoredIn].Last().GetType().GetGenericArguments()[0] == typeof(string))
                    factory.GetContext().AddCalculatedNode(transform.StoredIn, "string");
                else if (steps[transform.StoredIn].Last().GetType().GetGenericArguments()[0] == typeof(int))
                    factory.GetContext().AddCalculatedNode(transform.StoredIn, "int");
                else if (steps[transform.StoredIn].Last().GetType().GetGenericArguments()[0] == typeof(float))
                    factory.GetContext().AddCalculatedNode(transform.StoredIn, "float");
                else if (steps[transform.StoredIn].Last().GetType().GetGenericArguments()[0] == typeof(bool))
                    factory.GetContext().AddCalculatedNode(transform.StoredIn, "bool");
            }

            return steps;
        }

        private static List<ITransformStep> ParseSteps(List<string> steps, TransformationFactory factory)
        {
            return steps.Select(st =>
            {
                if (st.Contains("+"))
                {
                    return factory.CreateAddStep(st);
                }
                else if (st.Contains("-"))
                {
                    return factory.CreateSubStep(st);
                }
                else if (st.Contains("*"))
                {
                    return factory.CreateMulStep(st);
                }
                else if (st.Contains("/"))
                {
                    return factory.CreateDivStep(st);
                }
                else if (st.StartsWith("len(") && st.EndsWith(")"))
                {
                    return factory.CreateLenFuncStep(st);
                }

                return null;
            }).Where(st => st != null).ToList().As<List<ITransformStep>>();
        }
    }
}
