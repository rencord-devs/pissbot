using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;
using System.Runtime.CompilerServices;

namespace Rencord.PissBot.Droplets
{
    public class StickyNote : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public StickyNote(IGuildDataPersistence guildDataStore)
        {
            this.guildDataStore = guildDataStore;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            stopToken.Register(Stop);
            client.Ready += Ready;
            return Task.CompletedTask;
        }

        private void Stop()
        {
            if (this.client is null) return;
            this.client.Ready -= Ready;
            this.client.MessageReceived -= MessageReceived;
        }

        private Task Ready()
        {
            if (client is not null) client.MessageReceived += MessageReceived;
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = await guildDataStore.GetData(stc.Guild.Id);
            var config = guild.GetOrAddData(() => new StickyNoteConfiguration());
            var note = config.Notes.FirstOrDefault(c => c.Channel?.Id == arg.Channel.Id);
            if (note is not null)
            {
                lock (note)
                {
                    if (note.Waiting) return; // another thread is already waiting to make the next post
                    if (note.LastPosted.HasValue && DateTimeOffset.UtcNow - note.LastPosted.Value < TimeSpan.FromSeconds(5))
                    {
                        note.Waiting = true;
                    }
                    else
                    {
                        note.LastPosted = DateTimeOffset.UtcNow; // we're about to post, make any other posts wait
                    }
                }

                if (note.Waiting)
                {
                    await Task.Delay(TimeSpan.FromSeconds(5) - (DateTimeOffset.UtcNow - note.LastPosted!.Value));
                }

                if (note.LastMessageId.HasValue)
                    await arg.Channel.DeleteMessageAsync(note.LastMessageId.Value);

                var embed = new EmbedBuilder();
                embed.Title = note.NoteTitle;
                embed.Description = note.NoteText;
                embed.Author = note.NoteAuthorName is not null ? new EmbedAuthorBuilder().WithName(note.NoteAuthorName) : null;
                embed.Footer = note.NoteFooter is not null ? new EmbedFooterBuilder().WithText(note.NoteFooter) : null;
                embed.ImageUrl = note.NoteImageUrl;
                embed.ThumbnailUrl = note.NoteThumbnailUrl;

                var msg = await arg.Channel.SendMessageAsync(
                    embed: embed.Build());

                lock (note)
                {
                    note.LastPosted = DateTimeOffset.UtcNow;
                    note.LastMessageId = msg.Id;
                    note.Waiting = false;
                }
            }
        }
    }

    public class MiddleFinger : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;

        public MiddleFinger(IGuildDataPersistence guildDataStore)
        {
            this.guildDataStore = guildDataStore;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            stopToken.Register(Stop);
            client.Ready += Ready;
            return Task.CompletedTask;
        }

        private void Stop()
        {
            if (this.client is null) return;
            this.client.Ready -= Ready;
            this.client.MessageReceived -= MessageReceived;
        }

        private Task Ready()
        {
            if (client is not null) client.MessageReceived += MessageReceived;
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = await guildDataStore.GetData(stc.Guild.Id);
            var config = guild.GetOrAddData(() => new MiddleFingerConfiguration());
            if (!config.EnableMiddleFinger) return;
            if (config.ExcludedChannels.Any(x => x.Id == stc.Id)) return;

            if (config.Users.Any(c => c.Id == arg.Author.Id))
            {
                await arg.AddReactionAsync(Emoji.Parse("🖕"));
                if (config.Time > 0)
                {
                    var botId = client!.CurrentUser.Id;
                    _ = Task.Run(async () =>
                    {
                        await Task.Delay((int)config.Time * 1000);
                        await arg.RemoveReactionAsync(Emoji.Parse("🖕"), botId);
                    });
                }
            }
        }
    }
}
