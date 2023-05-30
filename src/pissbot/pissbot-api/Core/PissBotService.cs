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
            if (discordClient is not null) return;
            discordClient = await discordClientFactory.InitialiseClient();
            if (pissDroplets?.Any() == true)
            {
                foreach (var droplet in pissDroplets)
                {
                    await droplet.Start(discordClient, stoppingToken);
                }
            }
            stoppingToken.Register(Stop); // register this callback after the droplets register theirs, so it is called last when the delegates are invoked
            await discordClient.StartAsync();
            await discordClient.SetActivityAsync(new StreamingGame("piss", "https://www.youtube.com/renmakesmusic"));
        }

        private void Stop() =>
            discordClientFactory.DisposeClient().Wait();
    }

    /// <summary>
    /// A piss droplet is a unit of functionality that is exposed by PissBot.
    /// </summary>
    public interface IPissDroplet
    {
        Task Start(DiscordSocketClient client, CancellationToken stopToken);
    }
}