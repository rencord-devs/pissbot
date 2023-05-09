using Azure.Storage.Blobs;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Persistence
{
    public abstract class BlobPersistence<T> : CachedPersistence<T>, IDataPersistence<T> where T : IId, new()
    {
        private readonly BlobStoreOptions options;
        private readonly BlobContainerClient client;

        public BlobPersistence(IOptions<BlobStoreOptions> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            client = new BlobContainerClient(this.options.ConnectionString, this.options.Container);
        }

        protected override async Task<T> GetDataFromStore(ulong id)
        {
            var blobClient = client.GetBlobClient(id.ToString());
            if ((await blobClient.ExistsAsync())?.Value != true)
            {
                return await NewData(id); // doesn't exist yet, return new data
            }

            var response = await blobClient.DownloadAsync();
            using var streamReader = new StreamReader(response.Value.Content);
            var str = await streamReader.ReadToEndAsync();
            var r = JsonConvert.DeserializeObject<T>(str, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All
            });
            return r ?? new T();
        }

        protected abstract Task<T> NewData(ulong id);

        protected override async Task SaveDataToStore(T? data)
        {
            if (data is null) return;
            var blobClient = client.GetBlobClient(data.Id.ToString());
            await blobClient.UploadAsync(new BinaryData(JsonConvert.SerializeObject(data, new JsonSerializerSettings
            {
                TypeNameHandling = TypeNameHandling.All,
            })), overwrite: true);
        }
    }
}