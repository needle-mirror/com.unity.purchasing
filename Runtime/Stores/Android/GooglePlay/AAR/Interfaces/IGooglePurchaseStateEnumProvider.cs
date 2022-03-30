namespace UnityEngine.Purchasing.Interfaces
{
    internal interface IGooglePurchaseStateEnumProvider
    {
        int Purchased();
        int Pending();
    }
}
