﻿using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using System.Collections.Concurrent;
using System.Linq;

namespace Rencord.PissBot.Droplets
{
    public class SentenceGameData
    {
        public ulong SentenceCount { get; set; }
        public List<string> CurrentSentence { get; set; } = new List<string>();
        public List<string> SentenceAuthors { get; set; } = new List<string>();
        public ulong PreviousAuthor { get; set; }
    }
    public class PissBotLookingForPiss : IPissDroplet
    {
        private readonly List<GuildOptions> options;
        private CancellationToken stopToken;

        public PissBotLookingForPiss(IOptions<List<GuildOptions>> options)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
        }

        public Task Start(IDiscordClient client, CancellationToken stopToken)
        {
            if (client is not DiscordSocketClient sc) throw new ArgumentException("client must be a socket client as the game uses events", nameof(client));
            this.stopToken = stopToken;
            sc.MessageReceived += MessageReceived;
            return Task.CompletedTask;
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (stopToken.IsCancellationRequested) return;
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = options.FirstOrDefault(x => x.Id == stc.Guild.Id);
            if (guild == null || !guild.EnableLookingForPiss) return;

            if (arg.Content is not null && arg.Content.ToLower().Contains("piss"))
            {
                try
                {
                    await arg.AddReactionAsync(Emote.Parse("<:notp:1000806527965347922>"));
                }
                catch
                {
                    await arg.AddReactionAsync(Emote.Parse("<:notp:1104541579521306684>"));
                }
            }
        }
    }
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
        private readonly IEnumerable<GuildOptions> options;
        private readonly IGuildDataPersistence guildDataStore;
        private DiscordSocketClient socketClient;
        private Random rand = new Random();
        private ConcurrentDictionary<ulong, SocketTextChannel> channelCache = new ConcurrentDictionary<ulong, SocketTextChannel>();
        private Dictionary<ulong, ConcurrentQueue<SocketMessage>> messageQueues = new Dictionary<ulong, ConcurrentQueue<SocketMessage>>();

        private string RandomNegativeResponse() =>
            negativeResponses[rand.Next(0, negativeResponses.Length)];

        public SentenceGame(IOptions<List<GuildOptions>> options, IGuildDataPersistence guildDataStore)
        {
            this.options = options?.Value ?? throw new ArgumentNullException(nameof(options));
            this.guildDataStore = guildDataStore;
        }

        public async Task Start(IDiscordClient client, CancellationToken stopToken)
        {
            if (client is not DiscordSocketClient sc) throw new ArgumentException("client must be a socket client as the game uses events", nameof(client));
            socketClient = sc;
            foreach  (var guild in options) // start a processing queue consumer for each server
            {
                messageQueues.Add(guild.Id, new ConcurrentQueue<SocketMessage>());
                _ = Task.Run(() => ProcessQueue(messageQueues[guild.Id], stopToken));
            }
            socketClient.MessageReceived += MessageReceived;
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

                    }
                }
                else
                {
                    await Task.Delay(5);
                }
            }
        }

        private async Task MessageReceived(SocketMessage arg)
        {
            if (arg.Author.IsBot) return;
            if (arg.Channel is not SocketTextChannel stc) return;

            var guild = options.FirstOrDefault(x => x.Id == stc.Guild.Id);
            if (guild == null) return;
            if (arg.Channel.Id != guild.SentenceGame?.GameChannel) return;

            // the message is a valid game message, add it to processing queue
            var q = messageQueues[guild.Id];
            q.Enqueue(arg);
        }

        private async Task HandleGameMessage(SocketMessage arg)
        {
            if (arg.Channel is not SocketTextChannel stc) return;
            var guild = options.FirstOrDefault(x => x.Id == stc.Guild.Id);
            if (guild is null) return;

            var gameChannel = channelCache.GetOrAdd(arg.Channel.Id, x => socketClient.GetChannel(arg.Channel.Id) is SocketTextChannel stc
                                                                            ? stc
                                                                            : throw new NotSupportedException("not a text channel"));
            
            var data = await guildDataStore.GetGuild(stc.Guild.Id);
            var gameData = data.GetOrAddData(() => new SentenceGameData());
            if (gameData.PreviousAuthor == arg.Author.Id)
            {
                await ReactNo(arg);
                await gameChannel.SendMessageAsync(messageReference: arg.Reference, text: RandomNegativeResponse());
                return;
            }

            // if there's only this entry, don't allow it to terminate the sentence
            var allowTermination = gameData.CurrentSentence.Count > 0;
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
                var resultChanId = guild.SentenceGame?.ResultsChannel.HasValue == true
                                    ? guild.SentenceGame.ResultsChannel.Value
                                    : gameChannel.Id;
                var resultChannel = channelCache.GetOrAdd(resultChanId,
                                                          x => socketClient.GetChannel(resultChanId) as SocketTextChannel ?? throw new NotSupportedException("Not a text channel"));

                var sentence = result.ToLower().Contains("piss") ? "The piss goblins have completed a new sentence!" : $"{guild.Name} has completed a new sentence!";
                string message = $"**{sentence}**\r\n\r\n> {result}\r\n\r\nWritten by {string.Join(" ", gameData.SentenceAuthors)} (#{gameData.SentenceCount})";
                if (resultChanId != gameChannel.Id)
                {
                    // send to the game channel always, to break up the sentences in the channel.
                    await gameChannel.SendMessageAsync(message);
                }
                await resultChannel.SendMessageAsync(message);
                gameData.SentenceAuthors.Clear();
                gameData.CurrentSentence.Clear();
                await ReactCelebrate(arg);
            }
            await guildDataStore.SaveGuild(stc.Guild.Id);
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