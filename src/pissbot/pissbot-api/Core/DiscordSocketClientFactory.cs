using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Rencord.PissBot.Core
{
    public class DiscordSocketClientFactory : IDiscordClientFactory
    {
        private readonly DiscordBotOptions discordOpts;

        public DiscordSocketClientFactory(IOptions<DiscordBotOptions> discordOpts)
        {
            this.discordOpts = discordOpts?.Value ?? throw new ArgumentNullException(nameof(discordOpts));
        }

        public async Task<IDiscordClient> GetClient()
        {
            var client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                MessageCacheSize = 100
            });
            await client.LoginAsync(TokenType.Bot, discordOpts.Token);
            return client;
        }
    }
}