using Discord;
using Microsoft.Extensions.Azure;
using System.Xml.Linq;

namespace Rencord.PissBot.Core
{
    public interface IDiscordClientFactory
    {
        Task<IDiscordClient> GetClient();
    }
}