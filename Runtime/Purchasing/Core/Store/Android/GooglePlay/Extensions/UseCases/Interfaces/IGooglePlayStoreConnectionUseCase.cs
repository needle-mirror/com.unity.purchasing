namespace UnityEngine.Purchasing
{
    /// <summary>
    ///  Interface for managing the connection to the Google Play Store.
    /// </summary>
    public interface IGooglePlayStoreConnectionUseCase
    {
        /// <summary>
        /// Ends the connection to the Google Play Store.
        /// </summary>
        public void EndConnection();
    }
}
