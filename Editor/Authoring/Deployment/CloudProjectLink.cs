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
        /// <remarks>
        /// A project id alone doesn't mean the project is linked: ProjectSettings can carry a
        /// <c>cloudProjectId</c> the signed-in user can't access, in which case the Services
        /// settings show the project as unlinked and every request for it fails with a 403.
        /// </remarks>
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
            if (!IsLinked())
            {
                await WaitForProjectStateChange(cancellationToken);
                cancellationToken.ThrowIfCancellationRequested();
            }

            return IsLinked() ? CloudProjectSettings.projectId : string.Empty;
        }

        static bool IsLinked()
            => CloudProjectSettings.projectBound && !string.IsNullOrEmpty(CloudProjectSettings.projectId);

        /// <summary>
        /// Completes when the project state changes, the timeout elapses, or the token is canceled,
        /// and always unsubscribes so an unlinked project doesn't accumulate listeners across syncs.
        /// </summary>
        /// <remarks>
        /// An assembly reload cancels the wait. The timeout's timer and both events live outside
        /// this package's assemblies, so while the wait is pending they keep the whole deployment
        /// object graph alive, and the reload leaves those assemblies loaded (on CoreCLR, a leak
        /// that fails leak-checked test runs). The `finally` below can't release them in time: it
        /// runs in a continuation queued for a later editor frame, and the reload happens first.
        /// Canceling closes the timer and runs the registration below synchronously.
        /// </remarks>
        static async Task WaitForProjectStateChange(CancellationToken cancellationToken)
        {
#if UNITY_2022_1_OR_NEWER
            var stateChanged = new TaskCompletionSource<bool>();
            var events = CloudProjectSettingsEventManager.instance;
            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            Action onStateChanged = () => stateChanged.TrySetResult(true);
            AssemblyReloadEvents.AssemblyReloadCallback onReload = () => timeout.Cancel();

            void Unsubscribe()
            {
                events.projectStateChanged -= onStateChanged;
                AssemblyReloadEvents.beforeAssemblyReload -= onReload;
            }

            events.projectStateChanged += onStateChanged;
            AssemblyReloadEvents.beforeAssemblyReload += onReload;
            try
            {
                timeout.CancelAfter(k_LinkTimeout);
                using (timeout.Token.Register(() =>
                {
                    Unsubscribe();
                    stateChanged.TrySetResult(false);
                }))
                {
                    await stateChanged.Task;
                }
            }
            finally
            {
                Unsubscribe();
            }
#else
            await Task.CompletedTask;
#endif
        }
    }
}
