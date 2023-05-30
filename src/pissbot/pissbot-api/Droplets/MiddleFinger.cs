using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets
{
    public class MiddleFinger : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public MiddleFinger(IGuildDataPersistence guildDataStore)
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
            var config = guild.GetOrAddData(() => new MiddleFingerConfiguration());
            if (!config.EnableMiddleFinger) return;

            if (config.Users.Any(c => c.Id == arg.Author.Id))
            {
                await arg.AddReactionAsync(Emoji.Parse("🖕"));
                if (config.Time > 0)
                {
                    var botId = client!.CurrentUser.Id;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay((int)config.Time * 1000);
                        await arg.RemoveReactionAsync(Emoji.Parse("🖕"), botId);
                    });
                }
            }
        }
    }
}
