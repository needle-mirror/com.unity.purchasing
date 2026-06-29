using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    /// <summary> This client has adminastrative priviledges to read and modify the Remote Catalog </summary>
    public interface ILiveContentConfigClient
    {
        /// <summary> Initializes the client that will be used to populate the catalog </summary>
        /// <param name="environmentId">The environment ID</param>
        /// <param name="projectId">The project ID</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        /// <summary> Obtains the remote list of items in the catalog </summary>
        /// <param name="cancellationToken"></param>
        /// <returns>The entries of the catalog as a list</returns>
        Task<List<CatalogItem>> List(CancellationToken cancellationToken);

        /// <summary> Creates or updates the specified catalog item remotely. </summary>
        /// <param name="catalogItem">The item </param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task Upsert(CatalogItem catalogItem, CancellationToken cancellationToken);

        /// <summary> Deletes the specified catalog item from the remote catalog. </summary>
        /// <param name="catalogItem">The item to delete</param>
        /// <param name="cancellationToken">Cancellation Token</param>
        /// <returns>A Task that represents the asynchronous operation.</returns>
        Task Delete(CatalogItem catalogItem, CancellationToken cancellationToken);
    }
}
