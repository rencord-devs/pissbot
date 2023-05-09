using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Options;

namespace Rencord.PissBot.Core
{
    public class PissBotService : BackgroundService, IHostedService
    {
        private readonly IDiscordClientFactory discordClientFactory;
        private readonly IEnumerable<IPissDroplet> pissDroplets;
        private DiscordSocketClient? discordClient;

        public PissBotService(IDiscordClientFactory discordClientFactory,
                              IEnumerable<IPissDroplet> pissDroplets)
        {
            this.discordClientFactory = discordClientFactory;
            this.pissDroplets = pissDroplets;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            stoppingToken.Register(Stop);
            discordClient ??= await discordClientFactory.GetClient();
            if (pissDroplets?.Any() == true)
            {
                foreach (var droplet in pissDroplets)
                {
                    await droplet.Start(discordClient, stoppingToken);
                }
            }
            await discordClient.StartAsync();
            await discordClient.SetActivityAsync(new StreamingGame("piss", "https://www.youtube.com/renmakesmusic"));
        }

        private async void Stop()
        {
            if (discordClient is null) return;
            await discordClient.StopAsync();
        }
    }

    /// <summary>
    /// A piss droplet is a unit of functionality that is exposed by PissBot.
    /// </summary>
    public interface IPissDroplet
    {
        Task Start(DiscordSocketClient client, CancellationToken stopToken);
    }
}