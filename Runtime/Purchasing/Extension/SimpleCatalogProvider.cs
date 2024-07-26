using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    // todo: make this public and remove it from purchasingfactory
    public class SimpleCatalogProvider : IBaseCatalogProvider
    {
        readonly Action<Action<List<ProductDefinition>>> m_Func;

        public SimpleCatalogProvider(Action<Action<List<ProductDefinition>>> func)
        {
            m_Func = func;
        }

        public void FetchProducts(Action<List<ProductDefinition>> callback)
        {
            m_Func?.Invoke(callback);
        }
    }
}
