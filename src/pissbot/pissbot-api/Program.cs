using Discord;
using Discord.WebSocket;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using Rencord.PissBot.Droplets;
using Rencord.PissBot.Droplets.Commands;
using Rencord.PissBot.Persistence;
using System.Diagnostics;

namespace Rencord.PissBot
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddLogging();
            builder.Services.AddApplicationInsightsTelemetry();
            if (Debugger.IsAttached) builder.Environment.EnvironmentName = "Development";

            builder.Services.Configure<DiscordBotOptions>(
                builder.Configuration.GetSection(DiscordBotOptions.DiscordBot));

            builder.Services.Configure<BlobStoreOptions>(
                builder.Configuration.GetSection(BlobStoreOptions.BlobStore));

            builder.Services.Configure<CosmosDbOptions>(
                builder.Configuration.GetSection(CosmosDbOptions.CosmosDb));

            builder.Services.Configure<List<GuildOptions>>(
                builder.Configuration.GetSection(GuildOptions.Guilds));

            builder.Services.AddSingleton(x =>
            {
                var opts = x?.GetService<IOptions<CosmosDbOptions>>()?.Value;
                var connStr = opts?.ConnectionString ?? throw new ArgumentNullException("CosmosDb.ConnectionString");
                var cli = new CosmosClient(connStr, new CosmosClientOptions
                {
                    Serializer = new CosmosTypedSerializer()
                });
                var db = cli.CreateDatabaseIfNotExistsAsync(opts?.DbName ?? throw new ArgumentNullException("CosmosDb.DbName")).Result;
                return db.Database;
            });
            builder.Services.AddSingleton<IDiscordClientFactory, DiscordSocketClientFactory>();
            builder.Services.AddSingleton<BlobGuildPersistence>();
            builder.Services.AddSingleton<IGuildDataPersistence, CosmosDbGuildPersistence>();
            builder.Services.AddSingleton<IUserDataPersistence, CosmosDbUserPersistence>();

            foreach (var type in typeof(Program).Assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(IPissDroplet))))
            {
                builder.Services.AddSingleton(typeof(IPissDroplet), type);
            }

            foreach (var type in typeof(Program).Assembly.GetTypes().Where(x => x.IsClass && !x.IsAbstract && x.GetInterfaces().Any(y => y == typeof(ICommand))))
            {
                builder.Services.AddSingleton(typeof(ICommand), type);
            }
            
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