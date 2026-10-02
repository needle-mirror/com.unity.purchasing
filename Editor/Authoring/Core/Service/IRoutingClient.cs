using System.Threading;
using System.Threading.Tasks;
using UnityEditor.Purchasing.Editor.Authoring.Core.Model;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Service
{
    /// <summary> Client for reading and updating the Payment Provider routing (targeting) configuration. </summary>
    interface IRoutingClient
    {
        /// <summary> Initializes the client with the target environment and project. </summary>
        /// <param name="environmentId">The environment ID.</param>
        /// <param name="projectId">The project ID.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task Initialize(string environmentId, string projectId, CancellationToken cancellationToken);

        /// <summary> Fetches the current routing configuration from the server. </summary>
        /// <param name="cancellationToken">Cancellation token.</param>
        /// <returns>The remote routing configuration.</returns>
        Task<ProviderRoutingConfig> FetchRouting(CancellationToken cancellationToken);

        /// <summary> Pushes a routing configuration to the server, replacing the current one. </summary>
        /// <param name="config">The routing configuration to push.</param>
        /// <param name="cancellationToken">Cancellation token.</param>
        Task PushRouting(ProviderRoutingConfig config, CancellationToken cancellationToken);
    }
}
