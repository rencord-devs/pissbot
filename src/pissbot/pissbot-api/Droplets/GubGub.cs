using Discord;
using Discord.WebSocket;
using Newtonsoft.Json.Bson;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;
using System.Collections.Generic;
using System.Reflection.Metadata.Ecma335;

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

    public class PrideRole : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private readonly ILogger<PrideRole> logger;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;
        private List<SocketRole> roles = new List<SocketRole>();
        private List<Color> colors = new List<Color>
        {
            Color.Red,
            Color.Orange,
            Color.Gold,
            Color.Green,
            Color.Blue,
            Color.Purple,
            Color.DarkPurple
        };
        private int color = 0;

        public PrideRole(IGuildDataPersistence guildDataStore, ILogger<PrideRole> logger)
        {
            this.guildDataStore = guildDataStore;
            this.logger = logger;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            stopToken.Register(Stop);
            client.Ready += Ready;
            Task.Run(Run);
            return Task.CompletedTask;
        }

        private void Stop()
        {
            if (this.client is null) return;
            this.client.Ready -= Ready;
            this.client.MessageReceived -= MessageReceived;
        }

        private async Task Run()
        {
            while (!stopToken.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(1000);
                    List<SocketRole> r;
                    var nextColor = colors[color++];
                    if (color == colors.Count) color = 0;
                    lock (roles)
                    {
                        r = new List<SocketRole>(roles);
                    }
                    foreach (var role in r)
                    {
                        await role.ModifyAsync(x => x.Color = nextColor);
                    }
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Exception in PrideRole.Run");
                }
            }
        }

        public Task Reinit() => 
            Ready();

        private async Task Ready()
        {
            if (client is not null)
            {
                lock(roles)
                {
                    roles.Clear();
                }
                foreach (var guild in client.Guilds)
                {
                    var guildData = await guildDataStore.GetData(guild.Id);
                    var config = guildData.GetOrAddData(() => new PrideRoleConfiguration());
                    if (config.EnablePrideRole && config.PrideRole is not null)
                    {
                        try
                        {
                            roles.Add(guild.GetRole(config.PrideRole.Id));

                        }
                        catch (Exception ex)
                        {

                        }
                    }
                }
            }
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
