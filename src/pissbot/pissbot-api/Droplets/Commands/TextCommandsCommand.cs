﻿using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class TextCommandsCommand : ICommand
    {
        public string Name => "textcommands";
        public const string EnableOption = "enable";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";

        public TextCommandsCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Exclude channels from member's text commands.") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable all PissBot text commands", isRequired: false)
                   .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from member's text commands", isRequired: false)
                   .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new TextCommandConfiguration());
            var modified = DataState.Pristine;

            var enableOpt = command.Data.Options.FirstOrDefault(x => x.Name == EnableOption);
            if (enableOpt?.Value is bool value)
            {
                if (config.EnableTextCommands != value)
                    modified = DataState.Modified;
                config.EnableTextCommands = value;
            }

            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);

            var excludeOpt = command.Data.Options.FirstOrDefault(x => x.Name == ExcludeChannelOption);
            if (excludeOpt?.Value is IChannel chan)
            {
                if (!config.ExcludedChannels.Any(c => c.Id == chan.Id))
                {
                    modified = DataState.Modified;
                    config.ExcludedChannels.Add(new ChannelSummary { Id = chan.Id, Name = chan.Name });
                }
            }

            var removeExcludeOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveExcludeOption);
            if (removeExcludeOpt?.Value is IChannel chan2)
            {
                if (config.ExcludedChannels.Any(c => c.Id == chan2.Id))
                {
                    modified = DataState.Modified;
                    config.ExcludedChannels.RemoveAll(c => c.Id == chan2.Id);
                }
            }

            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      TextCommandConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            var val = string.Join(", ", config.ExcludedChannels.Where(x => x.Name is not null).Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(val))
                val = "[no exlcudes]";
            eb.WithTitle("Text Commands configuration")
              .WithDescription($"The current configuration of PissBot Text Commands on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableTextCommands).WithIsInline(true),
                new EmbedFieldBuilder().WithName("excluded").WithValue(val).WithIsInline(false))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }

    public class SpeakCommand : ICommand
    {
        public string Name => "speak";
        public const string MessageOption = "message";
        public const string ReplyToOption = "replyto";

        public SpeakCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Send a message as PissBot to the current channel.") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(MessageOption, ApplicationCommandOptionType.String, "the text to say", isRequired: true)
                   .AddOption(ReplyToOption, ApplicationCommandOptionType.String, "the message id to reply to", isRequired: false);
            return Task.CompletedTask;
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var msgOpt = command.Data.Options.FirstOrDefault(x => x.Name == MessageOption);
            if (msgOpt?.Value is string value)
            {
                await command.RespondAsync("ok", ephemeral: true);
                value = value.Replace(@"\r\n", Environment.NewLine);
                var replyToOpt = command.Data.Options.FirstOrDefault(x => x.Name == ReplyToOption);
                if (replyToOpt?.Value is string val2 && ulong.TryParse(val2, out var val3))
                {
                    await command.Channel.SendMessageAsync(text: value, messageReference: new MessageReference(val3));
                }
                else
                {
                    await command.Channel.SendMessageAsync(text: value);
                }
            }
            await command.RespondAsync("no message", ephemeral: true);
            return (DataState.Pristine, DataState.Pristine);
        }
    }
}
