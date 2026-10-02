using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

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
        Task InitializeAsync(
            string environmentId,
            string projectId,
            CancellationToken cancellationToken);

        /// <summary>
        /// Lists config under <paramref name="pathPrefix"/> (e.g. <c>"catalog/"</c>) as
        /// metadata only. Each API document is flattened into one <see cref="LiveContentConfig"/>
        /// </summary>
        Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
            CancellationToken cancellationToken);

        /// <summary>
        /// Lists config metadata and inline content under <paramref name="pathPrefix"/>.
        /// Each API document is flattened into one <see cref="LiveContentConfig"/> per variant.
        /// </summary>
        Task<TransportResult<IReadOnlyList<LiveContentConfig>>> GetConfigsContentAsync(
            string pathPrefix,
            int limit,
            string after,
            bool? start,
            string schema,
            CancellationToken cancellationToken);

        /// <summary>
        /// Fetches the body of a config at <paramref name="path"/>.
        /// The result carries only the body, not document metadata.
        /// </summary>
        Task<TransportResult<LiveContentConfigBody>> GetConfigContentAsync(
            string path,
            CancellationToken cancellationToken);

        /// <summary>
        /// Creates a new config entry.
        /// </summary>
        /// <param name="path">path of the asset being created</param>
        /// <param name="jsonContent">fully-assembled DTO json</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>The created config's resulting metadata.</returns>
        Task<TransportResult<LiveContentConfig>> CreateConfigAsync(
            string path,
            string jsonContent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Updates an existing config entry.
        /// </summary>
        /// <param name="path">path of the asset being updated</param>
        /// <param name="jsonContent">fully-assembled DTO json</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>The updated config's resulting metadata.</returns>
        Task<TransportResult<LiveContentConfig>> UpdateConfigAsync(
            string path,
            string jsonContent,
            CancellationToken cancellationToken);

        /// <summary>
        /// Delete an existing config entry.
        /// </summary>
        /// <param name="path">Path to delete the configuration</param>
        /// <param name="cancellationToken">cancellation token for the async call</param>
        /// <returns>A payload-less <see cref="TransportResult"/> carrying the status outcome.</returns>
        Task<TransportResult> DeleteConfigAsync(
            string path,
            CancellationToken cancellationToken);
    }
}
