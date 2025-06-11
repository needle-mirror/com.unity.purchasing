#nullable enable

namespace UnityEngine.Purchasing
{
    interface IStoreManager
    {
        IStoreWrapper GetStore(string name);
    }
}
