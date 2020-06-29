namespace UnityEngine.Purchasing.Extension
{
    /// <summary>
    /// Configures Unity Purchasing for one or more
    /// stores.
    ///
    /// Store implementations must provide an
    /// implementation of this interface.
    /// </summary>
    public interface IPurchasingModule
    {
        void Configure(IPurchasingBinder binder);
    }
}
