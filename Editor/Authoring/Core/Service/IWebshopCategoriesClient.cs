using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    interface IWebshopCategoriesClient
    {
        Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        /// <summary>Returns null when the file does not exist on the server.</summary>
        Task<WebshopCategories> Get(CancellationToken cancellationToken);

        Task Upsert(WebshopCategories categories, CancellationToken cancellationToken);
    }
}
