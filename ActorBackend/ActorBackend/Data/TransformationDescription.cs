using ActorBackend.Actors;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ActorBackend.Data
{
    [JsonConverter(typeof(TransformationDesciptionConverter))]
    public class TransformationDescription
    {
        public List<string>? RemoveNodes { get; set; }

        public List<TransformationChange> Changes { get; set; }



        public TransformationSpecification ToSpecification()
        {
            var spec = new TransformationSpecification();

            if (RemoveNodes != null && RemoveNodes.Count > 0)
            {
                spec.ToRemove.AddRange(RemoveNodes);
            }

            spec.Changes.AddRange(Changes.Select(ch => ch.ToProtoNode()));

            return spec;
        }

        public static TransformationDescription FromSpecification(TransformationSpecification spec)
        {
            var desc = new TransformationDescription();

            desc.RemoveNodes = spec.ToRemove.ToList();
            desc.Changes = spec.Changes.Select(ch => TransformationChange.FromProtoNode(ch)).ToList();

            return desc;
        }
    }

    public class TransformationDesciptionConverter : JsonConverter
    {
        public override bool CanConvert(Type objectType)
        {
            return objectType == typeof(TransformationDescription);
        }

        public override object? ReadJson(JsonReader reader, Type objectType, object? existingValue, JsonSerializer serializer)
        {
            JObject json = JObject.Load(reader);
            TransformationDescription result = new TransformationDescription();

            if (json.ContainsKey("RemoveNodes"))
            {
                var removeReader = json["RemoveNodes"]?.CreateReader();
                if (removeReader != null)
                    result.RemoveNodes = serializer.Deserialize<List<string>>(removeReader);
            }

            var childReader = json["Changes"]!.CreateReader();
            result.Changes = serializer.Deserialize<List<TransformationChange>>(childReader)!;

            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }


    public class TransformationChange
    {
        public string StoredIn { get; set; }

        public List<string> Steps { get; set; }


        public TransformationSpecification.Types.Transformation ToProtoNode()
        {
            var res = new TransformationSpecification.Types.Transformation();

            res.StoredIn = StoredIn;
            res.Steps.AddRange(Steps);

            return res;
        }

        public static TransformationChange FromProtoNode(TransformationSpecification.Types.Transformation node)
        {
            var res = new TransformationChange();

            res.StoredIn = node.StoredIn;
            res.Steps = node.Steps.ToList();

            return res;
        }
    }
}
