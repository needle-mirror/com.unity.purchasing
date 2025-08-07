#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// A public interface for a class that acts out the use case of continuing promotional purchases.
    /// </summary>
    interface IContinuePromotionalPurchasesUseCase
    {
        /// <summary>
        /// Continue promotional purchases that were intercepted by SetPromotionalPurchaseInterceptorCallback
        /// </summary>
        void ContinuePromotionalPurchases();
    }
}
