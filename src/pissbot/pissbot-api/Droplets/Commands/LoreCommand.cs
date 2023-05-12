using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using Rencord.PissBot.Persistence;

namespace Rencord.PissBot.Droplets.Commands
{
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
                   .WithDescription("🌖 Children of the Moon, build your own lore! 🌖") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
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
                .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1020271842633711696.webp?size=96&quality=lossless")
                .WithImageUrl("https://cdn.discordapp.com/emojis/1020268026764996629.webp?size=96&quality=lossless")
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
                .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1020271842633711696.webp?size=96&quality=lossless")
                .WithImageUrl("https://cdn.discordapp.com/emojis/1020268026764996629.webp?size=96&quality=lossless")
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
                    .WithThumbnailUrl("https://cdn.discordapp.com/emojis/1020271842633711696.webp?size=96&quality=lossless")
                    .WithImageUrl("https://cdn.discordapp.com/emojis/1020268026764996629.webp?size=96&quality=lossless")
                    .WithFields(new EmbedFieldBuilder()
                        .WithName($"{(user is IGuildUser gu && !string.IsNullOrWhiteSpace(gu.Nickname) ? gu.Nickname : user.Username)}'s personal lore:").WithValue(string.IsNullOrEmpty(data?.PersonalLore) ? "[No lore has been written]" : data?.PersonalLore), new EmbedFieldBuilder()
                        .WithName($"{(user is IGuildUser gu2 && !string.IsNullOrWhiteSpace(gu2.Nickname) ? gu2.Nickname : user.Username)}'s moon lore:").WithValue(string.IsNullOrEmpty(data?.Lore) ? "[No lore has been written]" : data?.Lore));
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
}
