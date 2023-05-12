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

    /// <summary>
    /// Represents a command that uses modals to interact with the user
    /// </summary>
    public interface IModalCommand : ICommand
    {
        /// <summary>
        /// The modals to handle
        /// </summary>
        List<string> ModalIds { get; }
        Task<(DataState Guild, DataState User)> HandleModal(SocketModal modal, GuildData guildData, UserData userData);
    }
}
