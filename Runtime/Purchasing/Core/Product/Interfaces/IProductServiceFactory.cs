using System;

namespace UnityEngine.Purchasing
{
    interface IProductServiceFactory
    {
        void RegisterNewService(string name, Func<IProductService> createFunction);

        void RegisterNewExtendedService(string name, Func<IProductService, ExtensibleProductService> createFunction);

        IProductService Create(IStoreWrapper store);
    }
}
