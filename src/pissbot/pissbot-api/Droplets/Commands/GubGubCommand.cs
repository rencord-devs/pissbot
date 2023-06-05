using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class GubGubCommand : ICommand
    {
        public string Name => "gubgub";
        public const string EnableOption = "enable";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";

        public GubGubCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable reacts to posts that say Gub-Gub") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the Gub-Gub reaccs", isRequired: true)
                   .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from reactions", isRequired: false)
                   .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.First(x => x.Name == EnableOption);
            var config = guildData.GetOrAddData(() => new GubGubConfiguration());
            var modified = DataState.Pristine;

            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);

            var enableOpt = command.Data.Options.FirstOrDefault(x => x.Name == EnableOption);
            if (enableOpt?.Value is bool value)
            {
                if (config.EnableGubGub != value)
                    modified = DataState.Modified;
                config.EnableGubGub = value;
            }

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
                                                                      GubGubConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Gub-Gub configuration")
              .WithDescription($"The current configuration of PissBot Gub-Gub on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableGubGub).WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
