using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace ActorBackend.Data
{
    [JsonConverter(typeof(TransformationDesciptionConverter))]
    public class TransformationDescription
    {
        public List<string>? RemoveNodes { get; set; }

        public Dictionary<string, List<string>> Transformations { get; set; }
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

            json.Remove("RemoveNodes");

            var childReader = json.CreateReader();
            result.Transformations = serializer.Deserialize<Dictionary<string, List<string>>>(childReader)!;

            return result;
        }

        public override void WriteJson(JsonWriter writer, object? value, JsonSerializer serializer)
        {
            throw new NotImplementedException();
        }
    }
}
