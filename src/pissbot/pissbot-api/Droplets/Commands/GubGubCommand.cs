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
                   .WithDescription("Enable reacts to posts that say Gub-Gub") // NOTE: 100 chars max!
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

    public class PrideRoleCommand : ICommand
    {
        public string Name => "priderole";
        public const string EnableOption = "enable";
        public const string RoleOption = "role";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Pride Role (use one option at a time)") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the Pride Role", isRequired: false)
                   .AddOption(RoleOption, ApplicationCommandOptionType.Role, "set the pride role", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new PrideRoleConfiguration());
            var enable = command.Data.Options.FirstOrDefault(x => x.Name == EnableOption);
            if (enable?.Value is bool value)
            {
                config.EnablePrideRole = value;
                return Respond((DataState.Modified, DataState.Pristine), config, command, guildData);
            }

            var role = command.Data.Options.FirstOrDefault(x => x.Name == RoleOption);
            if (role?.Value is IRole rl)
            {
                config.PrideRole = new RoleSummary { Id = rl.Id, Mention = rl.Mention, Name = rl.Name };
                return Respond((DataState.Modified, DataState.Pristine), config, command, guildData);
            }

            return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      PrideRoleConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Pride Role configuration")
              .WithDescription($"The current configuration of PissBot Pride Role on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnablePrideRole).WithIsInline(true),
                new EmbedFieldBuilder().WithName("role").WithValue(config.PrideRole?.Mention ?? "[none set]").WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
