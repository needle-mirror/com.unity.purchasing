namespace UnityEngine.Purchasing.Interfaces
{
    interface IGooglePurchaseStateEnumProvider
    {
        int Purchased();
        int Pending();
    }
}
