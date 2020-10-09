namespace UnityEngine.Purchasing
{
    public interface IUDPExtensions : IStoreExtension
    {
        /// <summary>
        /// Some stores return user information after initialization.
        /// </summary>
        /// <returns>UserInfo may be null</returns>
        object GetUserInfo(); //UDP UserInfo via Reflection

        /// <summary>
        /// Return the UDP initialization error.
        /// </summary>
        /// <returns></returns>
        string GetLastInitializationError();

        /// <summary>
        /// Enable debug log for UDP.
        /// </summary>
        /// <param name="enable"></param>
        void EnableDebugLog(bool enable);
    }
}
