﻿using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using Rencord.PissBot.Droplets.Commands;
using Rencord.PissBot.Persistence;
using System;
using System.Runtime.InteropServices;

namespace Rencord.PissBot.Droplets
{
    public class CommandManager : IPissDroplet
    {
        private readonly IEnumerable<ICommand> commands;
        private readonly IGuildDataPersistence guildDataStore;
        private readonly IUserDataPersistence userDataStore;
        private readonly ILogger<CommandManager> logger;
        private readonly DiscordBotOptions options;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public CommandManager(IEnumerable<ICommand> commands,
                              IGuildDataPersistence guildDataStore,
                              IUserDataPersistence userDataStore,
                              IOptions<DiscordBotOptions> options,
                              ILogger<CommandManager> logger)
        {
            this.commands = commands ?? throw new ArgumentNullException(nameof(commands));
            this.guildDataStore = guildDataStore;
            this.userDataStore = userDataStore;
            this.logger = logger;
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
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
            this.client.SlashCommandExecuted -= CommandExecuted;
            this.client.ModalSubmitted -= ModalSubmitted;
        }

        private async Task Ready()
        {
            if (client is null) return;

            foreach (var guild in client.Guilds)
            {
                var existingCommands = await guild.GetApplicationCommandsAsync();
                await SyncSlashCommands(guild, existingCommands);
                await SyncMessageCommands(guild, existingCommands);
            }
            client.SlashCommandExecuted += CommandExecuted;
            client.ModalSubmitted += ModalSubmitted;
            client.MessageReceived += MessageReceived;
            client.MessageCommandExecuted += MessageCommandExecuted;
        }

        private async Task SyncSlashCommands(SocketGuild guild, IReadOnlyCollection<SocketApplicationCommand> existingCommands)
        {
            foreach (var command in commands)
            {
                if (stopToken.IsCancellationRequested) return;
                var guildCommand = new SlashCommandBuilder();
                await command.Configure(guildCommand);
                var existingCommand = existingCommands.FirstOrDefault(x => x.Name == command.Name && x.ApplicationId == options.ApplicationId);
                if (existingCommand is not null)
                {
                    // command authors can change a minor detail in the description to force a command rebuild on release (e.g. remove/add period)
                    if (guildCommand.Description == existingCommand.Description)
                        continue;
                    else
                        await existingCommand.DeleteAsync();
                }

                await guild.CreateApplicationCommandAsync(guildCommand.Build());
            }
        }

        private async Task SyncMessageCommands(SocketGuild guild, IReadOnlyCollection<SocketApplicationCommand> existingCommands)
        {
            foreach (var command in commands.OfType<IMessageCommand>())
            {
                if (stopToken.IsCancellationRequested) return;

                foreach (var cmdName in command.MessageCommands)
                {
                    var guildCommand = new MessageCommandBuilder();
                    guildCommand.WithDefaultMemberPermissions(GuildPermission.ManageMessages);
                    guildCommand.WithName(cmdName);
                    var existingCommand = existingCommands.FirstOrDefault(x => x.Name == cmdName && x.ApplicationId == options.ApplicationId);
                    if (existingCommand is null)
                    {
                        await guild.CreateApplicationCommandAsync(guildCommand.Build());
                    }

                }
            }
        }

        private async Task MessageCommandExecuted(SocketMessageCommand arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (!arg.GuildId.HasValue) return;
            var handler = commands.OfType<IMessageCommand>().FirstOrDefault(x => x.MessageCommands.Contains(arg.CommandName));
            if (handler is null) return;

            if (handler is IBotUserAwareCommand buac)
            {
                buac.BotUser = client!.CurrentUser;
            }

            var t1 = guildDataStore.GetData(arg.GuildId.Value);
            var t2 = userDataStore.GetData(arg.User.Id);
            await t1; await t2;

            try
            {
                var modified = await handler.Handle(arg, t1.Result, t2.Result);

                Task t3 = Task.CompletedTask, t4 = Task.CompletedTask;
                if (modified.User == DataState.Modified) t3 = userDataStore.SaveData(arg.User.Id);
                if (modified.Guild == DataState.Modified) t4 = guildDataStore.SaveData(arg.GuildId.Value);
                await t3; await t4;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in message command handler");
            }
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var msg = arg.Content?.ToLower();
            if (msg is null || !commands.OfType<ITextCommand>().Any(x => msg.StartsWith(x.Command))) return;
            var guild = await guildDataStore.GetData(stc.Guild.Id);
            var config = guild.GetOrAddData(() => new TextCommandConfiguration());
            if (!config.EnableTextCommands) return;
            if (config.ExcludedChannels.Any(x => x.Id == stc.Id)) return;

            var user = await userDataStore.GetData(arg.Author.Id);
            foreach (var command in commands.OfType<ITextCommand>().Where(x => msg?.StartsWith(x.Command) == true)) 
            {
                try
                {
                    var modified = await command.Handle(arg, guild, user);
                    Task t1 = Task.CompletedTask, t2 = Task.CompletedTask;
                    if (modified.User == DataState.Modified) t1 = userDataStore.SaveData(user.Id);
                    if (modified.Guild == DataState.Modified) t2 = guildDataStore.SaveData(guild.Id);
                    await t1; await t2;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error in text command handler");
                }
            }
        }

        private async Task ModalSubmitted(SocketModal modal)
        {
            if (stopToken.IsCancellationRequested) return;
            if (!modal.GuildId.HasValue) return;
            var handler = commands.Where(x => x is IModalCommand).Cast<IModalCommand>().FirstOrDefault(x => x.ModalIds?.Contains(modal.Data.CustomId) == true);
            if (handler is null) return;

            var t1 = guildDataStore.GetData(modal.GuildId.Value);
            var t2 = userDataStore.GetData(modal.User.Id);
            await t1; await t2;

            try
            {
                var modified = await handler.HandleModal(modal, t1.Result, t2.Result);
                if (!modal.HasResponded)
                {
                    await modal.DeferAsync(ephemeral: true);
                }
                Task t3 = Task.CompletedTask, t4 = Task.CompletedTask;
                if (modified.User == DataState.Modified) t3 = userDataStore.SaveData(modal.User.Id);
                if (modified.Guild == DataState.Modified) t4 = guildDataStore.SaveData(modal.GuildId.Value);
                await t3; await t4;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in command modal handler");
            }
        }

        private async Task CommandExecuted(SocketSlashCommand arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (!arg.GuildId.HasValue) return;
            var handler = commands.FirstOrDefault(x => x.Name == arg.CommandName);
            if (handler is null) return;

            if (handler is IBotUserAwareCommand buac)
            {
                buac.BotUser = client!.CurrentUser;
            }

            var t1 = guildDataStore.GetData(arg.GuildId.Value);
            var t2 = userDataStore.GetData(arg.User.Id);
            await t1; await t2;

            try
            {
                var modified = await handler.Handle(arg, t1.Result, t2.Result);

                Task t3 = Task.CompletedTask, t4 = Task.CompletedTask;
                if (modified.User == DataState.Modified) t3 = userDataStore.SaveData(arg.User.Id);
                if (modified.Guild == DataState.Modified) t4 = guildDataStore.SaveData(arg.GuildId.Value);
                await t3; await t4;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Error in command handler");
            }

        }
    }
}
