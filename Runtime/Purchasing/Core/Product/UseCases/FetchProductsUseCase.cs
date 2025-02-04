#nullable enable

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Use case that fetched products from its provider
    /// </summary>
    class FetchProductsUseCase : IFetchProductsUseCase, IStoreProductsCallback
    {
        readonly IStore m_Store;
        readonly IRetryService m_RetryService;

        ProductFetchRequest? m_ActiveRequest;
        IRetryRequest? m_ActiveRetryRequest;


        /// <summary>
        /// Create the use case object for a store.
        /// </summary>
        /// <param name="storeResponsible">The store responsible for the products to be retrieved</param>
        [Preserve]
        internal FetchProductsUseCase(IStore storeResponsible, IRetryService retryService)
        {
            m_Store = storeResponsible;
            m_RetryService = retryService;
            m_Store.SetProductsCallback(this);
        }

        public void FetchProducts(List<ProductDefinition>? productDefinitions, Action<List<Product>?> fetchSuccessAction,
            Action<List<ProductDefinition>?, string> fetchFailureAction, IRetryPolicy retryPolicy)
        {
            if (m_ActiveRequest == null)
            {
                if (productDefinitions != null)
                {
                    var readOnlyProducts = new ReadOnlyCollection<ProductDefinition>(productDefinitions);
                    ProcessValidFetchRequest(new ProductFetchRequest(readOnlyProducts, fetchSuccessAction,
                        fetchFailureAction), retryPolicy);
                }
                else
                {
                    fetchFailureAction.Invoke(productDefinitions, "FetchProducts required a valid list of products");
                }
            }
            else
            {
                fetchFailureAction.Invoke(productDefinitions,
                    "FetchProducts cannot be invoked if a fetch is already in progress. Please wait for the previous call to complete");
            }
        }

        void ProcessValidFetchRequest(ProductFetchRequest request, IRetryPolicy retryPolicy)
        {
            m_ActiveRequest = request;
            m_ActiveRetryRequest =
                m_RetryService.CreateRequest(() => m_Store.RetrieveProducts(request.RequestedProducts), retryPolicy);
            m_ActiveRetryRequest.Invoke();
        }

        /// <summary>
        /// Callback received when the call to retrieve products from the store is completed successfully.
        /// </summary>
        /// <param name="products"> The list of product descriptions retrieved. </param>
        public void OnProductsRetrieved(IReadOnlyList<ProductDescription> products)
        {
            if (m_ActiveRequest != null)
            {
                ProcessRetrievedProductsAndInvokeCallbacks(m_ActiveRequest, products);

                m_ActiveRequest = null;
            }
        }

        void ProcessRetrievedProductsAndInvokeCallbacks(ProductFetchRequest request, IReadOnlyList<ProductDescription> productsRetrieved)
        {
            var matchedDefinitions = new List<ProductDefinition>();
            var retrievedProducts = new List<Product>();

            foreach (var description in productsRetrieved)
            {
                var definition = GetMatchingDefinition(description);

                if (definition != null)
                {
                    matchedDefinitions.Add(definition);
                    retrievedProducts.Add(CreateMatchedProduct(definition, description));
                }
            }

            InvokeSuccess(retrievedProducts);
            InvokeFailureIfIncomplete(request, matchedDefinitions);
        }


        ProductDefinition? GetMatchingDefinition(ProductDescription description)
        {
            return m_ActiveRequest?.RequestedProducts.FirstOrDefault(definition =>
                definition.storeSpecificId == description.storeSpecificId);
        }

        Product CreateMatchedProduct(ProductDefinition definition, ProductDescription description)
        {
            var matchedProduct = new Product(definition, description.metadata)
            {
                availableToPurchase = true
            };

            return matchedProduct;
        }

        void InvokeSuccess(List<Product> fetchedProducts)
        {
            m_ActiveRequest?.SuccessAction?.Invoke(fetchedProducts);
        }

        void InvokeFailureIfIncomplete(ProductFetchRequest request, List<ProductDefinition> matchedDefinitions)
        {
            var unretrievedProducts = request.RequestedProducts.Where(def => !matchedDefinitions.Contains(def))
                .ToList();

            if (unretrievedProducts.Any())
            {
                if (matchedDefinitions.Count > 0)
                {
                    request.FailureAction.Invoke(unretrievedProducts,
                        "Retrieve Products succeeded, but could not retrieve the attached subset.");
                }
                else
                {
                    request.FailureAction.Invoke(unretrievedProducts,
                        "Retrieve Products failed, and could not retrieve any products.");
                }
            }
        }

        /// <summary>
        /// Callback received when a RetrieveProducts call could not be completed successfully.
        /// </summary>
        /// <param name="failureDescription"> The reason the fetch failed. </param>
        public async void OnProductsRetrieveFailed(ProductFetchFailureDescription failureDescription)
        {
            if (m_ActiveRequest == null)
            {
                return;
            }

            if (m_ActiveRetryRequest == null)
            {
                SendRequestFailureCallback(m_ActiveRequest, failureDescription);
                return;
            }

            var hasRetried = await m_ActiveRetryRequest.Retry(failureDescription);
            if (!hasRetried)
            {
                SendRequestFailureCallback(m_ActiveRequest, failureDescription);
            }
        }

        void SendRequestFailureCallback(ProductFetchRequest productFetchRequest, ProductFetchFailureDescription failureDescription)
        {
            var message = failureDescription.Message;
            if (string.IsNullOrEmpty(message))
            {
                message = failureDescription.Reason.ToString();
            }

            productFetchRequest.FailureAction.Invoke(productFetchRequest.RequestedProducts.ToList(), message);

            m_ActiveRequest = null;
        }
    }
}
