#nullable enable

using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    // Bridges PurchaseService (Unity.Purchasing asmdef) to the Insights
    // PurchaseEvent subsystem (Unity.Purchasing.Stores asmdef). The interface
    // lives here so PurchaseService can call it without depending on Stores.
    // The concrete implementation lives in
    // Runtime/Stores/Data/Insights/PurchaseEventEmitter.cs.
    internal interface IPurchaseEventEmitter
    {
        void SendPurchaseIntentStartEvent(ICart cart);
        void SendPurchasePaidEvent(PendingOrder order, IPurchaseFulfilledPayload? payload);
        void SendPurchaseFailedEvent(FailedOrder order);
        void SendPurchaseFulfilledEvent(ConfirmedOrder order, IPurchaseFulfilledPayload? payload);
        void SendPaymentOptionsShownEvent(IReadOnlyList<PaymentOption> optionsShown, string? defaultProvider);
    }
}
