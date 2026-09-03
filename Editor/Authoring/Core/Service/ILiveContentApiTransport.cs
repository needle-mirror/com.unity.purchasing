using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    /// <summary>
    /// Abstracts the Live Content Config HTTP surface from the <see cref="ILiveContentConfigClient"/>
    /// Each client must provide its own adapter
    /// <see cref="InitializeAsync"/> must be called once before any other method.
    /// </summary>
    internal interface ILiveContentApiTransport
    {
        /// <summary>
        /// Initialize the transport information.
        /// Implementations should also perform an initial auth-token refresh here.
        /// </summary>
        /// <param name="environmentId">target environment id</param>
        /// <param name="projectId">target project id</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns></returns>
        Task InitializeAsync(string environmentId, string projectId, CancellationToken cancellationToken);

        /// <summary>
        /// Lists config paths under <paramref name="pathPrefix"/> (e.g. <c>"catalog/"</c>).
        /// The <see cref="TransportResult.Content"/> of a successful result contains a JSON
        /// payload from which paths are extracted via <c>$..path</c> JSONPath.
        /// </summary>
        Task<TransportResult> GetConfigPathsAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
            bool noVariantTag,
            CancellationToken cancellationToken);

        Task<TransportResult> GetConfigContentAsync(string path, CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new config entry.
        /// </summary>
        /// <param name="path">path of the asset being created</param>
        /// <param name="jsonContent">fully-assembled DTO json</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>Resulting <see cref="TransportResult"/></returns>
        Task<TransportResult> CreateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing config entry.
        /// </summary>
        /// <param name="path">path of the asset being updated</param>
        /// <param name="jsonContent">fully-assembled DTO json</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>Resulting <see cref="TransportResult"/></returns>
        Task<TransportResult> UpdateConfigAsync(string path, string jsonContent, CancellationToken cancellationToken);

        /// <summary>
        /// Delete an existing config entry.
        /// </summary>
        /// <param name="path">Path to delete the configuration</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>Resulting <see cref="TransportResult"/></returns>
        Task<TransportResult> DeleteConfigAsync(string path, CancellationToken cancellationToken);
    }
}
