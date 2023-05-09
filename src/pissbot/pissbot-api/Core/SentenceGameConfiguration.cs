namespace Rencord.PissBot.Core
{
    public class GuildOptions
    {
        public const string Guilds = nameof(Guilds);
        public string? Name { get; set; }
        public ulong Id { get; set; }
        public SentenceGameConfiguration? SentenceGame { get; set; }
        public bool EnableLookingForPiss { get; set; }
    }

    public class SentenceGameConfiguration
    {
        public ulong? GameChannel { get; set; }

        public ulong? ResultsChannel { get; set; }

        public bool EnableSentenceGame { get; set; }
    }

    public class LookingForPissConfiguration
    {
        public bool EnableLookingForPiss { get; set; }
    }

    public class MiddleFingerConfiguration
    {
        public bool EnableMiddleFinger { get; set; }
        public long Time { get; set; } = 5;
        public List<MiddleFingerUser> Users { get; set; } = new List<MiddleFingerUser>();
    }

    public class MiddleFingerUser
    {
        public MiddleFingerUser(string mention, ulong id)
        {
            Mention = mention;
            Id = id;
        }

        public string? Mention { get; set; }
        public ulong Id { get; set; }
    }

    public class BlobStoreOptions
    {
        public const string BlobStore = nameof(BlobStore);

        public string? ConnectionString { get; set; }

        public string? Container { get; set; }
    }

    public class CosmosDbOptions
    {
        public const string CosmosDb = nameof(CosmosDb);

        public string? ConnectionString { get; set; }

        public string? DbName { get; set; }
    }
}