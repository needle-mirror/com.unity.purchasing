using System;

namespace UnityEngine.Purchasing
{
    public interface IExtensionProvider
    {
        T GetExtension<T>() where T : IStoreExtension;
    }
}
