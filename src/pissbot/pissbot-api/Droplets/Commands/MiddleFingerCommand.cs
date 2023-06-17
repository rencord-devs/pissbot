using Discord;
using Discord.WebSocket;
using Microsoft.Azure.Cosmos.Serialization.HybridRow.Schemas;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class StickyNoteCommand : ICommand
    {
        public string Name => "stickynote";
        public const string TimeOption = "time";
        public const string AddOption = "add";
        public const string RemoveOption = "remove";
        public const string RemoveChannelOption = "channeltoremove";

        public const string ChannelOption = "channel";
        public const string NoteTitleOption = "title";
        public const string NoteTextOption = "notetext";
        public const string NoteFooterOption = "notefooter";
        public const string NoteAuthorNameOption = "noteauthor";
        public const string NoteThumbnailUrlOption = "notethumbnail";
        public const string NoteImageUrlOption = "noteimage";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Add a sticky note to a channel.")
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(AddOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("add a sticky note")
                        .WithRequired(false)
                        .AddOption(ChannelOption, ApplicationCommandOptionType.Channel, "the channel", isRequired: true)
                        .AddOption(NoteTitleOption, ApplicationCommandOptionType.String, "the message title", isRequired: true)
                        .AddOption(NoteTextOption, ApplicationCommandOptionType.String, "the message text", isRequired: true)
                        .AddOption(NoteFooterOption, ApplicationCommandOptionType.String, "the message footer", isRequired: false)
                        .AddOption(NoteAuthorNameOption, ApplicationCommandOptionType.String, "the message author name", isRequired: false)
                        .AddOption(NoteThumbnailUrlOption, ApplicationCommandOptionType.String, "the thumbnail url", isRequired: false)
                        .AddOption(NoteImageUrlOption, ApplicationCommandOptionType.String, "the image url", isRequired: false)
                   )
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(RemoveOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("remove a sticky note")
                        .WithRequired(false)
                        .AddOption(RemoveChannelOption, ApplicationCommandOptionType.Channel, "the channel", isRequired: true)
                   );
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new StickyNoteConfiguration());
            var modified = DataState.Pristine;

            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);

            var addOpt = command.Data.Options.FirstOrDefault(x => x.Name == AddOption);
            if (addOpt is not null)
            {
                var val = new StickyNoteData();

                var channel = addOpt.Options.FirstOrDefault(x => x.Name == ChannelOption);
                if (channel?.Value is not IChannel chan) return Respond((modified, DataState.Pristine), config, command, guildData);
                var notetext = addOpt.Options.FirstOrDefault(x => x.Name == NoteTextOption);
                if (notetext?.Value is not string noteTextValue) return Respond((modified, DataState.Pristine), config, command, guildData);

                if (config.Notes.Any(x => x.Channel?.Id == chan.Id)) return Respond((modified, DataState.Pristine), config, command, guildData, errorMsg: "This channel aready has a sticky note");

                modified = DataState.Modified;
                val.Channel = new ChannelSummary
                {
                    Id = chan.Id,
                    Name = chan.Name
                };
                val.NoteText = noteTextValue;

                var title = addOpt.Options.FirstOrDefault(x => x.Name == NoteTitleOption);
                if (title?.Value is string v1)
                {
                    val.NoteTitle = v1;
                }

                var footer = addOpt.Options.FirstOrDefault(x => x.Name == NoteFooterOption);
                if (footer?.Value is string v2)
                {
                    val.NoteFooter = v2;
                }

                var author = addOpt.Options.FirstOrDefault(x => x.Name == NoteAuthorNameOption);
                if (footer?.Value is string v3)
                {
                    val.NoteAuthorName = v3;
                }

                var thumbnail = addOpt.Options.FirstOrDefault(x => x.Name == NoteThumbnailUrlOption);
                if (thumbnail?.Value is string v4)
                {
                    val.NoteThumbnailUrl = v4;
                }

                var image = addOpt.Options.FirstOrDefault(x => x.Name == NoteImageUrlOption);
                if (image?.Value is string v5)
                {
                    val.NoteImageUrl = v5;
                }

                config.Notes.Add(val);
                return Respond((modified, DataState.Pristine), config, command, guildData);
            }

            var remOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveOption);
            if (remOpt is not null)
            {
                var channel = remOpt.Options.FirstOrDefault(x => x.Name == RemoveChannelOption);
                if (channel?.Value is not IChannel chan) return Respond((modified, DataState.Pristine), config, command, guildData);
                
                modified = config.Notes.RemoveAll(x => x.Channel?.Id == chan.Id) > 0
                    ? DataState.Modified
                    : DataState.Pristine;
                return Respond((modified, DataState.Pristine), config, command, guildData);
            }

            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      StickyNoteConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData,
                                                                      string? errorMsg = null)
        {
            var eb = new EmbedBuilder();
            if (errorMsg is not null)
            {
                eb.WithTitle("Sticky Notes Error")
                  .WithDescription($"Error in Sticky Notes on {guildData.Name}")
                  .WithFields(new EmbedFieldBuilder().WithName("Error").WithValue(errorMsg).WithIsInline(false))
                  .WithColor(Color.Red);
            }
            else
            {
                eb.WithTitle("Sticky Notes configuration")
                  .WithDescription($"The current configuration of Sticky Notes on {guildData.Name}")
                  .WithFields(
                    config.Notes.Select(x => new EmbedFieldBuilder().WithName(x.Channel?.Name ?? "[unknown channel]").WithValue(x.NoteText).WithIsInline(false)).ToArray())
                  .WithColor(Color.Gold);
            }
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }

    public class MiddleFingerCommand : ICommand
    {
        public string Name => "middlefinger";
        public const string EnableOption = "enable";
        public const string TimeOption = "time";
        public const string AddOption = "adduser";
        public const string RemoveOption = "removeuser";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Add :middle_finger: reacts to posts by specified users, removing the react after N seconds.")
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the middle finger reaccs", isRequired: false)
                   .AddOption(TimeOption, ApplicationCommandOptionType.Integer, "the time in seconds to leave the reaction up - set to 0 leave it forever", isRequired: false)
                   .AddOption(AddOption, ApplicationCommandOptionType.User, "add a user to the list", isRequired: false)
                   .AddOption(RemoveOption, ApplicationCommandOptionType.User, "remove a user from the list", isRequired: false)
                   .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from reactions", isRequired: false)
                   .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false);
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
                                                                      MiddleFingerConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            var val = string.Join(", ", config.ExcludedChannels.Where(x => x.Name is not null).Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(val))
                val = "[no exlcudes]";
            eb.WithTitle("Middle Finger configuration")
              .WithDescription($"The current configuration of middle finger on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableMiddleFinger).WithIsInline(true),
                new EmbedFieldBuilder().WithName("time in seconds to show reaction (0 = don't remove)").WithValue(config.Time).WithIsInline(true),
                new EmbedFieldBuilder().WithName("users to react to").WithValue(MentionString(config)).WithIsInline(false),
                new EmbedFieldBuilder().WithName("excluded").WithValue(val).WithIsInline(false))
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
