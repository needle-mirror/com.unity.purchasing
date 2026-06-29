#nullable enable

namespace UnityEngine.Purchasing
{
    // Mirrors insights.producers.iapsdk.v1alpha1.PaymentOption (proto enum).
    // Int values must match the proto wire numbers so the binary writer can
    // cast directly. Lives in the Unity.Purchasing asmdef rather than under
    // Insights.Models because IPurchaseEventEmitter.SendPaymentOptionsShownEvent
    // crosses the Purchasing → Stores asmdef boundary: Unity.Purchasing.Stores
    // references Unity.Purchasing, not the other way, so the enum at the
    // interface must live on the Purchasing side. The Stores-side DTO and
    // writer reference this same enum.
    internal enum PaymentOption
    {
        Unspecified = 0,
        Native = 1,
        Stripe = 2,
        Codapay = 3,
        Webshop = 4
    }
}
