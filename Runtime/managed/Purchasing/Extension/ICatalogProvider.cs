using System;
using System.Collections;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Extension
{
    public interface ICatalogProvider
    {
        void FetchProducts(Action<HashSet<ProductDefinition>> callback);
    }
}
