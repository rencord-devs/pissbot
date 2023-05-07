namespace Rencord.PissBot.Core
{
    public interface IGuildDataPersistence
    {
        Task<GuildData> GetGuild(ulong id);
        Task SaveGuild(ulong id);
    }
}