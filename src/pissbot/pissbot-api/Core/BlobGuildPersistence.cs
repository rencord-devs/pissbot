using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using System.Collections.Concurrent;

namespace Rencord.PissBot.Core
{
    public class BlobGuildPersistence : IGuildDataPersistence
    {
        public class GuildLock
        {
            public bool Saving { get; set; }
        }

        private readonly BlobStoreOptions options;
        private readonly BlobContainerClient client;
        private readonly ConcurrentDictionary<ulong, (GuildData Guild, GuildLock Lock)> cache = new ConcurrentDictionary<ulong, (GuildData Guild, GuildLock Lock)>();

        public BlobGuildPersistence(IOptions<BlobStoreOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.client = new BlobContainerClient(this.options.ConnectionString, this.options.Container);
        }

        public async Task<GuildData> GetGuild(ulong id)
        {
            if (cache.TryGetValue(id, out var g)) return g.Guild;
            var blobClient = client.GetBlobClient(id.ToString());
            if ((await blobClient.ExistsAsync())?.Value != true)
            {
                return cache.GetOrAdd(id, x => (new GuildData(), new GuildLock())).Guild; // guild doesn't exist yet, return empty data
            }

            var response = await blobClient.DownloadAsync();
            using var streamReader = new StreamReader(response.Value.Content);
            var str = await streamReader.ReadToEndAsync();
            var r = JsonConvert.DeserializeObject<GuildData>(str, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            if (r is null) return cache.GetOrAdd(id, x => (new GuildData(), new GuildLock())).Guild;
            return cache.GetOrAdd(id, (r, new GuildLock())).Guild;
        }

        public async Task SaveGuild(ulong id)
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
                var blobClient = client.GetBlobClient(id.ToString());
                await blobClient.UploadAsync(new BinaryData(JsonConvert.SerializeObject(g.Guild, new JsonSerializerSettings
                {
                    TypeNameHandling = TypeNameHandling.All,
                })), overwrite: true);
                lock (g.Lock)
                {
                    g.Lock.Saving = false;
                }
            }
        }
    }
}