using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    interface ICatalogLoader
    {
        Task<CatalogEntryDeploymentItem> ReadCatalog(string path, CancellationToken token);
        Task CreateOrUpdateCatalog(CatalogEntryDeploymentItem deployableEntryDeploymentItem, CancellationToken token);
        Task DeleteCatalog(CatalogEntryDeploymentItem entryDeploymentItem, CancellationToken token);
        void DeserializeAndPopulateFromPath(CatalogEntryDeploymentItem config, string path);
    }
}
