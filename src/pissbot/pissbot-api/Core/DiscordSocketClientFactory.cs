using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Rencord.PissBot.Core
{
    public class DiscordSocketClientFactory : IDiscordClientFactory, IDisposable
    {
        private readonly DiscordBotOptions discordOpts;
        private DiscordSocketClient? client;

        public DiscordSocketClientFactory(IOptions<DiscordBotOptions> discordOpts)
        {
            this.discordOpts = discordOpts?.Value ?? throw new ArgumentNullException(nameof(discordOpts));
        }

        public async Task<DiscordSocketClient> GetClient() =>
            client ?? await InitialiseClient();

        public async Task<DiscordSocketClient> InitialiseClient()
        {
            client = new DiscordSocketClient(new DiscordSocketConfig
            {
                GatewayIntents = GatewayIntents.AllUnprivileged | GatewayIntents.MessageContent,
                MessageCacheSize = 100
            });
            await client.LoginAsync(TokenType.Bot, discordOpts.Token);
            return client;
        }

        public async Task DisposeClient()
        {
            if (client is null) return;
            await client.LogoutAsync();
            await client.StopAsync();
            await client.DisposeAsync();
            client = null;
        }

        public void Dispose() =>
            DisposeClient().Wait();
    }
}