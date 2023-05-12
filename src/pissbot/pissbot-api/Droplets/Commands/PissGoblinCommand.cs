using System.Text;
using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class PissGoblinCommand : ICommand
    {
        public string Name => "piss-goblin";
        public const string LeagueOption = "league";
        public const string PublicOption = "showinchannel";

        public PissGoblinCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Commands for piss goblins") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                   .AddOption(LeagueOption, 
                              ApplicationCommandOptionType.SubCommand, 
                              "Show the piss goblin piss league table",
                              options: new List<SlashCommandOptionBuilder>
                              {
                                new SlashCommandOptionBuilder()
                                    .WithName(PublicOption)
                                    .WithDescription("Show the output to all members in the channel")
                                    .WithType(ApplicationCommandOptionType.Boolean)
                                    .WithRequired(false)
                              });
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.First(x => x.Name == LeagueOption);
            if (opt is null) return Task.FromResult((DataState.Pristine, DataState.Pristine));
            var config = guildData.GetOrAddData(() => new LookingForPissConfiguration());
            return Respond((DataState.Pristine, DataState.Pristine), command, config, !opt.Options.Any(x => x.Name == PublicOption && x.Value is bool v && v));
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      SocketSlashCommand command, 
                                                                      LookingForPissConfiguration config,
                                                                      bool ephemeral)
        {
            var rank = new StringBuilder();
            var count = new StringBuilder();
            var mention = new StringBuilder();
            var i = 0;
            foreach (var pg in config.PissLeague)
            {
                i++;
                rank.AppendLine(i.ToString());
                count.AppendLine(pg.PissCount.ToString());
                mention.AppendLine(pg.Mention);

            }
            var eb = new EmbedBuilder();
            eb.WithTitle("Piss Goblin League Table")
              .WithDescription($"The top ranked piss-saying Piss-Goblins")
              .WithFields(
                new EmbedFieldBuilder().WithName("rank").WithValue(rank.ToString()).WithIsInline(true),
                new EmbedFieldBuilder().WithName("piss count").WithValue(count.ToString()).WithIsInline(true),
                new EmbedFieldBuilder().WithName("member").WithValue(mention.ToString()).WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: ephemeral, embed: eb.Build());
            return result;
        }
    }
}
