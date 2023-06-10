using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets
{
    public class KeywordReacts : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public KeywordReacts(IGuildDataPersistence guildDataStore)
        {
            this.guildDataStore = guildDataStore;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            stopToken.Register(Stop);
            client.Ready += Ready;
            return Task.CompletedTask;
        }

        private void Stop()
        {
            if (this.client is null) return;
            this.client.Ready -= Ready;
            this.client.MessageReceived -= MessageReceived;
        }

        private Task Ready()
        {
            if (client is not null) client.MessageReceived += MessageReceived;
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = await guildDataStore.GetData(stc.Guild.Id);
            var config = guild.GetOrAddData(() => new KeywordReactsConfiguration());
            if (!config.EnableKeywordReacts) return;
            if (config.ExcludedChannels.Any(x => x.Id == stc.Id)) return;

            #pragma warning disable CS0618 // Type or member is obsolete - this code removes the obsolete data from the store
            var oldConfig = guild.RemoveData<GubGubConfiguration>();
            if (oldConfig is not null)
            {
                // migrate existing thing
                config.WatchTerms["gub-gub"] = "<:letsgo:1020270999037554719>";
                config.WatchTerms["gub gub"] = "<:letsgo:1020270999037554719>";
                config.WatchTerms["gubgub"] = "<:letsgo:1020270999037554719>";
            }
            #pragma warning restore CS0618 // Type or member is obsolete

            var content = arg.Content?.ToLower();
            if (content is not null)
            {
                foreach (var term in config.WatchTerms.Where(x => content.Contains(x.Key)))
                {
                    try
                    {
                        await arg.AddReactionAsync(Emote.Parse(term.Value));
                    }
                    catch (Exception ex)
                    {
                    }
                }
            }
        }
    }
}
