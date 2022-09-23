using System;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    internal class SimpleCatalogProvider : ICatalogProvider
    {
        private readonly Action<Action<HashSet<ProductDefinition>>> m_Func;

        internal SimpleCatalogProvider(Action<Action<HashSet<ProductDefinition>>> func)
        {
            m_Func = func;
        }

        public void FetchProducts(Action<HashSet<ProductDefinition>> callback)
        {
            m_Func?.Invoke(callback);
        }
    }
}
