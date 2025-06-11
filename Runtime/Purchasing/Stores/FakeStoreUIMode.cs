namespace UnityEngine.Purchasing
{
    /// <summary>
    /// The various kinds of Fake Store UI presentations.
    /// Requires UIFakeStore variant of FakeStore to function.
    /// </summary>
    public enum FakeStoreUIMode
    {
        /// <summary>
        /// FakeStore by default displays no dialogs.
        /// </summary>
        Default,

        /// <summary>
        /// Simple dialog is shown when Purchasing.
        /// </summary>
        StandardUser,

        /// <summary>
        /// Dialogs with failure reason code selection when
        /// Initializing/Retrieving Products and when Purchasing.
        /// </summary>
        DeveloperUser
    }
}
