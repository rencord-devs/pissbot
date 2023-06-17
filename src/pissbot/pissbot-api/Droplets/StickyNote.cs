using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;
using System.Diagnostics;

namespace Rencord.PissBot.Droplets
{
    public class StickyNote : IPissDroplet
    {
        private readonly IGuildDataPersistence guildDataStore;
        private CancellationToken stopToken;
        private DiscordSocketClient? client;
        private HashSet<StickyNoteData> queue;
        private object queueLock = new object();

        public StickyNote(IGuildDataPersistence guildDataStore)
        {
            this.guildDataStore = guildDataStore;
            this.queue = new HashSet<StickyNoteData>();
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.stopToken = stopToken;
            this.client = client;
            stopToken.Register(Stop);
            client.Ready += Ready;

            _ = Task.Run(() => ProcessQueue(stopToken));

            return Task.CompletedTask;
        }

        private async Task ProcessQueue(CancellationToken cancellationToken)
        {
            var delay = TimeSpan.Zero;
            var t1 = Stopwatch.StartNew();
            while (!cancellationToken.IsCancellationRequested)
            {
                if (delay > TimeSpan.Zero)
                    await Task.Delay(delay);
                t1.Restart();
                HashSet<StickyNoteData> toProcess;
                lock (queueLock)
                {
                    if (!queue.Any())
                    {
                        delay = TimeSpan.FromSeconds(2) - t1.Elapsed;
                        continue;
                    }
                    toProcess = queue;
                    queue = new HashSet<StickyNoteData>();
                }
                foreach (var note in toProcess)
                {
                    var chan = client!.GetChannel(note.Channel!.Id) as SocketTextChannel;
                    if (note.LastMessageId.HasValue)
                    {
                        var lastMsg = await chan!.GetMessagesAsync(1).FirstOrDefaultAsync();
                        if (lastMsg?.FirstOrDefault()?.Id == note.LastMessageId.Value)
                            continue; // ignore if our message is already the newest
                    }
                    if (note.LastMessageId.HasValue)
                        await chan!.DeleteMessageAsync(note.LastMessageId.Value);
                    await SendNote(note, chan);
                    await guildDataStore.SaveData(chan.Guild.Id);
                }
                delay = TimeSpan.FromSeconds(2) - t1.Elapsed;
            }
        }

        public static async Task SendNote(StickyNoteData note, SocketTextChannel? chan)
        {
            var embed = new EmbedBuilder();
            embed.Title = note.NoteTitle;
            embed.Description = note.NoteText;
            embed.Author = note.NoteAuthorName is not null ? new EmbedAuthorBuilder().WithName(note.NoteAuthorName) : null;
            embed.Footer = note.NoteFooter is not null ? new EmbedFooterBuilder().WithText(note.NoteFooter) : null;
            embed.ImageUrl = note.NoteImageUrl;
            embed.ThumbnailUrl = note.NoteThumbnailUrl;
            embed.Color = Color.Gold;

            var msg = await chan!.SendMessageAsync(
                embed: embed.Build());
            note.LastMessageId = msg.Id;
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
                lock (queueLock)
                {
                    queue.Add(note);
                }
            }
        }
    }
}
