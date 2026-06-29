#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    class GooglePlayPurchaseUseCase : PurchaseUseCase, IGooglePlayChangeSubscriptionUseCase,
        IGooglePlayChangeSubscriptionCallback
    {
        readonly List<SubscriptionChangeRequest> m_PendingRequests = new();
        IProductCache m_ProductCache;
        public event Action<DeferredPaymentUntilRenewalDateOrder>? OnDeferredPaymentUntilRenewalDate;

        internal GooglePlayPurchaseUseCase(IGooglePlayStore storeResponsible, IProductCache productCache)
            : base(storeResponsible)
        {
            storeResponsible.SetChangeSubscriptionCallback(this);
            m_ProductCache = productCache;
            OnPurchaseFail += OnSubscriptionChangeFailed;
        }

        public void ChangeSubscription(Order currentOrder, CartItem newSubscriptionItem,
            GooglePlayReplacementMode replacementMode)
        {
            var newSubscription = newSubscriptionItem.Product;
            if (!IsSubscriptionChangeValid(currentOrder, newSubscription))
            {
                OnPurchaseFailed(new FailedOrder(new Cart(newSubscriptionItem), PurchaseFailureReason.Unknown,
                    "Invalid SubscriptionProducts requested for purchase. Please pass a valid `SubscriptionProduct` object."));
                return;
            }

            if (FindExistingPurchaseRequest(newSubscription) ||
                ConflictingSubscriptionChangeRequestExists(currentOrder, newSubscription))
            {
                RejectPurchaseDueToPendingDuplicate(newSubscription);
            }
            else
            {
                var subscriptionChangeRequest = new SubscriptionChangeRequest(currentOrder, newSubscriptionItem,
                    replacementMode);
                AddAndSendSubscriptionChangeRequest(subscriptionChangeRequest);
            }
        }

        bool IsSubscriptionChangeValid(Order currentOrder, Product newSubscription)
        {
            var currentProduct = currentOrder.CartOrdered.Items().FirstOrDefault()?.Product;
            if (currentProduct == null)
            {
                return false;
            }

            return IsSubscriptionProductValid(newSubscription) && IsSubscriptionProductValid(currentProduct) &&
                !currentProduct.Equals(newSubscription);
        }

        static bool IsSubscriptionProductValid(Product? subscription)
        {
            return subscription?.baseListing != null;
        }

        bool ConflictingSubscriptionChangeRequestExists(Order currentOrder, Product newSubscription)
        {
            return m_PendingRequests.Exists(request =>
                request.CurrentOrder.Equals(currentOrder) ||
                request.NewSubscription.Equals(newSubscription));
        }

        void AddAndSendSubscriptionChangeRequest(SubscriptionChangeRequest subscriptionChangeRequest)
        {
            m_PendingRequests.Add(subscriptionChangeRequest);

            var cartItem = subscriptionChangeRequest.NewSubscriptionItem;
            var listingDefinition = cartItem.Product.catalogListings.TryGetValue(cartItem.CatalogListingId, out var listing)
                ? listing.definition
                : null;
            GooglePlayStore()?.ChangeSubscription(listingDefinition,
                subscriptionChangeRequest.CurrentOrder, subscriptionChangeRequest.ReplacementMode);
        }

        IGooglePlayStore? GooglePlayStore()
        {
            return m_Store as IGooglePlayStore;
        }

        void OnSubscriptionChangeFailed(FailedOrder order)
        {
            try
            {
                var orderItem = order.CartOrdered.Items().First();
                var storeSpecificId = orderItem.Product.catalogListings.TryGetValue(orderItem.CatalogListingId, out var orderListing) ? orderListing.definition.storeSpecificId : null;
                if (storeSpecificId != null)
                {
                    var request = GetMatchingRequest(storeSpecificId);

                    if (request != null)
                    {
                        m_PendingRequests.Remove(request);
                    }
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public void OnSubscriptionChangeDeferredUntilRenewal(string storeSpecificId)
        {
            try
            {
                var request = GetMatchingRequest(storeSpecificId);
                var pendingPurchase =
                    new DeferredPaymentUntilRenewalDateOrder(request?.CurrentOrder,
                        request?.NewSubscription);

                if (request != null)
                {
                    m_PendingRequests.Remove(request);
                }

                InvokeOnDeferredPaymentUntilRenewalDate(pendingPurchase);
            }
            catch (Exception)
            {
                var product = m_ProductCache.FindOrDefault(storeSpecificId);
                OnPurchaseFailed(new FailedOrder(new Cart(new CartItem(product)), PurchaseFailureReason.Unknown,
                    $"The product with sku id: {storeSpecificId}, was successfully purchased. The request list may be corrupt."));
            }
        }

        internal void InvokeOnDeferredPaymentUntilRenewalDate(DeferredPaymentUntilRenewalDateOrder pendingPurchase)
        {
            OnDeferredPaymentUntilRenewalDate?.Invoke(pendingPurchase);
        }

        public void OnSubscriptionChange(string storeSpecificId)
        {
            try
            {
                var request = GetMatchingRequest(storeSpecificId);

                if (request != null)
                {
                    m_PendingRequests.Remove(request);
                }
            }
            catch (Exception)
            {
                var product = m_ProductCache.FindOrDefault(storeSpecificId);
                OnPurchaseFailed(new FailedOrder(new Cart(new CartItem(product)), PurchaseFailureReason.Unknown,
                    $"The product with sku id: {storeSpecificId}, was successfully purchased. The request list may be corrupt."));
            }
        }

        SubscriptionChangeRequest? GetMatchingRequest(string productId)
        {
            return m_PendingRequests.FirstOrDefault(request =>
            {
                var ci = request.NewSubscriptionItem;
                return ci.Product.catalogListings.TryGetValue(ci.CatalogListingId, out var listing)
                    && listing.definition.storeSpecificId == productId;
            });
        }

        bool FindExistingPurchaseRequest(Product productToCheckFor)
        {
            return m_PendingRequests.Exists(request => request.NewSubscription.Equals(productToCheckFor));
        }

        void RejectPurchaseDueToPendingDuplicate(Product product)
        {
            var cart = new Cart(new CartItem(product));
            var failedOrder = new FailedOrder(cart, PurchaseFailureReason.DuplicateTransaction,
                "Cannot attempt to purchase a Product that has an existing pending purchase request");
            OnPurchaseFailed(failedOrder);
        }
    }
}
