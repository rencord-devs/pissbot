using Newtonsoft.Json;

namespace Rencord.PissBot.Core
{
    public interface IId
    {
        ulong Id { get; set; }
        string id { get; set; }
    }

    public class GuildData : TypedData, IId
    {
        [JsonIgnore]
        public ulong Id { get; set; }
        public string id // this has to be a string for cosmos to work
        { 
            get
            {
                return Id.ToString();
            } 
            set
            {
                if (ulong.TryParse(value, out var r))
                {
                    Id = r;
                }
                else throw new ArgumentException("not a ulong");
            }
        }
        public string? Name { get; set; }
    }

    public class UserData : TypedData, IId
    {
        [JsonIgnore]
        public ulong Id { get; set; }
        public string id // this has to be a string for cosmos to work
        {
            get
            {
                return Id.ToString();
            }
            set
            {
                if (ulong.TryParse(value, out var r))
                {
                    Id = r;
                }
                else throw new ArgumentException("not a ulong");
            }
        }
        public string? GuildUserName { get; set; }
        public string? DiscordUserName { get; set; }
        public string? Mention { get; set; }
    }

    public abstract class TypedData
    {
        public List<object> Data { get; set; } = new List<object>();

        public T? GetData<T>() where T : class
        {
            return Data?.FirstOrDefault(x => x is T) as T ?? default;
        }

        public T GetOrAddData<T>(Func<T> func) where T : class
        {
            return Data?.FirstOrDefault(x => x is T) as T ?? Add(func());
        }

        public void SetData<T>(T data) where T : class
        {
            var existing = GetData<T>();
            if (existing != null)
            {
                Data.Remove(existing);
            }
            Data.Add(data);
        }

        private T Add<T>(T t) where T : class
        {
            Data.Add(t);
            return t;
        }
    }
}