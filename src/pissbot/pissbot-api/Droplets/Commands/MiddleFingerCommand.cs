using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class MiddleFingerCommand : ICommand
    {
        public string Name => "middlefinger";
        public const string EnableOption = "enable";
        public const string TimeOption = "time";
        public const string AddOption = "adduser";
        public const string RemoveOption = "removeuser";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Add :middle_finger: reacts to posts by specified users, removing the react after N seconds")
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the middle finger reaccs", isRequired: false)
                   .AddOption(TimeOption, ApplicationCommandOptionType.Integer, "the time in seconds to leave the reaction up - set to 0 leave it forever", isRequired: false)
                   .AddOption(AddOption, ApplicationCommandOptionType.User, "add a user to the list", isRequired: false)
                   .AddOption(RemoveOption, ApplicationCommandOptionType.User, "remove a user from the list", isRequired: false);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new MiddleFingerConfiguration());
            var modified = DataState.Pristine;

            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);

            var enableOpt = command.Data.Options.FirstOrDefault(x => x.Name == EnableOption);
            if (enableOpt?.Value is bool value)
            {
                if (config.EnableMiddleFinger != value)
                    modified = DataState.Modified;
                config.EnableMiddleFinger = value;
            }

            var timeOpt = command.Data.Options.FirstOrDefault(x => x.Name == TimeOption);
            if (timeOpt?.Value is long time)
            {
                if (config.Time != time)
                    modified = DataState.Modified;
                config.Time = time;
            }

            var addOpt = command.Data.Options.FirstOrDefault(x => x.Name == AddOption);
            if (addOpt?.Value is IUser user)
            {
                if (!config.Users.Any(c => c.Id == user.Id))
                {
                    modified = DataState.Modified;
                    config.Users.Add(new MiddleFingerUser(user.Mention, user.Id));
                }
            }

            var removeOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveOption);
            if (removeOpt?.Value is IUser user2)
            {
                if (config.Users.Any(c => c.Id == user2.Id))
                {
                    modified = DataState.Modified;
                    config.Users.RemoveAll(c => c.Id == user2.Id);
                }
            }

            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      MiddleFingerConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Middle Finger configuration")
              .WithDescription($"The current configuration of middle finger on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableMiddleFinger).WithIsInline(true),
                new EmbedFieldBuilder().WithName("time in seconds to show reaction (0 = don't remove)").WithValue(config.Time).WithIsInline(true),
                new EmbedFieldBuilder().WithName("users to react to").WithValue(MentionString(config)).WithIsInline(false))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }

        private static string MentionString(MiddleFingerConfiguration config)
        {
            var s = string.Join(", ", config.Users.Select(x => x.Mention));
            return string.IsNullOrWhiteSpace(s) ? "No users added" : s;
        }
    }
}
