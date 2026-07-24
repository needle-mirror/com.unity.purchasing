using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    interface ICatalogCsvLoader
    {
        Task<(List<CatalogItem> items, List<AssetState> issues)> ReadCatalog(
            string path,
            CancellationToken token);

        Task WriteCatalog(
            string path,
            List<CatalogItem> items,
            CancellationToken token);

        Task DeleteCatalog(string path, CancellationToken token);
    }
}
