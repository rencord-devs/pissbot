using System.Globalization;
using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets.Commands
{
    public class LoreMasterCommand : IModalCommand
    {
        public string Name => "loremaster";

        public List<string> ModalIds { get; } = new List<string> 
        {
            EditModal
        };

        public const string EditModal = $"{nameof(LoreMasterCommand)}{nameof(EditModal)}";
        public const string WriteOption = "write";
        public const string UserOption = "user";
        public const string ChannelOption = "channel";
        public const string RoleOption = "role";
        private const string LoreValue = "lore_value";
        private readonly IUserDataPersistence userDataStore;
        private readonly IGuildDataPersistence guildDataStore;

        public LoreMasterCommand(IUserDataPersistence userDataStore, IGuildDataPersistence guildDataStore)
        {
            this.userDataStore = userDataStore ?? throw new ArgumentNullException(nameof(userDataStore));
            this.guildDataStore = guildDataStore;
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("🌖 Loremasters, build Moon Lore for your gracious followers 🌖") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(WriteOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("write lore")
                        .AddOption(UserOption, ApplicationCommandOptionType.User, "the user's lore to write", isRequired: false)
                        .AddOption(ChannelOption, ApplicationCommandOptionType.Channel, "the channel's lore to write", isRequired: false)
                        .AddOption(RoleOption, ApplicationCommandOptionType.Role, "the role's lore to write", isRequired: false));
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var writeOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption);
            if (writeOpt is not null) return WriteModal(command, guildData, userData);

            return Task.FromResult((DataState.Pristine, DataState.Pristine));
        }

        private async Task<(DataState Guild, DataState User)> WriteModal(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            (ILore? data, UserData? user) = await SelectWriteData(command, guildData);
            var config = guildData.GetOrAddData(() => new LoreConfiguration());

            var targetType = data is RoleLoreData 
                                            ? "role" 
                                            : data is ChannelLoreData 
                                                ? "channel" 
                                                : "user";
            var targetId = data is RoleLoreData rd
                                            ? rd.RoleId 
                                            : data is ChannelLoreData cd 
                                                ? cd.ChannelId
                                                : user?.Id;

            if (targetId is null)
            {
                await command.RespondAsync(ephemeral: true, text: "You must select either a user, role or channel when writing the command");
                return (DataState.Pristine, DataState.Pristine);
            }

            var modalId = $"{EditModal}_{targetType}_{targetId}";
            ModalIds.Add(modalId);

            if (data?.Lore?.Length > 1900) data.Lore = data.Lore.Substring(0, 1900);

            var modal = new ModalBuilder()
                .WithTitle("Moon child lore")
                .WithCustomId(modalId)
                .AddTextInput($"Lore for {data?.Name ?? user?.GuildUserName ?? guildData.Name}:", LoreValue, TextInputStyle.Paragraph, value: data?.Lore ?? string.Empty, required: false, maxLength: 1900);
            await command.RespondWithModalAsync(modal.Build());

            return (DataState.Pristine, DataState.Pristine);
        }

        private async Task<(ILore? lore, UserData? user)> SelectWriteData(SocketSlashCommand command, GuildData guildData)
        {
            UserData? selectedUserData = null;
            ILore? data = null;

            var userOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption)?.Options.FirstOrDefault(x => x.Name == UserOption);
            if (userOpt?.Value is IUser iu)
            {
                selectedUserData = await userDataStore.GetData(iu.Id);
                data = selectedUserData.GetOrAddData(() => new UserLoreData());
            }

            var chanOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption)?.Options.FirstOrDefault(x => x.Name == ChannelOption);
            if (chanOpt?.Value is IChannel chan)
            {
                if (guildData.GetOrAddData(() => new ServerLoreData()).ChannelLore.TryGetValue(chan.Id, out var chanLore))
                {
                    data = chanLore;
                }
                else
                {
                    data = guildData.GetOrAddData(() => new ServerLoreData()).ChannelLore[chan.Id] = new ChannelLoreData { ChannelId = chan.Id, ChannelName = chan.Name };
                }
            }

            var roleOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption)?.Options.FirstOrDefault(x => x.Name == RoleOption);
            if (roleOpt?.Value is IRole role)
            {
                if (guildData.GetOrAddData(() => new ServerLoreData()).RoleLore.TryGetValue(role.Id, out var roleLore))
                {
                    data = roleLore;
                }
                else
                {
                    data = guildData.GetOrAddData(() => new ServerLoreData()).RoleLore[role.Id] = new RoleLoreData { RoleId = role.Id, RoleName = role.Name };
                }
            }

            return (data, selectedUserData);
        }

        private async Task<(ILore? lore, UserData? user)> SelectSaveData(string type, ulong id, GuildData guildData)
        {
            UserData? selectedUserData = null;
            ILore? data = null;

            if (type == "user")
            {
                selectedUserData = await userDataStore.GetData(id);
                data = selectedUserData.GetOrAddData(() => new UserLoreData());
            }

            if (type == "channel")
            {
                data = guildData.GetOrAddData(() => new ServerLoreData()).ChannelLore[id];
                if (data is null)
                {
                    data = guildData.GetOrAddData(() => new ServerLoreData()).ChannelLore[id] = new ChannelLoreData { ChannelId = id };
                }
            }

            if (type == "role")
            {
                data = guildData.GetOrAddData(() => new ServerLoreData()).RoleLore[id];
                if (data is null)
                {
                    data = guildData.GetOrAddData(() => new ServerLoreData()).RoleLore[id] = new RoleLoreData { RoleId = id };
                }
            }

            if (data is null) return (null, selectedUserData);

            return (data, selectedUserData);
        }

        public async Task<(DataState Guild, DataState User)> HandleModal(SocketModal modal, GuildData guildData, UserData userData)
        {
            ModalIds.Remove(modal.Data.CustomId);
            var bits = modal.Data.CustomId.Split("_");
            var type = bits[1];
            var id = ulong.Parse(bits[2]);

            var data = await SelectSaveData(type, id, guildData);

            var value = modal.Data.Components.FirstOrDefault(x => x.CustomId == LoreValue);
            if (value is null) return (DataState.Pristine, DataState.Pristine);

            if (data.lore is not null)
                data.lore.Lore = value.Value;
            return (data.lore is null || data.lore is UserLoreData ? DataState.Pristine : DataState.Modified, 
                    data.lore is UserLoreData ? DataState.Modified : DataState.Pristine);
        }
    }
}
