using System.Collections.Generic;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.IO
{
    interface ICatalogCsvParser
    {
        List<CatalogItem> Parse(string csvContent, out List<AssetState> issues);
        string Serialize(List<CatalogItem> items);
    }
}
