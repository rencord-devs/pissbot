using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class SentencesCommand : ICommand
    {
        public string Name => "sentences";
        public const string EnableOption = "enable";
        public const string GameChannelOption = "gamechannel";
        public const string ResultChannelOption = "resultchannel";
        public const string RemoveResultChannelOption = "removeresultchannel";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable the sentence game")
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the sentence game", isRequired: false)
                   .AddOption(GameChannelOption, ApplicationCommandOptionType.Channel, "the channel to play in", isRequired: false)
                   .AddOption(ResultChannelOption, ApplicationCommandOptionType.Channel, "the optional additional channel to post results in", isRequired: false)
                   .AddOption(RemoveResultChannelOption, ApplicationCommandOptionType.Boolean, "set to true to remove existing results channel and only post results in the game channel", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new SentenceGameConfiguration());
            var modified = DataState.Pristine;
            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);
            var enableOpt = command.Data.Options.FirstOrDefault(x => x.Name == EnableOption);
            if (enableOpt?.Value is bool value)
            {
                if (config.EnableSentenceGame != value)
                    modified = DataState.Modified;
                config.EnableSentenceGame = value;
            }

            var channelOpt = command.Data.Options.FirstOrDefault(x => x.Name == GameChannelOption);
            if (channelOpt?.Value is IGuildChannel channel)
            {
                if (config.GameChannel != channel.Id)
                    modified = DataState.Modified;
                config.GameChannel = channel.Id;
            }

            var reultChannelOpt = command.Data.Options.FirstOrDefault(x => x.Name == ResultChannelOption);
            if (reultChannelOpt?.Value is IGuildChannel channel2)
            {
                if (config.ResultsChannel != channel2.Id)
                    modified = DataState.Modified;
                config.ResultsChannel = channel2.Id;
            }

            var removeReultChannelOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveResultChannelOption);
            if (removeReultChannelOpt?.Value is bool value2 && value2 && config.ResultsChannel.HasValue)
            {
                modified = DataState.Modified;
                config.ResultsChannel = null;
            }

            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      SentenceGameConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Sentence Game configuration")
              .WithDescription($"The current configuration of middle finger on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableSentenceGame).WithIsInline(true),
                new EmbedFieldBuilder().WithName("game channel").WithValue(config.GameChannel).WithIsInline(true),
                new EmbedFieldBuilder().WithName("results channel").WithValue(config.ResultsChannel).WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
