using System.Collections.Generic;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal interface ILiveContentAdapterService
    {
        Task<List<ConfigContentData>> GetConfigsContent(string schema = null, string schemaVersion = null, int? limit = null, string after = null);
    }
}
