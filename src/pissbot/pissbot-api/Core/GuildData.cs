namespace Rencord.PissBot.Core
{
    public class GuildData
    {
        public ulong Id { get; set; }
        public string? Name { get; set; }
        public List<object> Data { get; set; } = new List<object>();
        public T? GetData<T>() where T : class
        {
            return Data?.FirstOrDefault(x => x is T) as T ?? default;
        }
        public T GetOrAddData<T>(Func<T> func) where T : class
        {
            return Data?.FirstOrDefault(x => x is T) as T ?? Add(func());
        }

        private T Add<T>(T t) where T : class
        {
            Data.Add(t);
            return t;
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
    }
}