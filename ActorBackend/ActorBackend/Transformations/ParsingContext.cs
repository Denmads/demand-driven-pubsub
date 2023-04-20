using ActorBackend.Transformations.Inputs;
using System.Xml.Linq;
using static ActorBackend.Actors.SubscriptionQueryResponse.Types;

namespace ActorBackend.Transformations
{
    public class ParsingContext
    {
        private DataNodeCollection queriedNodes;

        private Dictionary<string, string> calculatedNodes = new Dictionary<string, string>();

        private (string, string) prevNode = ("","");

        public ParsingContext(DataNodeCollection queriedNodes)
        {
            this.queriedNodes = queriedNodes;

        }

        public bool IsPrevAllowed()
        {
            return prevNode.Item1 != "";
        }

        public void AddCalculatedNode(string name, string datatype)
        {
            calculatedNodes[name] = datatype;
            prevNode = (name, datatype);
        }

        public bool isNodeName(string name)
        {
            return queriedNodes.Nodes.Keys.Contains(name) || calculatedNodes.ContainsKey(name);
        }


        public IInput CreateVariableInput(string name)
        {
            if (queriedNodes.Nodes.Keys.Contains(name)) {

                return queriedNodes.Nodes[name].DataType switch
                {
                    "string" => new Variable<string>(name, queriedNodes.Nodes[name].DataType),
                    "int" => new Variable<int>(name, queriedNodes.Nodes[name].DataType),
                    "float" => new Variable<float>(name, queriedNodes.Nodes[name].DataType),
                    "bool" => new Variable<bool>(name, queriedNodes.Nodes[name].DataType),
                    _ => throw new InvalidDataException("Invalid datatype: " + queriedNodes.Nodes[name].DataType)

                };
            }
            else
            {
                return calculatedNodes[name] switch
                {
                    "string" => new Variable<string>(name, calculatedNodes[name]),
                    "int" => new Variable<int>(name, calculatedNodes[name]),
                    "float" => new Variable<float>(name, calculatedNodes[name]),
                    "bool" => new Variable<bool>(name, calculatedNodes[name]),
                    _ => throw new InvalidDataException("Invalid datatype: " + queriedNodes.Nodes[name].DataType)
                };
            }
        }

        public IInput CreatePrevInput()
        {
            return prevNode.Item2 switch
            {
                "string" => new Previous<string>(prevNode.Item2),
                "int" => new Previous<int>(prevNode.Item2),
                "float" => new Previous<float>(prevNode.Item2),
                "bool" => new Previous<bool>(prevNode.Item2),
                _ => throw new InvalidDataException("Invalid datatype: " + prevNode.Item2)

            };
        }
    }
}
