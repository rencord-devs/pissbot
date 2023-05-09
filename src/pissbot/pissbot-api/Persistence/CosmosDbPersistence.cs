using Microsoft.Azure.Cosmos;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Persistence
{
    public abstract class CosmosDbPersistence<T> : CachedPersistence<T>, IDataPersistence<T> where T : IId, new()
    {
        private readonly Container cosmosContainer;

        public CosmosDbPersistence(Database db, string containerName)
        {
            cosmosContainer = db.CreateContainerIfNotExistsAsync(containerName, "/id").Result.Container;
        }

        protected override async Task<T> GetDataFromStore(ulong id)
        {
            try
            {
                return await cosmosContainer.ReadItemAsync<T>(id.ToString(), new PartitionKey(id.ToString()));
            }
            catch (CosmosException ex)
            when (ex.RetryAfter.HasValue)
            {
                await Task.Delay(ex.RetryAfter.Value);
                return await GetData(id);
            }
            catch (CosmosException ex)
            when (ex.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                return await NewData(id);
            }
        }

        protected abstract Task<T> NewData(ulong id);

        protected override async Task SaveDataToStore(T? data)
        {
            if (data is null) return;
            await cosmosContainer.UpsertItemAsync(data, new PartitionKey(data.Id.ToString()));
        }
    }
}