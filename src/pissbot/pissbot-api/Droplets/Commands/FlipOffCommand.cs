using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public class FlipOffCommand : ITextCommand
    {
        public string Name => "flipoff";

        public string Command => "-flipoff";

        public const string TargetOption = "target";

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Flip off a specified member")
                   .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                   .AddOption(TargetOption, ApplicationCommandOptionType.User, "the user to flip off", isRequired: true);
            return Task.CompletedTask;
        }

        public async Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            if (command?.Data?.Options?.FirstOrDefault(x => x.Name == TargetOption)?.Value is IUser user)
            {
                var eb = new EmbedBuilder();
                eb.WithTitle($"Flip off!")
                  .WithDescription($"{command.User.Mention} flipped off {user.Mention}!\r\n\r\n> **{RandomFlipOff()}**")
                  .WithColor(Color.DarkPurple);
                await command.RespondAsync(ephemeral: false, embed: eb.Build(), allowedMentions: AllowedMentions.All);
            }
            return (DataState.Pristine, DataState.Pristine);
        }

        private Random rnd = new Random();
        private static string[] flipoffs = new string[]
        {
            @"╭∩╮(･◡･)╭∩╮",
            @"( ° ͜ʖ͡°)╭∩╮",
            @"凸( •̀_•́ )凸",
            @"┌∩┐(◣_◢)┌∩┐",
            @"凸(ಠ_ಠ)凸",
            @"🖕"
        };

        private string RandomFlipOff() =>
            flipoffs[rnd.Next(0, flipoffs.Length)];

        public async Task<(DataState Guild, DataState User)> Handle(SocketMessage message, GuildData guildData, UserData userData)
        {
            
            if (message.MentionedUsers?.FirstOrDefault() is IUser user)
            {
                var eb = new EmbedBuilder();
                eb.WithTitle($"Flip off!")
                  .WithDescription($"{message.Author.Mention} flipped off {user.Mention}!\r\n\r\n> **{RandomFlipOff()}**")
                  .WithColor(Color.DarkPurple);
                await message.Channel.SendMessageAsync(embed: eb.Build(), allowedMentions: AllowedMentions.All);
            }
            return (DataState.Pristine, DataState.Pristine);
        }
    }
}
