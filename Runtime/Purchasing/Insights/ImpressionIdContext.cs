#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    // Process-wide slot for the impression_id correlation key (IAPSDKEvent
    // field 24). Minted at the start of a purchase journey — typically when
    // the Payment Option Provider modal is shown — and consumed by the next
    // PurchaseIntentStartEvent so the two land on the backend with the same
    // id. Static because the journey can cross store boundaries: the modal
    // runs in the PaymentProvider context, but if the player picks NATIVE
    // the subsequent PurchaseIntentStartEvent fires on the Apple / Google
    // PurchaseEventEmitter (a different DI scope, a different IPlayerData).
    //
    // Per the proto, impression_id is not propagated past
    // PurchaseIntentStartEvent — TakeOrMint clears the slot on consumption.
    internal static class ImpressionIdContext
    {
        static string? s_Current;

        // Clear on play-mode start so an id staged in one Editor session
        // doesn't leak into the next.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStaticsOnLoad()
        {
            s_Current = null;
        }

        // Generates a fresh id, stores it for a later TakeOrMint, and returns
        // it. Use at the start of a purchase journey (e.g. modal shown).
        public static string Mint()
        {
            var id = Guid.NewGuid().ToString();
            s_Current = id;
            return id;
        }

        // Returns the staged id (and clears it) if one exists; otherwise mints
        // a fresh id. Use at PurchaseIntentStartEvent so a journey that began
        // via the modal carries that modal's id, while a direct-purchase
        // journey (no modal) still gets a fresh id stamped on the event.
        public static string TakeOrMint()
        {
            var staged = s_Current;
            s_Current = null;
            return staged ?? Guid.NewGuid().ToString();
        }

        // Drops any staged id. Use when the modal closes without a selection —
        // prevents an abandoned modal's id from leaking into an unrelated
        // direct-purchase journey later in the same session.
        public static void Clear()
        {
            s_Current = null;
        }
    }
}
