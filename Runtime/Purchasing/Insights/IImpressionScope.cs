#nullable enable

using System;

namespace UnityEngine.Purchasing
{
    // Lets classes that put the impression_id into their requests take it as a
    // dependency. The only implementation is ImpressionScope.Shared.
    internal interface IImpressionScope
    {
        string? Current { get; }
        IDisposable Enter(string id);
        IDisposable EnterOrJoin();
        IDisposable EnterOutcome();
        IDisposable EnterNone();
    }
}
