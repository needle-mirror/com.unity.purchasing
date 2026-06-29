namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal enum OrderStatus
    {
        Created,
        Cancelled,
        Paid,
        Fulfilled,
        Failed,
        Revoked,
        Unknown,
    }
}
