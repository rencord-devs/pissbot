using System.Text.Json.Serialization;

namespace Rencord.PissBot.Droplets.Commands
{
    public class ServerLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => null;
        public string Lore { get; set; } = string.Empty;
        public Dictionary<ulong, RoleLoreData> RoleLore { get; set; } = new Dictionary<ulong, RoleLoreData>();
        public Dictionary<ulong, ChannelLoreData> ChannelLore { get; set; } = new Dictionary<ulong, ChannelLoreData>();
    }
}
