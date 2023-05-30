using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Newtonsoft.Json.Linq;
using Rencord.PissBot.Core;
using System.Linq;
using System.Security.Authentication.ExtendedProtection;
using System.Threading.Channels;

namespace Rencord.PissBot.Droplets.Commands
{
    public class LookingForPissCommand : ICommand
    {
        public string Name => "lookingforpiss";
        public const string EnableOption = "enable";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";

        public LookingForPissCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable/disable piss reacts and manage excluded channels (use 1 option at a time)") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the piss reaccs", isRequired: false)
                   .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from reactions", isRequired: false)
                   .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.FirstOrDefault();
            var config = guildData.GetOrAddData(() => new LookingForPissConfiguration());

            return opt?.Value switch
            {
                bool value when opt.Name == EnableOption => ToggleEnable(command, guildData, config, value),
                IChannel channel when opt.Name == ExcludeChannelOption => ExcludeChannel(command, guildData, config, channel),
                IChannel channel when opt.Name == RemoveExcludeOption => UnexcludeChannel(command, guildData, config, channel),
                _ => Respond((DataState.Pristine, DataState.Pristine), config, command, guildData)
            };
        }

        private Task<(DataState Guild, DataState User)> UnexcludeChannel(SocketSlashCommand command, GuildData guildData, LookingForPissConfiguration config, IChannel channel)
        {
            var removed = config.ExcludedChannels.RemoveAll(x => x.Id == channel.Id);
            return Task.FromResult((removed > 0 ? DataState.Modified : DataState.Pristine, DataState.Pristine));
        }

        private Task<(DataState Guild, DataState User)> ExcludeChannel(SocketSlashCommand command, GuildData guildData, LookingForPissConfiguration config, IChannel channel)
        {
            if (!config.ExcludedChannels.Any(x => x.Id == channel.Id))
            {
                config.ExcludedChannels.Add(new ChannelSummary { Id = channel.Id, Name = channel.Name });
                return Task.FromResult((DataState.Modified, DataState.Pristine));
            }
            return Task.FromResult((DataState.Pristine, DataState.Pristine));
        }

        private Task<(DataState Guild, DataState User)> ToggleEnable(SocketSlashCommand command, GuildData guildData, LookingForPissConfiguration config, bool value)
        {
            var modified = config.EnableLookingForPiss == value
                ? DataState.Pristine
                : DataState.Modified;
            config.EnableLookingForPiss = value;
            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      LookingForPissConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Loooking for Piss configuration")
              .WithDescription($"The current configuration of PissBot looking for piss on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableLookingForPiss).WithIsInline(true),
                new EmbedFieldBuilder().WithName("excluded").WithValue(string.Join(", ", config.ExcludedChannels.Select(x => x.Name))).WithIsInline(false))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
