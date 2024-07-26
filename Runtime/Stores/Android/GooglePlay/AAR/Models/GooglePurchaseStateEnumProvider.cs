using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Models
{
    [Preserve]
    class GooglePurchaseStateEnumProvider : IGooglePurchaseStateEnumProvider
    {
        public int Purchased()
        {
            return GooglePurchaseStateEnum.Purchased();
        }

        public int Pending()
        {
            return GooglePurchaseStateEnum.Pending();
        }
    }
}
