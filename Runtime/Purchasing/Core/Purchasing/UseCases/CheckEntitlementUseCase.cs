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
        readonly List<CheckEntitlementRequest> m_OngoingRequests = new();

        [Preserve]
        internal CheckEntitlementUseCase(IStore storeResponsible)
        {
            m_Store = storeResponsible;
            m_Store.SetEntitlementCheckCallback(this);
        }

        public void IsProductEntitled(Product product, Action<Entitlement> onResult)
        {
            if (product == null)
            {
                var entitlement = new Entitlement(null, null, EntitlementStatus.Unknown, "Invalid product: null");
                onResult?.Invoke(entitlement);
                return;
            }

            var productId = product.uSku ?? "unknown";
            if (FindExistingEntitlementRequest(product))
            {
                var entitlement = new Entitlement(
                    product: null,
                    order: null,
                    status: EntitlementStatus.Unknown,
                    message: $"Duplicate CheckEntitlement request detected for product id: {productId}");
                onResult?.Invoke(entitlement);
                return;
            }

            AddAndSendCheckEntitlementRequest(product, onResult);
        }

        bool FindExistingEntitlementRequest(Product productToCheckFor)
        {
            return m_OngoingRequests.Exists(request => request.ProductToCheck.Equals(productToCheckFor));
        }

        void AddAndSendCheckEntitlementRequest(Product product, Action<Entitlement> onCheckComplete)
        {
            if (product.catalogListings.Count == 0)
            {
                var fallback = new Entitlement(product, null, EntitlementStatus.Unknown,
                    $"No catalog listings available for product id: {product.uSku ?? "unknown"}");
                onCheckComplete?.Invoke(fallback);
                return;
            }

            var request = new CheckEntitlementRequest(product, onCheckComplete)
            {
                RemainingListings = product.catalogListings.Count
            };
            m_OngoingRequests.Add(request);

            foreach (var catalogListing in product.catalogListings.Values)
            {
                try
                {
                    m_Store.CheckEntitlement(catalogListing.definition);
                }
                catch (Exception e)
                {
                    OnCheckEntitlement(catalogListing.definition, EntitlementStatus.Unknown,
                        $"Exception during CheckEntitlement: {e.Message}");
                }
            }
        }

        public void OnCheckEntitlement(ProductDefinition productDefinition, EntitlementStatus status,
            string message = null)
        {
            var matchingRequest = GetMatchingRequest(productDefinition);
            if (matchingRequest == null)
            {
                Debug.unityLogger.LogIAPWarning($"[CheckEntitlement] Missing request for product id: {productDefinition.id}. " +
                                                $"Callback will not be invoked. Status: {status}. Message: {message ?? "none"}");
                return;
            }

            if (Priority(status) > Priority(matchingRequest.BestStatus))
            {
                matchingRequest.BestStatus = status;
            }
            if (!string.IsNullOrEmpty(message))
            {
                matchingRequest.LastMessage = message;
            }
            matchingRequest.RemainingListings--;

            if (matchingRequest.RemainingListings > 0)
            {
                return;
            }

            m_OngoingRequests.Remove(matchingRequest);
            var entitlement = new Entitlement(
                matchingRequest.ProductToCheck, null, matchingRequest.BestStatus, matchingRequest.LastMessage);
            matchingRequest.OnChecked?.Invoke(entitlement);
        }

        CheckEntitlementRequest GetMatchingRequest(ProductDefinition productDefinition)
        {
            return m_OngoingRequests.FirstOrDefault(request =>
                request.ProductToCheck.uSku == productDefinition.id);
        }

        static int Priority(EntitlementStatus status) => status switch
        {
            EntitlementStatus.FullyEntitled => 4,
            EntitlementStatus.EntitledButNotFinished => 3,
            EntitlementStatus.EntitledUntilConsumed => 2,
            EntitlementStatus.NotEntitled => 1,
            EntitlementStatus.Unknown => 0,
            _ => 0,
        };
    }
}
