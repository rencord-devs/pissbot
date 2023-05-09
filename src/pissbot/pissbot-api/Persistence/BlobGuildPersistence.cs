using Microsoft.AspNetCore.Mvc.Formatters;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Options;
using Rencord.PissBot.Core;
using System.Xml.Linq;

namespace Rencord.PissBot.Persistence
{

    public class BlobGuildPersistence : BlobPersistence<GuildData>, IGuildDataPersistence
    {
        public BlobGuildPersistence(IOptions<BlobStoreOptions> options) : base(options)
        {
        }

        protected override Task<GuildData> NewData(ulong id) =>
            Task.FromResult(new GuildData());
    }
}