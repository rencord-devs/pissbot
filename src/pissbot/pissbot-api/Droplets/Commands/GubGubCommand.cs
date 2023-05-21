using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class GubGubCommand : ICommand
    {
        public string Name => "gubgub";
        public const string EnableOption = "enable";

        public GubGubCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable <:letsgo:1020270999037554719> reacts to posts that say Gub-Gub") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the Gub-Gub reaccs", isRequired: true);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.First(x => x.Name == EnableOption);
            var config = guildData.GetOrAddData(() => new GubGubConfiguration());
            if (opt.Value is not bool value) return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
            var modified = config.EnableGubGub == value
                ? DataState.Pristine
                : DataState.Modified;
            config.EnableGubGub = value;
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
