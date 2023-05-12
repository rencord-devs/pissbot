using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class LookingForPissCommand : ICommand
    {
        public string Name => "lookingforpiss";
        public const string EnableOption = "enable";

        public LookingForPissCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable :notp: reacts to posts that say piss when made by a member with a role with piss in the name") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the piss reaccs", isRequired: true);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.First(x => x.Name == EnableOption);
            var config = guildData.GetOrAddData(() => new LookingForPissConfiguration());
            if (opt.Value is not bool value) return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
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
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableLookingForPiss).WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
