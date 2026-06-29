using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    interface ICatalogDashboardUrlResolver
    {
        Task<string> PurchasingDashboardUrlGetter(string assetName);
    }
}
