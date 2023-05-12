using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;
using System.Diagnostics;
using System.Globalization;

namespace Rencord.PissBot.Droplets
{

    public class PissBotLookingForPiss : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public PissBotLookingForPiss(IGuildDataPersistence guildDataStore)
        {
            this.guildDataStore = guildDataStore;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            client.Ready += Ready;
            return Task.CompletedTask;
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
            var config = guild.GetOrAddData(() => new LookingForPissConfiguration());
            if (!config.EnableLookingForPiss) return;

            if (arg.Content is not null && arg.Content.ToLower().Contains("piss"))
            {
                "piss".Split("piss", StringSplitOptions.None);
                var user = stc.Guild.GetUser(arg.Author.Id);
                if (user?.Roles is null || !user.Roles.Any(x => x.Name?.ToLower().Contains("piss") == true)) return;
                try
                {
                    await arg.AddReactionAsync(Emote.Parse("<:notp:1000806527965347922>"));
                }
                catch
                {
                    await arg.AddReactionAsync(Emote.Parse("<:notp:1104541579521306684>"));
                }
                config.AddPiss(user.Id, arg.Content.ToLower().Split("piss").Length - 1, user.Mention);
                await guildDataStore.SaveData(guild.Id);
            }
        }
    }
}
