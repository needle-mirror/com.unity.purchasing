#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace UnityEngine.Purchasing
{
    internal class GooglePlayPurchaseUseCase : PurchaseUseCase, IGooglePlayChangeSubscriptionUseCase,
        IGooglePlayChangeSubscriptionCallback
    {
        readonly List<SubscriptionChangeRequest> m_PendingRequests = new List<SubscriptionChangeRequest>();
        public event Action<DeferredPaymentUntilRenewalDateOrder>? OnPurchaseDeferredUntilRenewalAction;

        internal GooglePlayPurchaseUseCase(IGooglePlayStore storeResponsible)
            : base(storeResponsible)
        {
            storeResponsible.SetChangeSubscriptionCallback(this);
        }

        public void ChangeSubscription(Product previousSubscription, Product newSubscription,
            GooglePlayProrationMode prorationMode)
        {
            if (!IsSubscriptionChangeValid(previousSubscription, newSubscription))
            {
                throw new PurchaseException(
                    "Invalid SubscriptionProducts requested for purchase. No callbacks will be sent for this call. Please pass a valid `SubscriptionProduct` object.");
            }

            if (FindExistingPurchaseRequest(newSubscription) ||
                ConflictingSubscriptionChangeRequestExists(previousSubscription, newSubscription))
            {
                RejectPurchaseDueToPendingDuplicate(newSubscription);
            }
            else
            {
                var subscriptionChangeRequest = new SubscriptionChangeRequest(previousSubscription, newSubscription,
                    prorationMode);
                AddAndSendSubscriptionChangeRequest(subscriptionChangeRequest);
            }

            GooglePlayStore()?.ChangeSubscription(newSubscription.definition,
                previousSubscription, prorationMode);
        }

        bool IsSubscriptionChangeValid(Product previousSubscription, Product newSubscription)
        {
            return IsSubscriptionProductValid(newSubscription) && IsSubscriptionProductValid(previousSubscription) &&
                !previousSubscription.Equals(newSubscription);
        }

        bool IsSubscriptionProductValid(Product? subscription)
        {
            return subscription?.definition != null;
        }

        bool ConflictingSubscriptionChangeRequestExists(Product previousSubscription, Product newSubscription)
        {
            return m_PendingRequests.Exists(request =>
                request.PreviousSubscription.Equals(previousSubscription) ||
                request.NewSubscription.Equals(newSubscription));
        }

        private void AddAndSendSubscriptionChangeRequest(SubscriptionChangeRequest subscriptionChangeRequest)
        {
            m_PendingRequests.Add(subscriptionChangeRequest);

            GooglePlayStore()?.ChangeSubscription(subscriptionChangeRequest.NewSubscription.definition,
                subscriptionChangeRequest.PreviousSubscription, subscriptionChangeRequest.ProrationMode);
        }

        private IGooglePlayStore? GooglePlayStore()
        {
            return m_Store as IGooglePlayStore;
        }

        public void OnSubscriptionChangeDeferredUntilRenewal(string storeSpecificId)
        {
            try
            {
                HandleSubscriptionChangeDeferredUntilRenewal(storeSpecificId);
            }
            catch (InvalidOperationException)
            {
                ThrowUnknownProductException(storeSpecificId);
            }
        }

        void HandleSubscriptionChangeDeferredUntilRenewal(string storeSpecificId)
        {
            try
            {
                var request = GetMatchingRequest(storeSpecificId);
                var pendingPurchase =
                    new DeferredPaymentUntilRenewalDateOrder(request?.PreviousSubscription,
                        request?.NewSubscription);

                if (request != null)
                {
                    m_PendingRequests.Remove(request);
                }

                OnPurchaseDeferredUntilRenewalAction?.Invoke(pendingPurchase);
            }
            catch (InvalidOperationException)
            {
                throw new PurchaseException(
                    $"The product with sku id: {storeSpecificId} was successfully purchased. The request list may be corrupt. No callbacks will be sent for this call.");
            }
        }

        SubscriptionChangeRequest? GetMatchingRequest(string productId)
        {
            return m_PendingRequests.FirstOrDefault(request =>
                request.NewSubscription.definition.storeSpecificId == productId);
        }

        bool FindExistingPurchaseRequest(Product productToCheckFor)
        {
            return m_PendingRequests.Exists(request => request.NewSubscription.Equals(productToCheckFor));
        }

        static void ThrowUnknownProductException(string storeSpecificId)
        {
            throw new PurchaseException(
                $"An unknown Product, sku id: {storeSpecificId}, was successfully purchased. The request list may be corrupt. No callbacks will be sent for this call.");
        }

        void RejectPurchaseDueToPendingDuplicate(Product product)
        {
            var cart = new Cart(new CartItem(product));
            var failedOrder = new FailedOrder(cart, PurchaseFailureReason.Unknown,
                "Cannot Attempt to purchase a Product that has an existing pending purchase request");

            OnPurchaseFailed(failedOrder);
        }
    }
}
