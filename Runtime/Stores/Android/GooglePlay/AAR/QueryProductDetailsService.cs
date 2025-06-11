#nullable enable

using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.Purchasing.Exceptions;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Purchasing.Models;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing
{
    class QueryProductDetailsService : IQueryProductDetailsService
    {
        readonly IGoogleBillingClient m_BillingClient;
        readonly IGoogleCachedQueryProductDetailsService m_GoogleCachedQueryProductDetailsService;
        readonly IProductDetailsConverter m_ProductDetailsConverter;

        [Preserve]
        internal QueryProductDetailsService(IGoogleBillingClient billingClient,
            IGoogleCachedQueryProductDetailsService googleCachedQueryProductDetailsService,
            IProductDetailsConverter productDetailsConverter)
        {
            m_BillingClient = billingClient;
            m_GoogleCachedQueryProductDetailsService = googleCachedQueryProductDetailsService;
            m_ProductDetailsConverter = productDetailsConverter;
        }

        public Task<List<AndroidJavaObject>> QueryProductDetails(ProductDefinition product)
        {
            return QueryProductDetails(new List<ProductDefinition>
            {
                product
            }.AsReadOnly());
        }

        public async Task<List<ProductDescription>> QueryProductDescriptions(IReadOnlyCollection<ProductDefinition> products)
        {
            var productDetails = await QueryProductDetails(products);
            return m_ProductDetailsConverter.ConvertOnQueryProductDetailsResponse(productDetails, products);
        }

        public virtual async Task<List<AndroidJavaObject>> QueryProductDetails(IReadOnlyCollection<ProductDefinition> products)
        {
            var responses = await QueryInAppsAndSubsProductDetails(products);

            m_GoogleCachedQueryProductDetailsService.AddCachedQueriedProductDetails(responses.ProductDetails());
            if (ShouldRetryQuery(products, responses))
            {
                var billingResponse = responses.GetRecoverableBillingResponseCode();
                var description = new ProductFetchFailureDescription(ProductFetchFailureReason.ProviderUnavailable,
                    $"Could not retrieve all product details. GoogleBillingResponseCode : {billingResponse}", true);
                throw new GoogleFetchProductException(GoogleFetchProductsFailureReason.Unknown, billingResponse, description);
            }

            return GetCachedProductDetails(products).ToList();
        }

        async Task<ProductDetailsQueryResponse> QueryInAppsAndSubsProductDetails(IReadOnlyCollection<ProductDefinition> products)
        {
            var tasks = new List<Task<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)>>()
            {
                QueryInAppsAsync(products),
                QuerySubsAsync(products)
            };
            await Task.WhenAll(tasks);

            var responses = new ProductDetailsQueryResponse();

            foreach (var task in tasks)
            {
                responses.AddResponse(task.Result.Item1, task.Result.Item2);
            }

            return responses;
        }

        bool ShouldRetryQuery(IEnumerable<ProductDefinition> requestedProducts, IProductDetailsQueryResponse queryResponse)
        {
            return !AreAllProductDetailsCached(requestedProducts) && queryResponse.IsRecoverable();
        }

        bool AreAllProductDetailsCached(IEnumerable<ProductDefinition> products)
        {
            return products.Select(m_GoogleCachedQueryProductDetailsService.Contains).All(isCached => isCached);
        }

        IEnumerable<AndroidJavaObject> GetCachedProductDetails(IEnumerable<ProductDefinition> products)
        {
            var cachedProducts = products.Where(m_GoogleCachedQueryProductDetailsService.Contains).ToList();
            return m_GoogleCachedQueryProductDetailsService.GetCachedQueriedProductDetails(cachedProducts);
        }

        Task<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)> QueryInAppsAsync(
            IEnumerable<ProductDefinition> products)
        {
            var productList = products
                .Where(product => product.type != ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            return QueryProductDetails(productList, GoogleProductTypeEnum.InApp());
        }

        Task<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)> QuerySubsAsync(
            IEnumerable<ProductDefinition> products)
        {
            var productList = products
                .Where(product => product.type == ProductType.Subscription)
                .Select(product => product.storeSpecificId)
                .ToList();
            return QueryProductDetails(productList, GoogleProductTypeEnum.Sub());
        }

        Task<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)> QueryProductDetails(List<string> productList, string type)
        {
            var taskCompletionSource = new TaskCompletionSource<(IGoogleBillingResult, IEnumerable<AndroidJavaObject>)>();

            if (productList.Count == 0)
            {
                taskCompletionSource.SetResult((new GoogleBillingResult(null), new List<AndroidJavaObject>()));
            }
            else
            {
                m_BillingClient.QueryProductDetailsAsync(productList, type,
                    (billingResult, productDetails) => taskCompletionSource.SetResult((billingResult, productDetails)));
            }

            return taskCompletionSource.Task;
        }
    }
}
