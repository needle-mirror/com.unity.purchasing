using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    [Obsolete]
    internal interface IStoreInternal
    {
        //TODO IAP-3119 make sure StandardPurchasingModule is no longer needed with core revamp
        // Internal mechanism for informing the store about the SPM (formerly used via reflection)
        //void SetModule(StandardPurchasingModule module);
    }
}
