using Rencord.PissBot.Core;
using System.Collections.Concurrent;

namespace Rencord.PissBot.Persistence
{
    public abstract class CachedPersistence<T> : IDataPersistence<T> where T : IId
    {
        public class CacheLock
        {
            public bool Saving { get; set; }
        }

        private readonly ConcurrentDictionary<ulong, (T Data, CacheLock Lock)> cache = new ConcurrentDictionary<ulong, (T Data, CacheLock Lock)>();

        protected CachedPersistence()
        {
        }

        public async Task<T> GetData(ulong id)
        {
            if (cache.TryGetValue(id, out var g)) return g.Data;
            return cache.GetOrAdd(id, (await GetDataFromStore(id), new CacheLock())).Data;
        }

        protected abstract Task<T> GetDataFromStore(ulong id);

        public async Task SaveData(ulong id)
        {
            if (cache.TryGetValue(id, out var g))
            {
                lock (g.Lock)
                {
                    if (g.Lock.Saving)
                    {
                        return;
                    }
                    g.Lock.Saving = true;
                }
                await SaveDataToStore(g.Data);
                lock (g.Lock)
                {
                    g.Lock.Saving = false;
                }
            }
        }

        protected abstract Task SaveDataToStore(T? data);
    }
}