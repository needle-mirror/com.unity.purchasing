using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace Samples.Purchasing.IAP5.Demo
{
    public class IAPPaywallCallbacks
    {
        PaywallManager m_PaywallManager;
        public IAPPaywallCallbacks(PaywallManager paywallManager)
        {
            m_PaywallManager = paywallManager;
        }
        public void OnInitialProductsFetched(List<Product> products)
        {
            m_PaywallManager.m_IAPLogger.LogConsole("===========");
            m_PaywallManager.m_IAPLogger.LogConsole("OnInitialProductsFetched:");
            m_PaywallManager.m_IAPLogger.LogFetchedProducts(products);
            m_PaywallManager.UpdateActivePurchaseButtons();
            m_PaywallManager.FetchExistingPurchases();
        }
        public void OnInitialProductsFetchFailed(ProductFetchFailed failure)
        {
            m_PaywallManager.m_IAPLogger.LogConsole("===========");
            m_PaywallManager.m_IAPLogger.LogConsole("OnInitialProductsFetchFailed:");
            m_PaywallManager.m_IAPLogger.LogConsole(failure.FailureReason);
        }
        public void OnExistingPurchasesFetched(Orders existingOrders)
        {
            m_PaywallManager.m_IAPLogger.LogConsole("===========");
            m_PaywallManager.m_IAPLogger.LogConsole("OnExistingPurchasesFetched:");
            m_PaywallManager.m_IAPLogger.LogConsole(PaywallManager.IsReceiptAvailable(existingOrders) ? "Success - Found Existing Orders with receipts" : "Notice: - No Existing Orders with receipts");
        }
        public void OnExistingPurchasesFetchFailed(PurchasesFetchFailureDescription failure)
        {
            m_PaywallManager.m_IAPLogger.LogConsole("===========");
            m_PaywallManager.m_IAPLogger.LogConsole("OnExistingPurchasesFetchFailed:");
            m_PaywallManager.m_IAPLogger.LogConsole(failure.Message);
        }
        public void OnPurchasePending(PendingOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;

                m_PaywallManager.m_IAPLogger.LogCompletedPurchase(product, order.Info);
                m_PaywallManager.ValidatePurchaseIfPossible(order.Info);
            }

            m_PaywallManager.ConfirmOrderIfAutomatic(order);
        }
        public void OnPurchaseConfirmed(Order order)
        {
            switch (order)
            {
                case FailedOrder failedOrder:
                    OnConfirmationFailed(failedOrder);
                    break;
                case ConfirmedOrder confirmedOrder:
                    OnPurchaseConfirmed(confirmedOrder);
                    break;
            }
        }

        void OnConfirmationFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;

            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                m_PaywallManager.m_IAPLogger.LogFailedConfirmation(cartItem.Product, reason);
            }
        }

        public void OnPurchaseConfirmed(ConfirmedOrder order)
        {
            foreach (var cartItem in order.CartOrdered.Items())
            {
                var product = cartItem.Product;

                m_PaywallManager.m_IAPLogger.LogConfirmedOrder(product, order.Info);
            }
        }
        public void OnPurchaseFailed(FailedOrder failedOrder)
        {
            var reason = failedOrder.FailureReason;

            foreach (var cartItem in failedOrder.CartOrdered.Items())
            {
                m_PaywallManager.m_IAPLogger.LogFailedPurchase(cartItem.Product, reason);
            }
        }
        public void OnOrderDeferred(DeferredOrder deferredOrder)
        {
            foreach (var cartItem in deferredOrder.CartOrdered.Items())
            {
                m_PaywallManager.m_IAPLogger.LogDeferredPurchase(cartItem.Product);
            }
        }
    }
}
