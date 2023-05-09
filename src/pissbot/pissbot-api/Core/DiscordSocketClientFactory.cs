using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Rencord.PissBot.Core
{
    public class DiscordSocketClientFactory : IDiscordClientFactory
    {
        private readonly DiscordBotOptions discordOpts;
        private DiscordSocketClient client;

        public DiscordSocketClientFactory(IOptions<DiscordBotOptions> discordOpts)
        {
            this.discordOpts = discordOpts?.Value ?? throw new ArgumentNullException(nameof(discordOpts));
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                MessageCacheSize = 100
            });
        }

        public async Task<DiscordSocketClient> GetClient()
        {
            if (client.LoginState == LoginState.LoggedOut)
            {
                await client.LoginAsync(TokenType.Bot, discordOpts.Token);
            }
            return client;
        }
    }
}