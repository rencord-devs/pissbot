using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets
{
    public class GubGub : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public GubGub(IGuildDataPersistence guildDataStore)
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
            var config = guild.GetOrAddData(() => new GubGubConfiguration());
            if (!config.EnableGubGub) return;

            var content = arg.Content?.ToLower();
            if (content is not null && (content.Contains("gub-gub") || content.Contains("gub gub") || content.Contains("gubgub")))
            {
                try
                {
                    await arg.AddReactionAsync(Emote.Parse("<:letsgo:1020270999037554719>"));
                }
                catch (Exception ex)
                {
                    try
                    {
                        await arg.AddReactionAsync(Emoji.Parse("😯"));
                    }
                    catch (Exception ex2)
                    {

                    }
                }
            }
        }
    }
}
