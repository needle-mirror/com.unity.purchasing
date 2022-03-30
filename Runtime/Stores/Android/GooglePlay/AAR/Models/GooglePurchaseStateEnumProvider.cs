using UnityEngine.Purchasing.Interfaces;

namespace UnityEngine.Purchasing.Models
{
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
