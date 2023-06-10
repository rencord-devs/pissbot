using Discord;
using Discord.WebSocket;
using Rencord.PissBot.Core;
using System;

namespace Rencord.PissBot.Droplets.Commands
{
    public class KeywordReactsCommand : ICommand
    {
        public string Name => "keywordreacts";
        public const string EnableOption = "enable";
        public const string ConfigureOption = "configure";
        public const string ExcludeChannelOption = "excludechannel";
        public const string RemoveExcludeOption = "removeexclude";
        public const string AddTermOption = "addterm";
        public const string RemoveTermOption = "removeterm";
        public const string AddTermTermOption = "addtermterm";
        public const string AddTermEmojiOption = "addtermemoji";
        public const string RemoveTermTermOption = "removetermterm";

        public KeywordReactsCommand()
        {
        }

        public Task Configure(SlashCommandBuilder builder)
        {
            builder.WithName(Name)
                   .WithDescription("Enable reacts to posts with keywords.") // NOTE: 100 chars max!
                   .WithDefaultMemberPermissions(GuildPermission.ManageChannels)
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(ConfigureOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("configure")
                        .WithRequired(false)
                        .AddOption(EnableOption, ApplicationCommandOptionType.Boolean, "enable or disable the Gub-Gub reaccs", isRequired: false)
                        .AddOption(ExcludeChannelOption, ApplicationCommandOptionType.Channel, "exclude a channel from reactions", isRequired: false)
                        .AddOption(RemoveExcludeOption, ApplicationCommandOptionType.Channel, "stop excluding a channel", isRequired: false)
                   )
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(AddTermOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("add a term")
                        .WithRequired(false)
                        .AddOption(AddTermTermOption, ApplicationCommandOptionType.String, "the term", isRequired: true)
                        .AddOption(AddTermEmojiOption, ApplicationCommandOptionType.String, "the emoji to react", isRequired: true)
                   )
                   .AddOption(new SlashCommandOptionBuilder()
                        .WithName(RemoveTermOption)
                        .WithType(ApplicationCommandOptionType.SubCommand)
                        .WithDescription("remove a term")
                        .WithRequired(false)
                        .AddOption(RemoveTermTermOption, ApplicationCommandOptionType.String, "the term", isRequired: true)
                   );
            return Task.CompletedTask;
        }

        public Task<(DataState Guild, DataState User)> Handle(SocketSlashCommand command, GuildData guildData, UserData userData)
        {
            var config = guildData.GetOrAddData(() => new KeywordReactsConfiguration());
            var modified = DataState.Pristine;

            #pragma warning disable CS0618 // Type or member is obsolete - this code removes the obsolete data from the store
            var oldConfig = guildData.RemoveData<GubGubConfiguration>();
            if (oldConfig is not null)
            {
                // migrate existing thing
                config.WatchTerms["gub-gub"] = "<:letsgo:1020270999037554719>";
                config.WatchTerms["gub gub"] = "<:letsgo:1020270999037554719>";
                config.WatchTerms["gubgub"] = "<:letsgo:1020270999037554719>";
                modified = DataState.Modified;
            }
            #pragma warning restore CS0618 // Type or member is obsolete

            if (command.Data?.Options is null) return Respond((modified, DataState.Pristine), config, command, guildData);

            var configureOpt = command.Data.Options.FirstOrDefault(x => x.Name == ConfigureOption);
            if (configureOpt is not null)
            {
                var enableOpt = configureOpt.Options.FirstOrDefault(x => x.Name == EnableOption);
                if (enableOpt?.Value is bool value)
                {
                    if (config.EnableKeywordReacts != value)
                        modified = DataState.Modified;
                    config.EnableKeywordReacts = value;
                }

                var excludeOpt = configureOpt.Options.FirstOrDefault(x => x.Name == ExcludeChannelOption);
                if (excludeOpt?.Value is IChannel chan)
                {
                    if (!config.ExcludedChannels.Any(c => c.Id == chan.Id))
                    {
                        modified = DataState.Modified;
                        config.ExcludedChannels.Add(new ChannelSummary { Id = chan.Id, Name = chan.Name });
                    }
                }

                var removeExcludeOpt = configureOpt.Options.FirstOrDefault(x => x.Name == RemoveExcludeOption);
                if (removeExcludeOpt?.Value is IChannel chan2)
                {
                    if (config.ExcludedChannels.Any(c => c.Id == chan2.Id))
                    {
                        modified = DataState.Modified;
                        config.ExcludedChannels.RemoveAll(c => c.Id == chan2.Id);
                    }
                }
            }
            
            var addTermOpt = command.Data.Options.FirstOrDefault(x => x.Name == AddTermOption);
            if (addTermOpt is not null)
            {
                var term = addTermOpt.Options.First(x => x.Name == AddTermTermOption);
                var emoji = addTermOpt.Options.First(x => x.Name == AddTermEmojiOption);
                config.WatchTerms[((string)term.Value).ToLower()] = ((string)emoji.Value).ToLower();
                modified = DataState.Modified;
            }

            var removeTermOpt = command.Data.Options.FirstOrDefault(x => x.Name == RemoveTermOption);
            if (removeTermOpt is not null)
            {
                var term = removeTermOpt.Options.First(x => x.Name == RemoveTermTermOption);
                if (config.WatchTerms.ContainsKey(((string)term.Value).ToLower()))
                {
                    config.WatchTerms.Remove(((string)term.Value).ToLower());
                    modified = DataState.Modified;
                }
            }

            return Respond((modified, DataState.Pristine), config, command, guildData);
        }

        private async Task<(DataState Guild, DataState User)> Respond((DataState modified, DataState Pristine) result,
                                                                      KeywordReactsConfiguration config,
                                                                      SocketSlashCommand command,
                                                                      GuildData guildData)
        {
            var eb = new EmbedBuilder();
            var val = string.Join(", ", config.ExcludedChannels.Where(x => x.Name is not null).Select(x => x.Name));
            if (string.IsNullOrWhiteSpace(val))
                val = "[no exlcudes]";
            var val2 = string.Join("\r\n", config.WatchTerms.Select(x => $"{x.Key} - {x.Value}"));
            if (string.IsNullOrWhiteSpace(val))
                val2 = "[no terms]";
            eb.WithTitle("Keyword Reacts configuration")
              .WithDescription($"The current configuration of PissBot Keyword Reacts on {guildData.Name}")
              .WithFields(
                new EmbedFieldBuilder().WithName("enabled").WithValue(config.EnableKeywordReacts).WithIsInline(true),
                new EmbedFieldBuilder().WithName("terms").WithValue(val2).WithIsInline(false),
                new EmbedFieldBuilder().WithName("excluded").WithValue(val).WithIsInline(false))
              .WithColor(Color.DarkPurple);
            await command.RespondAsync(ephemeral: true, embed: eb.Build());
            return result;
        }
    }
}
