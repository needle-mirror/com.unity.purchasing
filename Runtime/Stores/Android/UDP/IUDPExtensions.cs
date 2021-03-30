namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Common interface for all UDP store purchasing extensions.
    /// </summary>
    public interface IUDPExtensions : IStoreExtension
    {
        /// <summary>
        /// Some stores return user information after initialization.
        /// </summary>
        /// <returns>UserInfo, which may be null</returns>
        object GetUserInfo(); //UDP UserInfo via Reflection

        /// <summary>
        /// Return the UDP initialization error.
        /// </summary>
        /// <returns>The error as a string</returns>
        string GetLastInitializationError();

        /// <summary>
        /// Enable debug log for UDP.
        /// </summary>
        /// <param name="enable"> Whether or not the logging is to be enabled. </param>
        void EnableDebugLog(bool enable);
    }
}
