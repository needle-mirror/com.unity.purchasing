#nullable enable

using System;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    interface ISetPromotionalPurchaseInterceptorCallbackUseCase
    {
        event Action<Product> OnPromotionalPurchaseIntercepted;
    }
}
