using System.Text.Json.Serialization;
using Discord;
using Discord.WebSocket;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets.Commands
{
    public class LookingForPissCommand : ICommand
    {
        public string Name => "lookingforpiss";
        public const string EnableOption = "enable";

        public LookingForPissCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable :notp: reacts to posts that say piss when made by a member with a role with piss in the name") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the piss reaccs", isRequired: true);
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var opt = command.Data.Options.First(x => x.Name == EnableOption);
            var config = guildData.GetOrAddData(() => new LookingForPissConfiguration());
            if (opt.Value is not bool value) return Respond((DataState.Pristine, DataState.Pristine), config, command, guildData);
            var modified = config.EnableLookingForPiss == value
                ? DataState.Pristine
                : DataState.Modified;
            config.EnableLookingForPiss = value;
            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      LookingForPissConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            eb.WithTitle("Loooking for Piss configuration")
              .WithDescription($"The current configuration of PissBot looking for piss on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableLookingForPiss).WithIsInline(true))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }

    public class LoreConfiguration
    {
        public RoleSummary? MemberRole { get; set; }
        public RoleSummary? MasterRole { get; set; }
    }

    public class RoleSummary
    {
        public string Mention { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public ulong Id { get; set; }
    }

    public interface ILore
    {
        string Lore { get; set; }
        string? Name { get; }
    }

    public class UserLoreData : ILore
    {
        public string PersonalLore { get; set; } = string.Empty;
        public string Lore { get; set; } = string.Empty;
        [JsonIgnore]
        public string? Name => null;
    }

    public class ServerLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => null;
        public string Lore { get; set; } = string.Empty;
        public Dictionary<ulong, RoleLoreData> RoleLore { get; set; } = new Dictionary<ulong, RoleLoreData>();
        public Dictionary<ulong, ChannelLoreData> ChannelLore { get; set; } = new Dictionary<ulong, ChannelLoreData>();
    }

    public class RoleLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => RoleName;
        public string RoleName { get; set; } = string.Empty;
        public ulong RoleId { get; set; }
        public string Lore { get; set; } = string.Empty;
    }
    public class ChannelLoreData : ILore
    {
        [JsonIgnore]
        public string? Name => ChannelName;
        public string ChannelName { get; set; } = string.Empty;
        public ulong ChannelId { get; set; }
        public string Lore { get; set; } = string.Empty;
    }

    public class LoreCommand : IModalCommand
    {
        public string Name => "lore";

        public List<string> ModalIds { get; } = new List<string> 
        {
            EditModal
        };

        public const string EditModal = $"{nameof(LoreCommand)}{nameof(EditModal)}";
        public const string WriteOption = "write";
        public const string ReadOption = "read";
        public const string UserOption = "user";
        public const string ChannelOption = "channel";
        public const string RoleOption = "role";
        private const string LoreValue = "lore_value";
        private readonly IUserDataPersistence userDataStore;
        private readonly IGuildDataPersistence guildDataStore;

        public LoreCommand(IUserDataPersistence userDataStore, IGuildDataPersistence guildDataStore)
        {
            this.userDataStore = userDataStore ?? throw new ArgumentNullException(nameof(userDataStore));
            this.guildDataStore = guildDataStore;
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Children of the Moon") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.SendMessages)
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(ReadOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("read lore")
                        .AddOption(UserOption, ApplicationCommandOptionType.User, "the user's lore to read (select none to read your own)", isRequired: false)
                        .AddOption(ChannelOption, ApplicationCommandOptionType.Channel, "the channel's lore to read", isRequired: false)
                        .AddOption(RoleOption, ApplicationCommandOptionType.Role, "the role's lore to read", isRequired: false))
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(WriteOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("write or update your own lore"));
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var writeOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption);
            if (writeOpt is not null) return WriteModal(command, guildData, userData);

            var readOpt = command.Data.Options.FirstOrDefault(x => x.Name == ReadOption);
            if (readOpt is not null) 
            {
                var userOpt = readOpt.Options.FirstOrDefault(x => x.Name == UserOption);
                if (userOpt?.Value is IUser iu) return ReadLore(command, guildData, userData, iu);
                var chanOpt = readOpt.Options.FirstOrDefault(x => x.Name == ChannelOption);
                if (chanOpt?.Value is IChannel chan) return ReadLore(command, guildData, userData, chan);
                var roleOpt = readOpt.Options.FirstOrDefault(x => x.Name == RoleOption);
                if (roleOpt?.Value is IRole role) return ReadLore(command, guildData, userData, role);
                return ReadLore(command, guildData, userData, command.User);
            }

            return Task.FromResult((DataState.Pristine, DataState.Pristine));
        }

        private async Task<(DataState Guild, DataState User)> ReadLore(SocketSlashCommand command, GuildData guildData, UserData userData, IRole role)
        {
            var config = guildData.GetOrAddData(() => new LoreConfiguration());
            var data = guildData.GetOrAddData(() => new ServerLoreData());
            var roleLore = data.RoleLore[role.Id];
            if (roleLore is null)
            {
                data.RoleLore[role.Id] = roleLore = new RoleLoreData{ RoleId = role.Id, RoleName = role.Name };
            }

            var embed = new EmbedBuilder()
                .WithTitle("Moon child lore")
                .WithColor(Color.DarkGrey)
                .WithFields(new EmbedFieldBuilder()
                    .WithName($"{role.Name}'s moon lore:").WithValue(roleLore?.Lore ?? "[No lore has been written]"));
            await command.RespondAsync(embed: embed.Build());

            return (DataState.Modified, DataState.Pristine);
        }

        private async Task<(DataState Guild, DataState User)> ReadLore(SocketSlashCommand command, GuildData guildData, UserData userData, IChannel chan)
        {
            var config = guildData.GetOrAddData(() => new LoreConfiguration());
            var data = guildData.GetOrAddData(() => new ServerLoreData());
            var chanLore = data.ChannelLore[chan.Id];
            var saveTask = Task.CompletedTask;
            if (chanLore is null)
            {
                data.ChannelLore[chan.Id] = chanLore = new ChannelLoreData{ ChannelId = chan.Id, ChannelName = chan.Name };
                saveTask = guildDataStore.SaveData(command.GuildId!.Value); // awaited later, run in parallel with sending reponse
            }

            var embed = new EmbedBuilder()
                .WithTitle("Moon child lore")
                .WithColor(Color.DarkGrey)
                .WithFields(new EmbedFieldBuilder()
                    .WithName($"{chan.Name}'s moon lore:").WithValue(chanLore?.Lore ?? "[No lore has been written]"));
            await command.RespondAsync(embed: embed.Build());
            await saveTask;

            return (DataState.Pristine, DataState.Pristine);
        }

        private async Task<(DataState Guild, DataState User)> ReadLore(SocketSlashCommand command, GuildData guildData, UserData userData, IUser user)
        {
            var config = guildData.GetOrAddData(() => new LoreConfiguration());
            var data = 
                (user == command.User
                    ? userData
                    : await userDataStore.GetData(user.Id)).GetOrAddData(() => new UserLoreData());

            try
            {
                var embed = new EmbedBuilder()
                    .WithTitle("Moon child lore")
                    .WithColor(Color.DarkGrey)
                    .WithFields(new EmbedFieldBuilder()
                        .WithName($"{user.Mention}'s personal lore:").WithValue(string.IsNullOrEmpty(data?.PersonalLore) ? "[No lore has been written]" : data?.PersonalLore), new EmbedFieldBuilder()
                        .WithName($"{user.Mention}'s moon lore:").WithValue(string.IsNullOrEmpty(data?.Lore) ? "[No lore has been written]" : data?.Lore));
                await command.RespondAsync(embed: embed.Build());
            }
            catch (Exception ex)
            {

            }


            if (user != command.User)
            {
                await userDataStore.SaveData(user.Id);
            }

            return (DataState.Pristine, DataState.Pristine);
        }

        private async Task<(DataState Guild, DataState User)> WriteModal(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new LoreConfiguration());
            var data = userData.GetOrAddData(() => new UserLoreData());
            var modal = new ModalBuilder()
                .WithTitle("Moon child lore")
                .WithCustomId(EditModal)
                .AddTextInput("Your personal lore:", LoreValue, TextInputStyle.Paragraph, value: data?.PersonalLore ?? string.Empty, required: false, maxLength: 4000);
            await command.RespondWithModalAsync(modal.Build());

            return (DataState.Pristine, DataState.Pristine);
        }

        public Task<(DataState Guild, DataState User)> HandleModal(SocketModal modal, GuildData guildData, UserData userData)
        {
            var value = modal.Data.Components.FirstOrDefault(x => x.CustomId == LoreValue);
            if (value is null) return Task.FromResult((DataState.Pristine, DataState.Pristine));
            var data = userData.GetOrAddData(() => new UserLoreData());
            data.PersonalLore = value.Value;
            return Task.FromResult((DataState.Pristine, DataState.Modified));
        }
    }

    public class LoreMasterCommand : IModalCommand
    {
        public string Name => "loremaster";

        public List<string> ModalIds { get; } = new List<string> 
        {
            EditModal
        };

        public const string EditModal = $"{nameof(LoreMasterCommand)}{nameof(EditModal)}";
        public const string WriteOption = "write";
        public const string RolesOption = "roles";
        public const string MemberRoleOption = "memberrole";
        public const string MasterRoleOption = "loremasterrole";
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
                   .WithDescription("Children of the Moon") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(WriteOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("write lore")
                        .AddOption(UserOption, ApplicationCommandOptionType.User, "the user's lore to write", isRequired: false)
                        .AddOption(ChannelOption, ApplicationCommandOptionType.Channel, "the channel's lore to write", isRequired: false)
                        .AddOption(RoleOption, ApplicationCommandOptionType.Role, "the role's lore to write", isRequired: false))
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(RolesOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("set the roles required for the lore commands")
                        .AddOption(MemberRoleOption, ApplicationCommandOptionType.Role, "the role required to write personal lore and read lore", isRequired: false)
                        .AddOption(MasterRoleOption, ApplicationCommandOptionType.Role, "the role required for loremaster commands", isRequired: false));
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var writeOpt = command.Data.Options.FirstOrDefault(x => x.Name == WriteOption);
            if (writeOpt is not null) return WriteModal(command, guildData, userData);

            var rolesOpt = command.Data.Options.FirstOrDefault(x => x.Name == RolesOption);
            if (rolesOpt is not null) return SetRoles(command, guildData);

            return Task.FromResult((DataState.Pristine, DataState.Pristine));
        }

        private static async Task<(DataState Guild, DataState User)> SetRoles(SocketSlashCommand command, GuildData guildData)
        {
            var config = guildData.GetOrAddData(() => new LoreConfiguration());
            var data = guildData.GetOrAddData(() => new ServerLoreData());

            var userOpt = command.Data.Options.FirstOrDefault(x => x.Name == RolesOption)?.Options.FirstOrDefault(x => x.Name == MemberRoleOption);
            if (userOpt?.Value is IRole role)
            {
                config.MemberRole = new RoleSummary
                {
                    Name = role.Name,
                    Mention = role.Mention,
                    Id = role.Id
                };
            }
            var masterOpt = command.Data.Options.FirstOrDefault(x => x.Name == RolesOption)?.Options.FirstOrDefault(x => x.Name == MasterRoleOption);
            if (masterOpt?.Value is IRole role2)
            {
                config.MasterRole = new RoleSummary
                {
                    Name = role2.Name,
                    Mention = role2.Mention,
                    Id = role2.Id
                };
            }
            var embed = new EmbedBuilder()
                .WithTitle("Moon child lore roles")
                .WithColor(Color.DarkGrey)
                .WithFields(new EmbedFieldBuilder()
                    .WithName($"Member role:").WithValue(config.MemberRole?.Mention ?? "[No role has been set]"),new EmbedFieldBuilder()
                    .WithName($"Loremaster role:").WithValue(config.MasterRole?.Mention ?? "[No role has been set]"));
            await command.RespondAsync(embed: embed.Build(), ephemeral: true);
            return (DataState.Modified, DataState.Pristine);
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
                                                : userData.Id;
            var modalId = $"{EditModal}_{targetType}_{targetId}";
            ModalIds.Add(modalId);

            var modal = new ModalBuilder()
                .WithTitle("Moon child lore")
                .WithCustomId(modalId)
                .AddTextInput($"Lore for {data?.Name ?? user?.GuildUserName ?? guildData.Name}:", LoreValue, TextInputStyle.Paragraph, value: data?.Lore ?? string.Empty, required: false, maxLength: 4000);
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

            if (data is null) return (null, selectedUserData);

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
