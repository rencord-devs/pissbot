using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;

namespace Rencord.PissBot.Droplets.Commands
{
    public interface ICommand
    {
        string Name { get; }
        Task Configure(SlashCommandBuilder builder);
        Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData);
    }
}
