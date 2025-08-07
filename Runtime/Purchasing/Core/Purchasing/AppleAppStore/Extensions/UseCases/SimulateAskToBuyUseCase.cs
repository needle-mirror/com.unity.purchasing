#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class SimulateAskToBuyUseCase : ISimulateAskToBuyUseCase
    {
        readonly IAppleStoreCallbacks m_AppleStoreCallbacks;

        [Preserve]
        internal SimulateAskToBuyUseCase(IAppleStoreCallbacks appleStoreCallbacks)
        {
            m_AppleStoreCallbacks = appleStoreCallbacks;
        }

        public bool SimulateAskToBuy()
        {
            return m_AppleStoreCallbacks.simulateAskToBuy;
        }

        public void SetSimulateAskToBuy(bool value)
        {
            m_AppleStoreCallbacks.simulateAskToBuy = value;
        }
    }
}
