namespace Rencord.PissBot.Core
{
    public class DiscordBotOptions
    {
        public const string DiscordBot = nameof(DiscordBot);

        public string? Token { get; set; }
        public ulong ApplicationId { get; set; }
    }
}