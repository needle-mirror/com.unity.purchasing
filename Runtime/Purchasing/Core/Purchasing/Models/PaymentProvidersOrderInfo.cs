#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    class PaymentProvidersOrderInfo : OrderInfo, IPaymentProvidersOrderInfo
    {
        public string? CustomReferenceId { get;  }
        public IReadOnlyDictionary<string, string>? Metadata { get; }

        public PaymentProvidersOrderInfo(string transactionID, string storeName, string? customReferenceId, Dictionary<string, string>? metadata, DateTime? purchaseDate = null)
            : base(string.Empty, transactionID, storeName, purchaseDate)
        {
            CustomReferenceId = customReferenceId;
            Metadata = metadata;
        }
    }
}
