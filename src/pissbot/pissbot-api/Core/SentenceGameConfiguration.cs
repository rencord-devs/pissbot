using Microsoft.Azure.Cosmos.Linq;

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

    public class GubGubConfiguration
    {
        public bool EnableGubGub { get; set; } = true;
    }

    public class LookingForPissConfiguration
    {
        public bool EnableLookingForPiss { get; set; }

        public List<PissLeagueEntry> PissLeague { get; set; } = new List<PissLeagueEntry>();
        
        public void AddPiss(ulong id, int pissesToAdd, string mention)
        {
            var existing = PissLeague.FirstOrDefault(x => x.Id == id);
            if (existing is null)
                PissLeague.Add(existing = new PissLeagueEntry { Id = id, Mention = mention });
            existing.PissCount += pissesToAdd;
            PissLeague.Sort((x, y) => y.PissCount.CompareTo(x.PissCount));
        }
    }

    public class PissLeagueEntry
    {
        public ulong Id { get; set; }
        public string Mention { get; set; } = string.Empty;
        public int PissCount { get; set; }
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