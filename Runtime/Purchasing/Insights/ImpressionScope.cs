#nullable enable

using System;
using System.Threading;

namespace UnityEngine.Purchasing
{
    // Ambient impression_id (IAPSDKEvent field 24) for the purchase journey
    // running on the current async flow. Backed by AsyncLocal, so the id passes
    // through synchronous calls and awaits — including developer-overridden
    // interfaces — without leaking into concurrent flows. It is dropped wherever
    // the call chain breaks (native callbacks, UI clicks, Update), which is
    // accepted.
    //
    // For Payment Provider purchases the SDK sends no event after intent-start:
    // the backend sends paid/fulfilled from the order, so the order must carry
    // this same id (see PaymentProviderImpl.InitiateOrder).
    //
    // Scopes must be entered inside an async method, or inside a synchronous
    // method that disposes before returning: an AsyncLocal assigned inside an
    // async method never reaches its caller.
    //
    // A journey ends at its outcome. Outcomes are reported inside EnterOutcome
    // (PurchaseUseCase does this for every store), which keeps the id readable
    // for the outcome's own events but stops EnterOrJoin joining it: a purchase
    // a handler starts — directly or after an await — begins its own journey.
    //
    // Only purchase entry points start or join a journey (EnterOrJoin). Every
    // other public service API that does store or async work runs inside
    // EnterNone, so a caller's id can't reach the outcomes of other transactions
    // it delivers (e.g. a handler's FetchPurchases). A reflection test
    // (ImpressionJourneyClassificationTests) makes every public service method
    // pick one, or be listed as inert.
    internal sealed class ImpressionScope : IImpressionScope
    {
        // The only instance: each instance keeps its own id, so an id set through one
        // instance can't be read through another.
        internal static readonly ImpressionScope Shared = new ImpressionScope();

        readonly AsyncLocal<Journey?> m_Current = new AsyncLocal<Journey?>();

        ImpressionScope() { }

        public string? Current => m_Current.Value?.Id;

        // Starts a new journey with a fresh id, whether or not one is active.
        public IDisposable Enter() => Enter(NewId());

        // Sets id for the scope's duration. Dispose restores the previous value,
        // so nested scopes unwind correctly.
        public IDisposable Enter(string id) => Set(new Journey(id, joinable: true));

        // Joins the active scope if it can be joined (the returned disposable
        // does nothing); otherwise enters a new scope with a freshly minted id.
        public IDisposable EnterOrJoin()
        {
            return m_Current.Value is { Joinable: true } ? NoOp.Instance : Enter();
        }

        // Marks the active journey as reporting its outcome for the scope's
        // duration: Current is unchanged, but EnterOrJoin no longer joins it.
        public IDisposable EnterOutcome()
        {
            var current = m_Current.Value;
            return current is { Joinable: true } ? Set(new Journey(current.Id, joinable: false)) : NoOp.Instance;
        }

        // Runs the scope's duration with no journey: Current is null and EnterOrJoin
        // mints a new id. For public APIs that are never part of a purchase journey,
        // so a caller's id can't reach the work they start.
        public IDisposable EnterNone()
        {
            return m_Current.Value is null ? NoOp.Instance : Set(null);
        }

        IDisposable Set(Journey? journey)
        {
            var previous = m_Current.Value;
            m_Current.Value = journey;
            return new Restore(this, previous);
        }

        static string NewId() => Guid.NewGuid().ToString();

        sealed class Journey
        {
            internal readonly string Id;
            internal readonly bool Joinable;

            internal Journey(string id, bool joinable)
            {
                Id = id;
                Joinable = joinable;
            }
        }

        sealed class Restore : IDisposable
        {
            readonly ImpressionScope m_Scope;
            readonly Journey? m_Previous;
            bool m_Disposed;

            internal Restore(ImpressionScope scope, Journey? previous)
            {
                m_Scope = scope;
                m_Previous = previous;
            }

            public void Dispose()
            {
                if (m_Disposed)
                    return;

                m_Disposed = true;
                m_Scope.m_Current.Value = m_Previous;
            }
        }

        sealed class NoOp : IDisposable
        {
            internal static readonly NoOp Instance = new NoOp();

            public void Dispose() { }
        }
    }
}
