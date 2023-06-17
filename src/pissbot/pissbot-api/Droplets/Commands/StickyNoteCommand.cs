using Discord;
using Discord.WebSocket;
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

        public async Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new StickyNoteConfiguration());
            var modified = DataState.Pristine;

            if (command.Data?.Options is null) return await Respond((modified, DataState.Pristine), config, command, guildData);

            var addOpt = command.Data.Options.FirstOrDefault(x => x.Name == AddOption);
            if (addOpt is not null)
            {
                var val = new StickyNoteData();

                var channel = addOpt.Options.FirstOrDefault(x => x.Name == ChannelOption);
                if (channel?.Value is not IChannel chan) return await Respond((modified, DataState.Pristine), config, command, guildData);
                var notetext = addOpt.Options.FirstOrDefault(x => x.Name == NoteTextOption);
                if (notetext?.Value is not string noteTextValue) return await Respond((modified, DataState.Pristine), config, command, guildData);

                if (config.Notes.Any(x => x.Channel?.Id == chan.Id)) return await Respond((modified, DataState.Pristine), config, command, guildData, errorMsg: "This channel aready has a sticky note");

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
                if (author?.Value is string v3)
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
                await StickyNote.SendNote(val, (command.Channel as SocketTextChannel)!.Guild.GetChannel(val.Channel.Id) as SocketTextChannel);
                return await Respond((modified, DataState.Pristine), config, command, guildData);
            }

            var remOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveOption);
            if (remOpt is not null)
            {
                var channel = remOpt.Options.FirstOrDefault(x => x.Name == RemoveChannelOption);
                if (channel?.Value is not IChannel chan) return await Respond((modified, DataState.Pristine), config, command, guildData);

                var note = config.Notes.FirstOrDefault(x => x.Channel?.Id == chan.Id);
                if (note is not null) 
                    config.Notes.Remove(note);
                modified = note is not null
                    ? DataState.Modified
                    : DataState.Pristine;
                try
                {
                    if (note?.LastMessageId.HasValue == true)
                        await ((command.Channel as SocketTextChannel)!.Guild.GetChannel(note.Channel!.Id) as SocketTextChannel)!.DeleteMessageAsync(note.LastMessageId.Value);
                }
                catch { }
                return await Respond((modified, DataState.Pristine), config, command, guildData);
            }

            return await Respond((modified, DataState.Pristine), config, command, guildData);
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
}
