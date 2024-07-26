#nullable enable

using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    interface IStoreManager
    {
        IStoreWrapper GetStore(string? name);
    }
}
