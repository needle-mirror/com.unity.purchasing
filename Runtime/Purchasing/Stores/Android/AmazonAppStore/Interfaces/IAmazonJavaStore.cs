#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Interfaces
{
    interface IAmazonJavaStore : IAndroidJavaStore
    {
        public string GetAmazonUserId();
        public void NotifyUnableToFulfillUnavailableProduct(string transactionID);
        public void WriteSandboxJSON(HashSet<ProductDefinition> products);

    }
}
