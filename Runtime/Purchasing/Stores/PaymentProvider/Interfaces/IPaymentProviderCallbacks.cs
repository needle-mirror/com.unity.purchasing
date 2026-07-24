#nullable enable

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviders;

namespace UnityEngine.Purchasing
{
    interface IPaymentProviderCallbacks
    {
        void SetPaymentProviderOverride(string? paymentProviderOverride);
        void SetComplianceCheck(Func<PaymentProviderComplianceContext, Task<bool>>? complianceCheck);
        void SetCustomReferenceId(string? customReferenceId);
        void SetCustomMetadata(IReadOnlyDictionary<string, string>? customMetadata);
        Task<string?> GenerateURL(string? catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens = null);
        Task RedirectToWebshop(string? catalogListingId = null, IReadOnlyList<PaymentProviderToken>? externalTokens = null);
        void Purchase(ICart cart, string paymentProviderName);
        void PurchaseProduct(string catalogListingId, string paymentProviderName);
    }
}
