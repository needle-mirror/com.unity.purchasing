using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class CheckEntitlementUseCase : ICheckEntitlementUseCase, IStoreCheckEntitlementCallback
    {
        readonly IStore m_Store;
        readonly List<CheckEntitlementRequest> m_OngoingRequests = new List<CheckEntitlementRequest>();

        [Preserve]
        internal CheckEntitlementUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetEntitlementCheckCallback(this);
        }

        public void IsProductEntitled(Product product, Action<Entitlement> onCheckComplete)
        {
            if (product == null)
            {
                throw new CheckEntitlementException("Invalid Product requested for entitlement check. Please pass a valid `Product` object.");
            }

            if (FindExistingEntitlementRequest(product))
            {
                throw new CheckEntitlementException("Duplicate product requested for entitlement. No callbacks will be sent for this call. Please refrain from passing the same `Product` multiple times.");
            }
            else
            {
                AddAndSendCheckEntitlementRequest(product, onCheckComplete);
            }
        }

        bool FindExistingEntitlementRequest(Product productToCheckFor)
        {
            return m_OngoingRequests.Exists(request => request.ProductToCheck.Equals(productToCheckFor));
        }

        void AddAndSendCheckEntitlementRequest(Product product, Action<Entitlement> checkCompleteAction)
        {
            m_OngoingRequests.Add(new CheckEntitlementRequest(product, checkCompleteAction));

            m_Store.CheckEntitlement(product.definition);
        }

        public void OnCheckEntitlementSucceeded(ProductDefinition productDefinition, EntitlementStatus status)
        {
            var matchingRequest = GetMatchingRequest(productDefinition);

            if (matchingRequest != null)
            {
                var cart = new Cart(matchingRequest.ProductToCheck);
                var orderInfo = new OrderInfo(String.Empty, String.Empty, String.Empty);

                Order order = null;
                if (status == EntitlementStatus.FullyEntitled)
                {
                    order = new ConfirmedOrder(cart, orderInfo);
                }
                else if (status == EntitlementStatus.EntitledUntilConsumed || status == EntitlementStatus.EntitledButNotFinished)
                {
                    order = new PendingOrder(cart, orderInfo);
                }

                var entitlement = new Entitlement(matchingRequest.ProductToCheck, order, status);

                matchingRequest.OnChecked?.Invoke(entitlement);

                m_OngoingRequests.Remove(matchingRequest);
            }
            else
            {
                throw new ConfirmOrderException($"Cannot find matching confirmation request for Product SKU: {productDefinition.storeSpecificId}. The List of orders may have become corrupt. No callbacks will be sent for this call. Entitlement status would be {status}.");
            }
        }

        CheckEntitlementRequest GetMatchingRequest(ProductDefinition productDefinition)
        {
            return m_OngoingRequests.FirstOrDefault(request =>
                request.ProductToCheck.definition.storeSpecificId
                    .Equals(productDefinition.storeSpecificId));
        }
    }
}
