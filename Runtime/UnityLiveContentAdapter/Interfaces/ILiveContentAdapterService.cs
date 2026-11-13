using System.Threading.Tasks;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal interface ILiveContentAdapterService
    {
        Task<ConfigContentPage> GetConfigsContent(string schema = null, string schemaVersion = null, int? limit = null, string after = null);
    }
}
