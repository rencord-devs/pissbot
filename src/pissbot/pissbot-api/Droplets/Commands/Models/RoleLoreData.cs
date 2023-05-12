using System.Text.Json.Serialization;

namespace Rencord.PissBot.Droplets.Commands
{
    public class RoleLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => RoleName;
        public string RoleName { get; set; } = string.Empty;
        public ulong RoleId { get; set; }
        public string Lore { get; set; } = string.Empty;
    }
}
