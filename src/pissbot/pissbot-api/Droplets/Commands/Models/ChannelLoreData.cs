using System.Text.Json.Serialization;

namespace Rencord.PissBot.Droplets.Commands
{
    public class ChannelLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => ChannelName;
        public string ChannelName { get; set; } = string.Empty;
        public ulong ChannelId { get; set; }
        public string Lore { get; set; } = string.Empty;
    }
}
