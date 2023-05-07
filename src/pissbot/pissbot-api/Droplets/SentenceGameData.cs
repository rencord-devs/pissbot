namespace Rencord.PissBot.Droplets
{
    public class SentenceGameData
    {
        public ulong SentenceCount { get; set; }
        public List<string> CurrentSentence { get; set; } = new List<string>();
        public List<string> SentenceAuthors { get; set; } = new List<string>();
        public ulong PreviousAuthor { get; set; }
    }
}
