using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets
{
    public class PissBotLookingForPiss : IPissDroplet
    {
        private readonly List<GuildOptions> options;
        private CancellationToken stopToken;

        public PissBotLookingForPiss(IOptions<List<GuildOptions>> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Start(IDiscordClient client, CancellationToken stopToken)
        {
            if (client is not DiscordSocketClient sc) throw new ArgumentException("client must be a socket client as the game uses events", nameof(client));
            this.stopToken = stopToken;
            sc.MessageReceived += MessageReceived;
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = options.FirstOrDefault(x => x.Id == stc.Guild.Id);
            if (guild == null || !guild.EnableLookingForPiss) return;

            if (arg.Content is not null && arg.Content.ToLower().Contains("piss"))
            {
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
            }
        }
    }
}
