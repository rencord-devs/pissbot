using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;
using System.Collections.Concurrent;
using System.Linq;

namespace Rencord.PissBot.Droplets
{
    public class SentenceGame : IPissDroplet
    {
        private static char[] terminators = new char[] { '.', '!', '?' };
        private static char[] spacers = new char[] { '_', '+', '&', '\r', '\n' };
        private static string[] negativeResponses = new string[]
        {
            "<a:Renstares:1057365848400076800>",
            "<:renwha:1061349950723731547>",
            "<:no:1057069758828269598>"
        };
        private readonly IGuildDataPersistence guildDataStore;
        private readonly ILogger<SentenceGame> logger;
        private DiscordSocketClient? client;
        private CancellationToken stopToken;
        private Random rand = new Random();
        private ConcurrentDictionary<ulong, SocketTextChannel> channelCache = new ConcurrentDictionary<ulong, SocketTextChannel>();
        private Dictionary<ulong, ConcurrentQueue<SocketMessage>> messageQueues = new Dictionary<ulong, ConcurrentQueue<SocketMessage>>();

        private string RandomNegativeResponse() =>
            negativeResponses[rand.Next(0, negativeResponses.Length)];

        public SentenceGame(IGuildDataPersistence guildDataStore, ILogger<SentenceGame> logger)
        {
            this.guildDataStore = guildDataStore;
            this.logger = logger;
        }

        public Task Start(DiscordSocketClient client, CancellationToken stopToken)
        {
            this.client = client;
            this.stopToken = stopToken;
            stopToken.Register(Stop);
            this.client.Ready += Ready;
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
            if (client is not null)
            {
                foreach (var guild in client.Guilds) // start a processing queue consumer for each server
                {
                    messageQueues.Add(guild.Id, new ConcurrentQueue<SocketMessage>());
                    _ = Task.Run(() => ProcessQueue(messageQueues[guild.Id], stopToken));
                }
                this.client.MessageReceived += MessageReceived;
            }
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;

            var guildData = await guildDataStore.GetData(stc.Guild.Id);
            var gameConfig = guildData.GetOrAddData(() => new SentenceGameConfiguration());
            if (!gameConfig.EnableSentenceGame) return;
            if (arg.Channel.Id != gameConfig.GameChannel) return;

            // the message is a valid game message, add it to processing queue
            var q = messageQueues[guildData.Id];
            q.Enqueue(arg);
        }

        private async Task ProcessQueue(ConcurrentQueue<SocketMessage> queue, CancellationToken cancellationToken)
        {
            while (!cancellationToken.IsCancellationRequested)
            {
                if (queue.TryDequeue(out var msg))
                {
                    try
                    {
                        await HandleGameMessage(msg);
                    }
                    catch (Exception ex)
                    {
                        logger.LogError(ex, "Game error in sentence game");
                    }
                }
                else
                {
                    await Task.Delay(5);
                }
            }
        }

        private async Task HandleGameMessage(SocketMessage arg)
        {
            if (arg.Channel is not SocketTextChannel stc) return;
            var gameChannel = channelCache.GetOrAdd(arg.Channel.Id, x => client?.GetChannel(arg.Channel.Id) is SocketTextChannel stc
                                                                            ? stc
                                                                            : throw new NotSupportedException("not a text channel"));
            
            var guildData = await guildDataStore.GetData(stc.Guild.Id);
            var gameData = guildData.GetOrAddData(() => new SentenceGameData());
            var gameConfig = guildData.GetOrAddData(() => new SentenceGameConfiguration());
            if (gameData.PreviousAuthor == arg.Author.Id)
            {
                await ReactNo(arg);
                await gameChannel.SendMessageAsync(messageReference: new MessageReference(arg.Id), text: RandomNegativeResponse());
                return;
            }

            // Don't allow single-word sentences.
            var allowTermination = gameData.CurrentSentence.Count > 1 || // 2 or more existing words
                                     (gameData.CurrentSentence.Count == 1 && // or 1 existing word and this entry contains more than just terminators (i.e. it has a 2nd word)
                                      arg.Content.Trim().Any(c => !terminators.Contains(c)));

            if (!await CheckValidWord(arg, gameChannel, allowTermination)) return;

            gameData.PreviousAuthor = arg.Author.Id;
            var item = arg.Content.Trim();
            gameData.CurrentSentence.Add(item);
            if (!gameData.SentenceAuthors.Contains(arg.Author.Mention))
                gameData.SentenceAuthors.Add(arg.Author.Mention);

            if (terminators.Contains(item[item.Length - 1]))
            {
                // end of sentence
                gameData.SentenceCount++;
                var result = string.Join(" ", gameData.CurrentSentence).Replace("  ", " ").Replace(" ?", "?").Replace(" .", ".").Replace(" !", "!");
                var resultChanId = gameConfig.ResultsChannel.HasValue == true
                                    ? gameConfig.ResultsChannel.Value
                                    : gameChannel.Id;
                var resultChannel = channelCache.GetOrAdd(resultChanId,
                                                          x => client?.GetChannel(resultChanId) as SocketTextChannel ?? throw new NotSupportedException("Not a text channel"));

                var sentence = result.ToLower().Contains("piss") ? "The piss goblins have completed a new sentence!" : $"{guildData.Name} has completed a new sentence!";
                string message = $"**{sentence}**\r\n\r\n> {result}\r\n\r\nWritten by {string.Join(" ", gameData.SentenceAuthors)} (#{gameData.SentenceCount})";
                if (resultChanId != gameChannel.Id)
                {
                    // send to the game channel always, to break up the sentences in the channel.
                    await gameChannel.SendMessageAsync(message);
                }
                await resultChannel.SendMessageAsync(message);
                gameData.SentenceAuthors.Clear();
                gameData.CurrentSentence.Clear();
                gameData.PreviousAuthor = 0;
                await ReactCelebrate(arg);
            }
            await guildDataStore.SaveData(stc.Guild.Id);
        }

        private async Task<bool> CheckValidWord(SocketMessage arg, SocketTextChannel gameChannel, bool allowTermination)
        {
            var item = arg.Content.Trim();

            // special cases
            // - null or white
            if (string.IsNullOrWhiteSpace(item))
            {
                await ReactNo(arg);
                return false;
            }
            // - all terminators
            if (allowTermination && item.All(x => terminators.Contains(x)))
            {
                await ReactYes(arg);
                return true;
            }
            // - more than 1 hyphen
            if (item.Count(x => x == '-') > 1)
            {
                await ReactNo(arg);
                return false;
            }
            // - pascal casing (allow either all-caps or up to 2 caps)
            if (item.Count(x => char.IsUpper(x)) > 2 && !item.All(x => char.IsUpper(x)))
            {
                await ReactNo(arg);
                return false;
            }
            // - decimal number
            if (decimal.TryParse(item, out _) && (allowTermination || !terminators.Contains(item.Last())))
            {
                await ReactYes(arg);
                return true;
            }
            // - decimal number with currency sign
            if ((char.GetUnicodeCategory(item.First()) == System.Globalization.UnicodeCategory.CurrencySymbol ||
                char.GetUnicodeCategory(item.Last()) == System.Globalization.UnicodeCategory.CurrencySymbol) &&
                (allowTermination || !terminators.Contains(item.Last())))
            {
                if (item.All(c => char.IsDigit(c) || c == '.' || c == ',' || char.GetUnicodeCategory(c) == System.Globalization.UnicodeCategory.CurrencySymbol))
                {
                    await ReactYes(arg);
                    return true;
                }
            }

            // check for whitespace
            for (var i = 0; i < item.Length; i++)
            {
                // checks a lot of whitespace characters, not just regular ascii space
                if (char.IsWhiteSpace(item, i)  || spacers.Contains(item[i]))
                {
                    await ReactNo(arg);
                    await gameChannel.SendMessageAsync(messageReference: arg.Reference, text: RandomNegativeResponse());
                    return false;
                }
            }

            // check for terminators
            for (var i = 0; i < item.Length; i++)
            {
                if (char.IsPunctuation(item, i))
                {
                    if (terminators.Contains(item[i]))
                    {
                        if (i < item.Length - 1)
                        {
                            // Not at the end, check that all remaining characters are also terminators
                            for (var x = i; x < item.Length; x++)
                            {
                                if (!terminators.Contains(item[x]))
                                {
                                    await ReactNo(arg);
                                    await gameChannel.SendMessageAsync(messageReference: arg.Reference, text: RandomNegativeResponse());
                                    return false;
                                }
                            }

                            if (allowTermination)
                            {
                                // All characters are terminators, short-circuit and return true
                                await ReactYes(arg);
                                return true;
                            }
                        }
                    }
                }
            }

            if (allowTermination || !terminators.Contains(item.Last()))
            {
                // word is valid, return true
                await ReactYes(arg);
                return true;
            }

            await ReactNo(arg);
            return false;
        }

        private static async Task ReactNo(SocketMessage arg)
        {
            try
            {
                await arg.AddReactionAsync(Emote.Parse("<:no:1057069758828269598>"));
            }
            catch
            {
                // custom emoji not available, use a noddy one
                await arg.AddReactionAsync(new Emoji("❌"));
            }
        }

        private static async Task ReactYes(SocketMessage arg)
        {
            try
            {
                await arg.AddReactionAsync(Emote.Parse("<:yes:1057069691400618055>"));
            }
            catch
            {
                // custom emoji not available, use a noddy one
                await arg.AddReactionAsync(new Emoji("✅"));
            }
        }

        private static async Task ReactCelebrate(SocketMessage arg)
        {
            try
            {
                await arg.AddReactionAsync(Emote.Parse("<a:ren_pat:1020778408520732754>"));
            }
            catch
            {
                // custom emoji not available, use a noddy one
                await arg.AddReactionAsync(new Emoji("🎉"));
            }
        }
    }
}
