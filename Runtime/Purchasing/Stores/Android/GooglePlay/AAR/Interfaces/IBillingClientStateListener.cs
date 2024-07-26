using System;
using UnityEngine.Purchasing.Models;

namespace UnityEngine.Purchasing.Interfaces
{
    public interface IBillingClientStateListener
    {
        void RegisterOnConnected(Action onConnected);
        void RegisterOnDisconnected(Action<GoogleBillingResponseCode> onDisconnected);
    }
}
