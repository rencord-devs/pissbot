using Rencord.PissBot.Core;

namespace Rencord.PissBot.Persistence
{
    public interface IGuildDataPersistence : IDataPersistence<GuildData>
    {
    }

    public interface IUserDataPersistence : IDataPersistence<UserData>
    {
    }

    public interface IDataPersistence<T> where T : IId
    {
        Task<T> GetData(ulong id);
        Task SaveData(ulong id);
    }
}