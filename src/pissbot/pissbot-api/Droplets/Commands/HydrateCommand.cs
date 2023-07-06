using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class HydrateCommand : ITextCommand
    {
        public string Name => "hydrate";

        public string Command => "-hydrate";

        public const string TargetOption = "target";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Give a drink to a specified member.")
                   .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                   .AddOption(TargetOption, ApplicationCommandOptionType.User, "the user to hydrate", isRequired: true);
            return Task.CompletedTask;
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            if (command?.Data?.Options?.FirstOrDefault(x => x.Name == TargetOption)?.Value is IUser user)
            {
                var eb = GetEmbed(command.User.Mention, user.Mention);
                await command.RespondAsync(ephemeral: false, embed: eb.Build(), allowedMentions: AllowedMentions.All);
            }
            return (DataState.Pristine, DataState.Pristine);
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketMessage message, GuildData guildData, UserData userData)
        {

            if (message.MentionedUsers?.FirstOrDefault() is IUser user)
            {
                var eb = GetEmbed(message.Author.Mention, user.Mention);
                await message.Channel.SendMessageAsync(embed: eb.Build(), allowedMentions: AllowedMentions.All);
            }
            return (DataState.Pristine, DataState.Pristine);
        }

        private EmbedBuilder GetEmbed(string authorMention, string targetMention)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle($"Mmmm tasty beverage!")
              .WithDescription($"{authorMention} has given a drink to {targetMention}!")
              .WithImageUrl("https://media.discordapp.net/attachments/996526781127467079/1120824093944598579/347400011_3448403935434080_5808416391837202921_n.jpg?width=604&height=604")
              .WithColor(Color.DarkPurple);
            return eb;
        }
    }
}
