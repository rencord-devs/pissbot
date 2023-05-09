using Discord;
using Discord.WebSocket;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Persistence
{
    public class CosmosDbGuildPersistence : CosmosDbPersistence<GuildData>, IGuildDataPersistence
    {
        public const string ContainerName = "guild";
        private readonly List<GuildOptions> guildOptions;
        private readonly IDiscordClientFactory discordClientFactory;
        private readonly BlobGuildPersistence blobGuildPersistence;
        private IDiscordClient? discordClient;

        public CosmosDbGuildPersistence(Database db,
                                        IOptions<List<GuildOptions>> guildOptions,
                                        IDiscordClientFactory discordClientFactory,
                                        BlobGuildPersistence blobGuildPersistence) 
            : base(db, ContainerName)
        {
            this.guildOptions = guildOptions?.Value ?? throw new ArgumentNullException(nameof(guildOptions));
            this.discordClientFactory = discordClientFactory ?? throw new ArgumentNullException(nameof(discordClientFactory));
            this.blobGuildPersistence = blobGuildPersistence;
        }

        protected override async Task<GuildData> NewData(ulong id)
        {
            // migrate legacy guilds - check the blob store to see if there's existing data to migrate (this can be removed in a subsequent release)
            var legacy = await blobGuildPersistence.GetData(id);
            var oldOpts = guildOptions.FirstOrDefault(x => x.Id == id);

            var result = legacy is not null && legacy.Data.Any()
                ? UpdateLegacy(legacy, id, oldOpts)
                : new GuildData
                  {
                      Id = id,
                      Name = oldOpts?.Name ?? await GetGuildName(id),
                      Data = new List<object>()
                  };


            if (oldOpts is not null)
            {
                if (oldOpts.SentenceGame is not null)
                {
                    oldOpts.SentenceGame.EnableSentenceGame = oldOpts.SentenceGame.GameChannel.HasValue && oldOpts.SentenceGame.GameChannel.Value != 0;
                    result.SetData(oldOpts.SentenceGame);
                }
                result.SetData(new LookingForPissConfiguration { EnableLookingForPiss = oldOpts.EnableLookingForPiss });
            }

            return result;
        }

        private GuildData UpdateLegacy(GuildData legacy, ulong id, GuildOptions? oldOpts)
        {
            legacy.Id = id;
            if (oldOpts is null) return legacy;
            legacy.Name = oldOpts.Name;
            return legacy;
        }

        private async Task<string> GetGuildName(ulong id)
        {
            var guild = guildOptions.FirstOrDefault(x => x.Id == id);
            if (string.IsNullOrWhiteSpace(guild?.Name))
            {
                discordClient ??= await discordClientFactory.GetClient();
                return (await discordClient.GetGuildAsync(id)).Name;
            }
            else return guild.Name;
        }
    }

    public class CosmosDbUserPersistence : CosmosDbPersistence<UserData>, IUserDataPersistence
    {
        public const string ContainerName = "user";
        private readonly IDiscordClientFactory discordSocketClientFactory;
        private DiscordSocketClient? client;

        public CosmosDbUserPersistence(Database db, IDiscordClientFactory discordSocketClientFactory)
            : base(db, ContainerName)
        {
            this.discordSocketClientFactory = discordSocketClientFactory;
        }

        protected override async Task<UserData> NewData(ulong id)
        {
            this.client ??= await discordSocketClientFactory.GetClient();
            var user = await client.GetUserAsync(id);
            return new UserData
            {
                Id = id,
                DiscordUserName = user.Username,
                Mention = user.Mention,
                Data = new List<object>()
            };
        }
    }
}