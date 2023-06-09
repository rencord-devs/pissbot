﻿using Discord;
using Discord.WebSocket;
using Microsoft.Extensions.Azure;
using System.Xml.Linq;

namespace Rencord.PissBot.Core
{
    public interface IDiscordClientFactory
    {
        Task DisposeClient();
        Task<DiscordSocketClient> GetClient();
        Task<DiscordSocketClient> InitialiseClient();
    }
}