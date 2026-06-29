#nullable enable
namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Entry point for the <see cref="IDeepLinkService"/>.
    /// </summary>
    public static class DeepLinkManager
    {
        static DeepLinkService? s_Instance;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            // Dispose before nulling so the old instance unhooks from
            // Application.deepLinkActivated (matters when Disable Domain Reload is on,
            // otherwise the stale subscription leaks and double-dispatches across sessions).
            s_Instance?.Dispose();
            s_Instance = null;
        }

        // Created early so the launch URL is captured and a deep link arriving
        // before game code subscribes is not missed; the link is cached until
        // the first handler attaches.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterAssembliesLoaded)]
        static void Bootstrap()
        {
            _ = GetDeepLinkService();
        }

        public static IDeepLinkService GetDeepLinkService()
        {
            return s_Instance ??= new DeepLinkService();
        }
    }
}
