using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace Rencord.PissBot.Persistence
{
    public class CosmosTypedSerializer : CosmosSerializer
    {
        public override T FromStream<T>(Stream stream)
        {
            using var sr = new StreamReader(stream, System.Text.Encoding.UTF8);
            var body = sr.ReadToEnd();
            return JsonConvert.DeserializeObject<T>(body, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            })!;
        }

        public override Stream ToStream<T>(T input)
        {
            var body = JsonConvert.SerializeObject(input, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            })!;
            return new MemoryStream(System.Text.Encoding.UTF8.GetBytes(body));
        }
    }
}