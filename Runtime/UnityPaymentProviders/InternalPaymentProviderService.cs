using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Services.Authentication.Internal;
using Unity.Services.Core.Configuration.Internal;
using UnityEngine.Purchasing.PaymentProviders;
using UnityEngine.Purchasing.PaymentProviderService.Apis.PaymentProvider;
using UnityEngine.Purchasing.PaymentProviderService.Http;
using UnityEngine.Purchasing.PaymentProviderService.Models;
using UnityEngine.Purchasing.PaymentProviderService.PaymentProvider;

namespace UnityEngine.Purchasing.PaymentProviderService
{
    internal class InternalPaymentProviderService : IPaymentProviderService
    {
        private IPaymentProviderServiceExceptionMapper m_ServiceExceptionMapper;
        internal InternalPaymentProviderService(IAccessToken accessToken, IEnvironmentId environmentId, ICloudProjectId cloudProjectId, string baseUrl = null)
        {
            var url = baseUrl ?? "https://iap.services.api.unity.com";
            PaymentProviderApiClient = new PaymentProviderApiClient(new HttpClient(), accessToken);
            Configuration = new Configuration(url, 10, 4, null);
            Configuration.Headers.Add("Unity-IAP-Package-Version", IAPVersion.Current);

            m_EnvironmentId = environmentId;
            m_CloudProjectId = cloudProjectId;

            m_ServiceExceptionMapper = new PaymentProviderServiceExceptionMapper();
        }

        IEnvironmentId m_EnvironmentId { get; }

        ICloudProjectId m_CloudProjectId { get; }

        /// <summary> Instance of IDefaultApiClient interface</summary>
        IPaymentProviderApiClient PaymentProviderApiClient { get; }

        /// <summary> Configuration properties for the service.</summary>
        Configuration Configuration { get; }

        public async Task<OrderData> GetUrl(string catalogListingId, string displayName, string locale, string currencyCode,
            string country, PlayerIdentity playerIdentity, string paymentProviderOverride, DeviceInfo deviceInfo, IReadOnlyList<PaymentProviderToken> paymentProviderTokens)
        {
            CheckForCloudProjectInfo();

            var request = new InitiatePaymentProviderOrderRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId,
                new OrderRequest(
                    new Player(
                        locale,
                        playerIdentity,
                        null, // TODO: Remove PlayerID from Player object in spec file.
                        displayName
                        ),
                    currencyCode,
                    null,
                    new List<string> { catalogListingId },
                    country,
                    paymentProviderOverride,
                    MapExternalTransactionTokens(paymentProviderTokens),
                    deviceInfo
                )
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response =
                    await PaymentProviderApiClient.InitiatePaymentProviderOrderAsync(request, Configuration);
                return CreateOrderDataFromResponse(response.Result);
            });
        }

        private static List<OrderRequestExternalTransactionTokensInner> MapExternalTransactionTokens(IReadOnlyList<PaymentProviderToken> paymentProviderTokens)
        {
            if (paymentProviderTokens == null || paymentProviderTokens.Count == 0)
            {
                return null;
            }

            var externalTokens = new List<OrderRequestExternalTransactionTokensInner>(paymentProviderTokens.Count);
            foreach (var token in paymentProviderTokens)
            {
                externalTokens.Add(new OrderRequestExternalTransactionTokensInner(
                    store: token.Store switch
                    {
                        PaymentProviderTokenStore.Apple => OrderRequestExternalTransactionTokensInner.StoreOptions.Apple,
                        PaymentProviderTokenStore.Google => OrderRequestExternalTransactionTokensInner.StoreOptions.Google,
                        _ => throw new ArgumentException($"Unknown store in PaymentProviderToken: {token.Store}")
                    },
                    token: token.Token,
                    type: token.Type switch
                    {
                        ExternalPurchaseTokenType.Acquisition => OrderRequestExternalTransactionTokensInner.TypeOptions.Acquisition,
                        ExternalPurchaseTokenType.Services => OrderRequestExternalTransactionTokensInner.TypeOptions.Services,
                        ExternalPurchaseTokenType.LinkOut => OrderRequestExternalTransactionTokensInner.TypeOptions.LinkOut,
                        // Google tokens have no type; default/0 causes the field to be omitted from the serialized payload.
                        _ => default
                    }
                ));
            }

            return externalTokens;
        }

        public async Task<List<ProductData>> GetProducts(List<string> skus, string locale, string currencyCode)
        {
            CheckForCloudProjectInfo();
            var request = new ListPaymentProvidersProductsRequest(
                m_CloudProjectId.GetCloudProjectId(),
                locale,
                currencyCode,
                skus
                );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.ListPaymentProvidersProductsAsync(request, Configuration);
                return CreateProductDataFromResponse(response.Result);
            });
        }

        public async Task<List<CatalogProductData>> GetCatalog(List<string> stores)
        {
            CheckForCloudProjectInfo();
            var request = new ListSKUsRequest(
                m_CloudProjectId.GetCloudProjectId(),
                stores
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.ListSKUsAsync(request, Configuration);
                return GetCatalogProductsFromResponse(response.Result);
            });
        }

        private void CheckForCloudProjectInfo()
        {
            if (m_EnvironmentId?.EnvironmentId is null ||
                m_CloudProjectId?.GetCloudProjectId() is null
               )
            {
                throw new CloudProjectAuthenticationException();
            }
        }

        private List<ProductData> CreateProductDataFromResponse(List<ProductResponse> results)
        {
            try
            {
                return results.Select(result => new ProductData()
                    {
                        catalogListingId = result.CatalogListingId,
                        unitySku = result.USku,
                        currency = result.Pricing.Currency,
                        priceInMicros = result.Pricing.Price,
                        title = result.ProductDetails.Title,
                        description = result.ProductDetails.Description,
                        priceString = result.Pricing.LocalizedPriceString,
                        language = result.ProductDetails.Language
                    }
                ).ToList();
            }
            catch (Exception e)
            {
                throw new ResponseDeserializationException($"Error deserializing ProductData from response: {e.Message}");
            }
        }

        private List<CatalogProductData> GetCatalogProductsFromResponse(List<SkuResponse> results)
        {
            try
            {
                return results.Select(data => new CatalogProductData()
                {
                    catalogListingId = data.CatalogListingId,
                    productType = CatalogProductTypeFromString(data.Type),
                    unitySku = data.USku,
                    storeOverrides = data.SkuOverrides != null ? data.SkuOverrides.Select(storeOverride =>
                        new CatalogStoreOverride()
                        {
                            storeName = storeOverride.Store, skuOverride = storeOverride.Sku
                        }
                    ).ToList() : new List<CatalogStoreOverride>()
                }).ToList();
            }
            catch (Exception e)
            {
                throw new ResponseDeserializationException($"Error deserializing CatalogProductData from response: {e.Message}");
            }
        }

        public async Task<OrderData> UpdateOrder(string orderId, UpdateOrderStatus status)
        {
            CheckForCloudProjectInfo();
            var request = new PaymentProvider.UpdateOrderRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId,
                orderId,
                new Models.UpdateOrderRequest(
                    status switch
                    {
                        UpdateOrderStatus.Fulfilled => Models.UpdateOrderRequest.StatusOptions.Fulfilled,
                        UpdateOrderStatus.Cancelled => Models.UpdateOrderRequest.StatusOptions.Cancelled,
                        _ => throw new ArgumentException("Unknown UpdateOrderStatus") // this absolutely should not happen
                    }
                )
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.UpdateOrderAsync(request, Configuration);
                return CreateOrderDataFromResponse(response.Result);
            });
        }

        public async Task<OrderData> GetOrder(string orderId)
        {
            CheckForCloudProjectInfo();
            var request = new GetOrderRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId,
                orderId
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.GetOrderAsync(request, Configuration);
                return CreateOrderDataFromResponse(response.Result);
            });
        }

        public async Task<List<OrderData>> GetEntitledOrders()
        {
            CheckForCloudProjectInfo();
            var request = new ListEntitledRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.ListEntitledAsync(request, Configuration);
                return CreateOrderDataListFromResponse(response.Result);
            });
        }

        public async Task<(List<string> Providers, bool PaymentOptionPopupEnabled)> GetEligiblePaymentProviders()
        {
            CheckForCloudProjectInfo();
            var request = new ListPaymentProvidersRequest(
                m_CloudProjectId.GetCloudProjectId(),
                m_EnvironmentId.EnvironmentId
            );

            return await m_ServiceExceptionMapper.InvokeAndMapServiceExceptions(async () =>
            {
                var response = await PaymentProviderApiClient.ListPaymentProvidersAsync(request, Configuration);
                // Missing paymentOptionsPopupEnabled defaults to true so older backends and rollbacks keep the popup on.
                return (response.Result.Providers, response.Result.PaymentOptionsPopupEnabled ?? true);
            });
        }

        private OrderData CreateOrderDataFromResponse(OrderResponse orderResponse)
        {
            try
            {
                return new OrderData()
                {
                    id = orderResponse.Id,
                    projectId = orderResponse.ProjectId,
                    environmentId = orderResponse.EnvironmentId,
                    playerId = orderResponse.PlayerId,
                    paymentProvider = orderResponse.PaymentProvider,
                    paymentProviderResourceId = orderResponse.PaymentProviderResourceId,
                    paymentProviderUrl = orderResponse.Url,
                    lineItems = orderResponse.LineItems.Select(lineItemResponse => new LineItem()
                        {
                            unitySku = lineItemResponse.Sku,
                            productType = lineItemResponse.ProductType,
                        }).ToList(),
                    status = OrderStatusFromString(orderResponse.Status),
                    fulfilledAt = orderResponse.FulfilledAt,
                    revokedAt = orderResponse.RevokedAt,
                    customReferenceId = orderResponse.CustomReferenceId,
                    metadata = orderResponse.Metadata,
                    createdAt = orderResponse.CreatedAt,
                    updatedAt = orderResponse.UpdatedAt
                };

            }
            catch (Exception e)
            {
                throw new ResponseDeserializationException($"Error deserializing Order from response: {e.Message}");
            }
        }

        private List<OrderData> CreateOrderDataListFromResponse(List<OrderResponse> results)
        {
            if (results.Count == 0)
            {
                return new List<OrderData>();
            }

            var orders = new List<OrderData>();

            foreach (var result in results)
            {
                try
                {
                    var order = CreateOrderDataFromResponse(result);
                    orders.Add(order);
                }
                catch (Exception e)
                {
                    Debug.LogError($"Failed to deserialize order for id {result.Id}: {e.Message}");
                }
            }

            if (orders.Count == 0)
            {
                throw new ResponseDeserializationException($"Failed to deserialize all Orders from response with count {results.Count}.");
            }

            return orders;
        }

        internal static CatalogProductType CatalogProductTypeFromString(string type)
        {
            return type switch
            {
                "Consumable" => CatalogProductType.Consumable,
                "NonConsumable" => CatalogProductType.NonConsumable,
                "non-consumable" => CatalogProductType.NonConsumable, //Temporary until Catalog is fixed
                "Subscription" => CatalogProductType.Subscription,
                _ => CatalogProductType.Unknown
            };
        }

        internal static OrderStatus OrderStatusFromString(string orderStatusString)
        {
            return orderStatusString switch
            {
                "created" => OrderStatus.Created,
                "cancelled" => OrderStatus.Cancelled,
                "paid" => OrderStatus.Paid,
                "fulfilled" => OrderStatus.Fulfilled,
                "failed" => OrderStatus.Failed,
                "revoked" => OrderStatus.Revoked,
                null => OrderStatus.Unknown,
                _ => OrderStatus.Unknown
            };
        }
    }
}
