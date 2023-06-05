using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class RenWatchCommand : ICommand
    {
        public string Name => "renwatch";
        public const string EnableOption = "enable";
        public const string AddTermOption = "addterm";
        public const string RemoveTermOption = "removeterm";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";

        public RenWatchCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable reacts to posts containing a Ren song title and manage song titles (use 1 option at a time)") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the Ren song title reaccs", isRequired: false)
                   .AddOption(AddTermOption, ApplicationCommandOptionType.String, "add a watch term", isRequired: false)
                   .AddOption(RemoveTermOption, ApplicationCommandOptionType.String, "remove a watch term", isRequired: false)
                   .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from reactions", isRequired: false)
                   .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.FirstOrDefault();
            var config = guildData.GetOrAddData(() => new RenWatchConfiguration());

            return opt?.Value switch
            {
                bool value when opt.Name == EnableOption => ToggleEnable(command, guildData, config, value),
                string term when opt.Name == AddTermOption => AddTerm(command, guildData, config, term),
                string term when opt.Name == RemoveTermOption => RemoveTerm(command, guildData, config, term),
                IChannel channel when opt.Name == ExcludeChannelOption => ExcludeChannel(command, guildData, config, channel),
                IChannel channel when opt.Name == RemoveExcludeOption => UnexcludeChannel(command, guildData, config, channel),
                _ => Respond((DataState.Pristine, DataState.Pristine), config, command, guildData)
            };
        }

        private Task<(DataState Guild, DataState User)> UnexcludeChannel(SocketSlashCommand command, GuildData guildData, RenWatchConfiguration config, IChannel channel)
        {
            var removed = config.ExcludedChannels.RemoveAll(x => x.Id == channel.Id);
            return Respond((removed > 0 ? DataState.Modified : DataState.Pristine, DataState.Pristine), config, command, guildData);
        }

        private Task<(DataState Guild, DataState User)> ExcludeChannel(SocketSlashCommand command, GuildData guildData, RenWatchConfiguration config, IChannel channel)
        {
            if (!config.ExcludedChannels.Any(x => x.Id == channel.Id))
            {
                config.ExcludedChannels.Add(new ChannelSummary { Id = channel.Id, Name = channel.Name });
                return Respond((DataState.Modified, DataState.Pristine), config, command, guildData);
            }
            return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
        }

        private Task<(DataState Guild, DataState User)> RemoveTerm(SocketSlashCommand command, GuildData guildData, RenWatchConfiguration config, string term)
        {
            if (config.WatchTerms.Contains(term.ToLower()))
            {
                config.WatchTerms.Remove(term.ToLower());
                return Respond((DataState.Modified, DataState.Pristine), config, command, guildData);
            }
            return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
        }

        private Task<(DataState Guild, DataState User)> AddTerm(SocketSlashCommand command, GuildData guildData, RenWatchConfiguration config, string term)
        {
            if (!config.WatchTerms.Contains(term.ToLower()))
            {
                config.WatchTerms.Add(term.ToLower());
                return Respond((DataState.Modified, DataState.Pristine), config, command, guildData);
            }
            return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
        }

        private Task<(DataState Guild, DataState User)> ToggleEnable(SocketSlashCommand command, GuildData guildData, RenWatchConfiguration config, bool value)
        {
            var modified = config.EnableRenWatch == value
                ? DataState.Pristine
                : DataState.Modified;
            config.EnableRenWatch = value;
            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      RenWatchConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            var fullList = string.Join(", ", config.WatchTerms);
            var list = fullList;
            if (fullList.Length > 1000)
            {
                list = new string(fullList.Take(1000).ToArray());
                list += "...";
            }
            if (string.IsNullOrWhiteSpace(list))
                list = "[no watch terms]";
            eb.WithTitle("RenWatch configuration")
              .WithDescription($"The current configuration of PissBot RenWatch on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableRenWatch).WithIsInline(true),
                new EmbedFieldBuilder().WithName("terms").WithValue(list).WithIsInline(false))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
