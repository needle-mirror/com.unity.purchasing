#nullable enable
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Utilities
{
    /// <summary>
    /// Container class that houses a UnityUtil created through dependency injection.
    /// </summary>
    static class UnityUtilContainer
    {
        private static UnityUtil? s_UnityUtilInstance;

        /// <summary>
        /// Creates an instance of UnityUtil or retrieves the existing one.
        /// </summary>
        /// <returns> The existing instance or the one just created. </returns>
        internal static UnityUtil Instance()
        {
            s_UnityUtilInstance ??= UnityUtilDependencyInjector.CreateUnityUtils();

            return s_UnityUtilInstance;
        }
    }
}
