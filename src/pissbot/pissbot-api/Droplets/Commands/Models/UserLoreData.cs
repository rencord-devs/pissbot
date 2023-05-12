using System.Text.Json.Serialization;

namespace Rencord.PissBot.Droplets.Commands
{
    public class UserLoreData : ILore
    {
        public string PersonalLore { get; set; } = string.Empty;
        public string Lore { get; set; } = string.Empty;
        [JsonIgnore]
        public string? Name => null;
    }
}
