namespace Rencord.PissBot.Core
{
    public class GuildOptions
    {
        public const string Guilds = nameof(Guilds);
        public string? Name { get; set; }
        public ulong Id { get; set; }
        public SentenceGameOptions? SentenceGame { get; set; }
        public bool EnableLookingForPiss { get; set; }
    }

    public class SentenceGameOptions
    {
        public const string SentenceGame = nameof(SentenceGame);

        public ulong? GameChannel { get; set; }

        public ulong? ResultsChannel { get; set; }
    }

    public class BlobStoreOptions
    {
        public const string BlobStore = nameof(BlobStore);

        public string? ConnectionString { get; set; }

        public string? Container { get; set; }
    }
}