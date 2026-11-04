using System;
using System.Threading;
using System.Threading.Tasks;

namespace UnityEditor.Purchasing.Editor.Authoring.Deployment
{
    interface ICloudProjectLink
    {
        /// <summary>
        /// Returns the linked cloud project id, waiting briefly if the editor hasn't finished
        /// linking yet. Returns an empty string if the project is still not linked.
        /// </summary>
        Task<string> WaitForProjectIdAsync(CancellationToken cancellationToken);
    }

    /// <summary>
    /// On editor startup, <see cref="CloudProjectSettings.projectId"/> stays empty until project
    /// linking finishes, and requests made before then fail with a 404 that looks like
    /// "item not found".
    /// </summary>
    class CloudProjectLink : ICloudProjectLink
    {
        static readonly TimeSpan k_LinkTimeout = TimeSpan.FromSeconds(10);

        public async Task<string> WaitForProjectIdAsync(CancellationToken cancellationToken)
        {
            if (string.IsNullOrEmpty(CloudProjectSettings.projectId))
            {
                await WaitForProjectStateChange(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return CloudProjectSettings.projectId;
        }

        /// <summary>
        /// Completes when the project state changes, the timeout elapses, or the token is canceled,
        /// and always unsubscribes so an unlinked project doesn't accumulate listeners across syncs.
        /// </summary>
        static async Task WaitForProjectStateChange(CancellationToken cancellationToken)
        {
#if UNITY_2022_1_OR_NEWER
            var stateChanged = new TaskCompletionSource<bool>();
            Action onStateChanged = () => stateChanged.TrySetResult(true);
            var events = CloudProjectSettingsEventManager.instance;
            events.projectStateChanged += onStateChanged;
            try
            {
                using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                timeout.CancelAfter(k_LinkTimeout);
                using (timeout.Token.Register(() => stateChanged.TrySetResult(false)))
                {
                    await stateChanged.Task;
                }
            }
            finally
            {
                events.projectStateChanged -= onStateChanged;
            }
#else
            await Task.CompletedTask;
#endif
        }
    }
}
