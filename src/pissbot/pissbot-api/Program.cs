using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using Rencord.PissBot.Droplets;
using System.Diagnostics;

namespace Rencord.PissBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            if (Debugger.IsAttached) builder.Environment.EnvironmentName = "Development";
            builder.Services.Configure<DiscordBotOptions>(
                builder.Configuration.GetSection(DiscordBotOptions.DiscordBot));

            builder.Services.Configure<BlobStoreOptions>(
                builder.Configuration.GetSection(BlobStoreOptions.BlobStore));

            builder.Services.Configure<List<GuildOptions>>(
                builder.Configuration.GetSection(GuildOptions.Guilds));

            builder.Services.AddApplicationInsightsTelemetry();
            builder.Services.AddSingleton<IDiscordClientFactory, DiscordSocketClientFactory>();
            builder.Services.AddSingleton<IGuildDataPersistence, BlobGuildPersistence>();
            builder.Services.AddSingleton<IPissDroplet, SentenceGame>();
            builder.Services.AddSingleton<IPissDroplet, PissBotLookingForPiss>();
            
            // Add services to the container.
            builder.Services.AddControllers();
            builder.Services.AddHostedService<PissBotService>();

            var app = builder.Build();

            // Configure the HTTP request pipeline.

            app.UseHttpsRedirection();
            app.UseAuthorization();

            app.MapControllers();

            app.Run();
        }
    }
}