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

            var productId = product.definition?.id ?? "unknown";
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
            var request = new CheckEntitlementRequest(product, onCheckComplete);
            m_OngoingRequests.Add(request);

            try
            {
                m_Store.CheckEntitlement(product.definition);
            }
            catch (Exception e)
            {
                m_OngoingRequests.Remove(request);
                var fallbackEntitlement = new Entitlement(
                    product: product,
                    order: null,
                    status: EntitlementStatus.Unknown,
                    message: $"Exception during CheckEntitlement: {e.Message}"
                );
                onCheckComplete?.Invoke(fallbackEntitlement);
            }
        }

        public void OnCheckEntitlement(ProductDefinition productDefinition, EntitlementStatus status,
            string message = null)
        {
            var matchingRequest = GetMatchingRequest(productDefinition);
            if (matchingRequest != null)
            {
                var entitlement = new Entitlement(matchingRequest.ProductToCheck, null, status);
                matchingRequest.OnChecked?.Invoke(entitlement);
                m_OngoingRequests.Remove(matchingRequest);
            }
            else
            {
                Debug.unityLogger.LogIAPWarning($"[CheckEntitlement] Missing request for productDefinition: {productDefinition.storeSpecificId}. " +
                                                $"Callback will not be invoked. Status: {status}. Message: {message ?? "none"}");
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
