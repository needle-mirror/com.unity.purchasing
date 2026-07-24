#nullable enable
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class PaymentProvidersOrderInfo : OrderInfo, IPaymentProvidersOrderInfo
    {
        public string? CustomReferenceId { get;  }
        public IReadOnlyDictionary<string, string>? Metadata { get; }

        public PaymentProvidersOrderInfo(string transactionID, string storeName, string? customReferenceId, Dictionary<string, string>? metadata)
            : base(string.Empty, transactionID, storeName)
        {
            CustomReferenceId = customReferenceId;
            Metadata = metadata;
        }
    }
}
