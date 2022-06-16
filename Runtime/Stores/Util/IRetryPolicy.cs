#nullable enable

using System;
namespace UnityEngine.Purchasing.Stores.Util
{
    interface IRetryPolicy
    {
        void Invoke(Action<Action> actionToTry, Action? onRetryAction = null);
    }
}
