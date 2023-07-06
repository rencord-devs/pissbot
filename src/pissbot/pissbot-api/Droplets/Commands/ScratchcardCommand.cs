using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class ScratchcardCommand : ITextCommand
    {
        public string Name => "scratchcard";

        public string Command => "-scratchcard";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Play a scratchcard.")
                   .WithDefaultMemberPermissions(GuildPermission.SendMessages);
            return Task.CompletedTask;
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            EmbedBuilder eb = GetEmbed(command.User.Mention);
            await command.RespondAsync(ephemeral: false, embed: eb.Build(), allowedMentions: AllowedMentions.All);
            return (DataState.Pristine, DataState.Pristine);
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketMessage message, GuildData guildData, UserData userData)
        {
            EmbedBuilder eb = GetEmbed(message.Author.Mention);
            await message.Channel.SendMessageAsync(embed: eb.Build(), allowedMentions: AllowedMentions.All);
            return (DataState.Pristine, DataState.Pristine);
        }

        private static EmbedBuilder GetEmbed(string authorMention)
        {
            var eb = new EmbedBuilder();
            var s = new Scratchcard(emojiCount: 9, winThreshold: 3);
            eb.WithTitle($"Scratcher")
              .WithDescription($"{authorMention}'s scratchcard - {s.GameInstruction()}\r\n\r\n# ||{s.Emoji[0]}|| ||{s.Emoji[1]}|| ||{s.Emoji[2]}||\r\n# ||{s.Emoji[3]}|| ||{s.Emoji[4]}|| ||{s.Emoji[5]}||\r\n# ||{s.Emoji[6]}|| ||{s.Emoji[7]}|| ||{s.Emoji[8]}||\r\n\r\n ||{s.ResultMessage()}||")
              .WithColor(Color.DarkPurple);
            return eb;
        }
    }

    public class Scratchcard
    {
        public Scratchcard(int emojiCount = 9, int winThreshold = 3, Random? rand = default)
        {
            rand ??= new Random();
            Emoji = new List<string>();
            for (var i = 0; i < emojiCount; i++)
            {
                Emoji.Add(scratchcardEmojis[rand.Next(scratchcardEmojis.Length)]);
            }
            Matches = Emoji.GroupBy(x => x).Select(x => x.Count()).OrderByDescending(x => x).First();
            Win = Matches >= winThreshold;
            WinThreshold = winThreshold;
        }

        public static string[] scratchcardEmojis = new string[]
        {
        "<a:Renstares:1057365848400076800>",
        "<a:RenJesus:1036380005132947518>",
        "<:renwha:1061349950723731547>",
        "<:whatyalookin:1103804719983493162>",
        "<:ren_pop:1022269472842727564>",
        "<:ren_lipbite:1010586448589758565>",
        "<a:ren_pat:1020778408520732754>",
        "<:reggplant:1057363715462283355>",
        "<:ren_shock:1010486479568515094>",
        "<:letsgo:1020270999037554719>",
        "<:renvibin:1103783536651153488>"
        };

        public List<string> Emoji { get; }

        public bool Win { get; }

        public int Matches { get; }
        public int WinThreshold { get; }

        public string ResultMessage()
        {
            if (!Win) return $"Better luck next time! You only matched {Matches} symbols";
            return $"Congratulations! You matched {Matches} symbols";
        }

        public string GameInstruction()
        {
            return $"match {WinThreshold} or more symbols to win!";
        }
    }
}
