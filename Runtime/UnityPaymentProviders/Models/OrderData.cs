#nullable enable
using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal struct OrderData
    {
        public Guid id;
        public Guid projectId;
        public Guid environmentId;
        public string playerId;
        public string paymentProvider;
        public string paymentProviderResourceId;
        public string paymentProviderUrl;

        public IReadOnlyList<LineItem> lineItems;

        public OrderStatus status; // [created, paid, failed, fulfilled, revoked]

        public DateTime? fulfilledAt;
        public DateTime? revokedAt;

        public string? customReferenceId;

        public Dictionary<string, string> metadata;
        public string url;

        public DateTime createdAt;
        public DateTime updatedAt;
    }

    internal struct LineItem
    {
        public string unitySku;
        public string productType;
    }
}
