#nullable enable

using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Purchasing.Extension;
using Uniject;
using UnityEngine.Purchasing.CatalogListings;
using UnityEngine.Purchasing.Extension;
using UnityEngine.Purchasing.LiveContentAdapterService;
using UnityEngine.Purchasing.PaymentProviders;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.WebshopService;
using LiveContentAdapterCloudError = UnityEngine.Purchasing.LiveContentAdapterService.CloudProjectAuthenticationException;
using UnityEngine.Purchasing.PaymentProviderService.Models;
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Scripting;
using BadRequestError = UnityEngine.Purchasing.PaymentProviderService.BadRequestError;
using CloudProjectAuthenticationException = UnityEngine.Purchasing.PaymentProviderService.CloudProjectAuthenticationException;
using ConflictError = UnityEngine.Purchasing.PaymentProviderService.ConflictError;
using NetworkError = UnityEngine.Purchasing.PaymentProviderService.NetworkError;
using ServiceUnavailableError = UnityEngine.Purchasing.PaymentProviderService.ServiceUnavailableError;

namespace UnityEngine.Purchasing.Stores
{
    [Preserve]
    internal class PaymentProviderImpl : InternalStore, IPaymentProvidersExtendedService, IPaymentProviderCallbacks
    {
        protected IPaymentProviderClientWrapper m_PaymentProviderClientWrapper;
        protected IWebshopClientWrapper m_WebshopClientWrapper;
        protected ICatalogListingClient m_CatalogListingClient;
        readonly ICurrencyFormatter m_CurrencyFormatter;
        IPlayerData m_PlayerData;
        string? m_PaymentProviderOverride;
        protected Func<PaymentProviderComplianceContext, Task<bool>>? m_ComplianceCheck;
        INativeAppleStore? m_NativeStore;
        DeviceInfo? m_DeviceInfo;
        readonly ConcurrentDictionary<string, Task<OrderData>> m_OrderDataCache = new();

        // Ambient per-flow context that tells GenerateURL whether the current compliance callback
        // was fired by the PaymentProvider purchase path (return PSP URL) or the webshop redirect
        // path (return the webshop URL we just fetched). Internal-only; the developer's compliance
        // callback just calls GenerateURL and gets the right URL for whichever flow is running.
        static readonly AsyncLocal<PurchaseFlowContext?> s_FlowContext = new();

        ILogger m_Logger;

        protected readonly CheckoutLauncherCoordinator m_CheckoutCoordinator;

        [Preserve]
        [Inject]
        internal PaymentProviderImpl(
            IPaymentProviderClientWrapper paymentProviderClientWrapper,
            IWebshopClientWrapper webshopClientWrapper,
            ICatalogListingClient catalogListingClient,
            IRetryService retryService, // might be able to modify retry service to use coroutines on webgl?
            ILogger logger,
            IUtil util,
            IPlayerData playerData,
            ICurrencyFormatter currencyFormatter
            )
        {
            m_PaymentProviderClientWrapper = paymentProviderClientWrapper;
            m_WebshopClientWrapper = webshopClientWrapper;
            m_CatalogListingClient = catalogListingClient;
            m_Logger = logger;
            m_Util = util;
            m_PlayerData = playerData;
            m_CurrencyFormatter = currencyFormatter;
            m_CheckoutCoordinator = new CheckoutLauncherCoordinator(util, logger);
            m_Util.focusChanged += OnFocusChanged;
        }

        // Test seam.
        internal PaymentProviderImpl(
            IPaymentProviderClientWrapper paymentProviderClientWrapper,
            IWebshopClientWrapper webshopClientWrapper,
            ICatalogListingClient catalogListingClient,
            IRetryService retryService,
            ILogger logger,
            IUtil util,
            IPlayerData playerData,
            ICurrencyFormatter currencyFormatter,
            CheckoutLauncherCoordinator coordinator
        )
        {
            m_PaymentProviderClientWrapper = paymentProviderClientWrapper;
            m_WebshopClientWrapper = webshopClientWrapper;
            m_CatalogListingClient = catalogListingClient;
            m_Logger = logger;
            m_Util = util;
            m_PlayerData = playerData;
            m_CurrencyFormatter = currencyFormatter;
            m_CheckoutCoordinator = coordinator;
            m_Util.focusChanged += OnFocusChanged;
        }

        // One presentation mode per launch context (both default to ExternalBrowser):
        // payment checkout (OpenURL) and webshop redirect (RedirectToWebshop). The
        // shared coordinator takes the mode per call.
        CheckoutPresentationMode m_PaymentCheckoutMode = CheckoutPresentationMode.ExternalBrowser;
        CheckoutPresentationMode m_WebshopMode = CheckoutPresentationMode.ExternalBrowser;

        public void SetCheckoutPresentationMode(CheckoutPresentationMode mode)
        {
            m_PaymentCheckoutMode = mode;
        }

        public void SetWebshopPresentationMode(CheckoutPresentationMode mode)
        {
            m_WebshopMode = mode;
        }

        public void SetWebViewLauncher(IWebViewLauncher? launcher)
        {
            m_CheckoutCoordinator.SetOverrideLauncher(launcher);
        }

        public void SetDeepLinkScheme(string? scheme)
        {
            m_CheckoutCoordinator.SetDeepLinkScheme(scheme);
        }

        #region OrderPolling

        protected IUtil m_Util;

        const int k_MaxOrderRetryAttempts = 5;
        const int k_RetryDelaySeconds = 2;

        protected UnresolvedOrders m_UnresolvedOrders = new UnresolvedOrders(k_MaxOrderRetryAttempts);
        bool m_ProcessingUnresolvedOrders = false;

        // Polling must only begin once the user returns from the browser / the webview is closed.
        bool m_CheckoutInProgress = false;

        void OnFocusChanged(bool hasFocus)
        {
            if (!hasFocus || m_CheckoutInProgress || m_ProcessingUnresolvedOrders || !m_UnresolvedOrders.HasOrders)
            {
                return;
            }

            FetchOutgoingPurchases();
        }

        async void FetchOutgoingPurchases()
        {
            m_ProcessingUnresolvedOrders = true;

            await PollOrders();

            if (!m_UnresolvedOrders.HasOrders)
            {
                m_ProcessingUnresolvedOrders = false;
            }
            else
            {
                TriggerOrderPollAfterWait();
            }
        }

        private void TriggerOrderPollAfterWait()
        {
            m_Util.InitiateCoroutine(WaitToPoll());
        }

        IEnumerator WaitToPoll()
        {
            // We use WaitForSecondsRealtime to avoid issues if Time.timeScale is modified.
            // e.g. to pause the game while making/processing purchases.
            yield return new WaitForSecondsRealtime(k_RetryDelaySeconds);
            FetchOutgoingPurchases();
        }

        async Task PollOrders()
        {
            var tmpUnresolvedOrders = m_UnresolvedOrders.GetValidOrders();
            foreach (var order in tmpUnresolvedOrders)
            {
                var shouldFinalize = m_UnresolvedOrders
                    .CheckShouldFinalizeAndIncrementRetryCount(order.orderId);

                var resolved = await TryResolveOrder(order, shouldFinalize);

                if (resolved)
                {
                    m_UnresolvedOrders.RemoveOrder(order.orderId);
                }
                else if (shouldFinalize && !resolved)
                {
                    // if we failed to call a callback when we expected to, that is not good.
                    // call deferred just in case.
                    ForceDeferredOrder(order);
                    m_UnresolvedOrders.RemoveOrder(order.orderId);
                }
            }
        }

        void ForceDeferredOrder(UnresolvedOrder order)
        {
            ICart cart;
            try
            {
                cart = CreateCartFromOrderData(order.orderData, new NonNullCartValidator());
            }
            catch
            {
                cart = new Cart(new List<CartItem>());
            }

            PurchaseCallback?.OnPurchaseDeferred(
                new DeferredOrder(
                    cart,
                    new OrderInfo("", order.orderId.ToFormattedString(),
                        PaymentProvider.Name)
                )
            );
        }

        async Task<bool> TryResolveOrder(UnresolvedOrder order, bool shouldFinalize)
        {
            var orderIdString = order.orderId.ToFormattedString();

            // First try-catch. Try to cancel the order if cancellation is a valid action for the order.
            // Depending on the result of this call, we may or may not stop trying to cancel the order.
            try
            {
                if (order.shouldTryCancel)
                {
                    var cancelledOrder = await CancelOrder(orderIdString);

                    // Anything other than a 200 success response should throw an exception.
                    // We should only receive a success when an order is successfully cancelled.

                    m_UnresolvedOrders.MarkOrderCannotBeCancelled(order.orderId);

                    var callbackInvoked = InvokePollingCallbacks(cancelledOrder, shouldFinalize);
                    if (callbackInvoked)
                    {
                        return true;
                    }
                }
            }
            // if this is the final attempt, allow a hail mary call to GetOrder
            // otherwise, do not try to GetOrder if we have a network/server unavailable error or similar
            catch (PaymentProviderException e) when (e.IsRetriable() && !shouldFinalize)
            {
                // Do not try to GetOrder this loop if we have a network/server error.
                return false;
            }
            catch (PaymentProviderException<ConflictError>)
            {
                // Order already has another non-created status.
                m_UnresolvedOrders.MarkOrderCannotBeCancelled(order.orderId);
            }
            catch (PaymentProviderException<BadRequestError>)
            {
                // Incorrect update request. Not resolvable, do not continue to cancel.
                m_UnresolvedOrders.MarkOrderCannotBeCancelled(order.orderId);
            }
            catch (Exception)
            {
                // Problem is unknown. Allow GetOrder and future cancellation attempts.
            }

            // Get order information if cancellation did not succeed or was not attempted.
            // Retriable errors on cancellation will continue unless the order has reached max retry attempts.
            // If the order has reached max retries, then we try to fetch order information as a best effort attempt
            // to return meaningful information about the order to the developer.

            try
            {
                var fetchedOrder = await GetOrder(orderIdString);

                // Anything other than a 200 success response should throw an exception.

                var callbackInvoked = InvokePollingCallbacks(fetchedOrder, shouldFinalize);
                return callbackInvoked;
            }
            catch (Exception e)
            {
                // Attempt to invoke OnPurchaseDeferred if this is the final call.
                // If the orderData in unresolvedOrders is NOT `created`, something has gone wrong.
                var callbackInvoked = LogErrorAndInvokeDeferredIfFinal(e, order, shouldFinalize);
                return callbackInvoked;
            }
        }

        bool LogErrorAndInvokeDeferredIfFinal(Exception exception, UnresolvedOrder order, bool shouldFinalize)
        {
            // Attempt to invoke OnPurchaseDeferred if this is the final call.
            // If the orderData in unresolvedOrders is NOT `created`, something has gone wrong.
            var invokedCallback = InvokePollingCallbacks(order.orderData, shouldFinalize);

            if (invokedCallback)
            {
                string debugLog = exception switch
                {
                    PaymentProviderException<NetworkError> => "Network error occured when trying to get order. Call FetchPurchases() when network connection is available.",
                    PaymentProviderException<ServiceUnavailableError> => "Service is currently unavailable when trying to get order. Call FetchPurchases() later.",
                    CloudProjectAuthenticationException => "Player not authenticated when trying to get order status.",
                    { } e => $"Unknown error occured when trying to get order status. {e.Message}",
                    _ => "Unknown error occured when trying to get order status."
                };

                Debug.unityLogger.LogIAP(debugLog);
            }

            return invokedCallback;
        }

        bool InvokePollingCallbacks(OrderData orderData, bool finalized)
        {
            var order = CreateOrderFromPurchase(orderData);
            switch (order)
            {
                case PendingOrder pendingOrder:
                    PurchaseCallback?.OnPurchaseSucceeded(pendingOrder);
                    return true;
                case FailedOrder failedOrder:
                    PurchaseCallback?.OnPurchaseFailed(failedOrder);
                    return true;
                case ConfirmedOrder confirmedOrder:
                    PurchaseCallback?.OnPurchaseFailed(
                        new FailedOrder(
                            confirmedOrder.CartOrdered,
                            PurchaseFailureReason.OrderStateChanged,
                            "Purchase has already been confirmed."));
                    return true;
                case DeferredOrder deferredOrder:
                    if (finalized)
                    {
                        PurchaseCallback?.OnPurchaseDeferred(deferredOrder);
                    }
                    return finalized;
            }

            return false;
        }

        #endregion

        public void SetPaymentProviderOverride(string? paymentProviderOverride)
        {
            m_PaymentProviderOverride = paymentProviderOverride;
        }

        public void SetComplianceCheck(Func<PaymentProviderComplianceContext, Task<bool>>? complianceCheck)
        {
            m_ComplianceCheck = complianceCheck;
        }

        // Runs the developer-supplied compliance callback before order creation.
        // Returns false (and emits a FailedOrder) if the developer rejects or the callback throws.
        protected async Task<bool> RunComplianceCheckAsync(ICart cart)
        {
            if (m_ComplianceCheck == null)
            {
                return true;
            }

            try
            {
                if (await m_ComplianceCheck(new PaymentProviderComplianceContext(cart)))
                {
                    return true;
                }

                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.PurchasingUnavailable,
                        "Compliance check rejected the purchase."));
                return false;
            }
            catch (RestrictedTokensNotAvailable)
            {
                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.NotSupported,
                        k_WebshopUnsupportedMessage));
                return false;
            }
            catch (Exception e)
            {
                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.PurchasingUnavailable,
                        $"Compliance check threw: {e.Message}"));
                return false;
            }
        }

        const string k_WebshopUnsupportedMessage =
            "Webshop is not supported by the installed version of com.unity.services.authentication. " +
            "Upgrade to 3.7.1 or above.";

        bool IsWebshopSupported()
        {
            return m_WebshopClientWrapper.WebshopClientIsAvailable
                && m_WebshopClientWrapper.GetWebshopService().RestrictedTokensAvailable();
        }

        public override void Connect()
        {
            var connectionState = GetStoreConnectionState();
            if (connectionState == ConnectionState.Connected)
            {
                ConnectCallback?.OnStoreConnectionSucceeded();
                return;
            }

            var service = m_PaymentProviderClientWrapper.GetPaymentProviderService();

            if (service is null)
            {
                SetStoreConnectionState(ConnectionState.Unavailable);
                ConnectCallback?.OnStoreConnectionFailed(
                    new StoreConnectionFailureDescription("Unity Services uninitialized. Make sure you use 'UnityServices.InitializeAsync()' before connecting.")
                    );
                return;
            }

            SetStoreConnectionState(ConnectionState.Connected);
            ConnectCallback?.OnStoreConnectionSucceeded();
        }

        public override void FetchProducts(IReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            var connectionState = GetStoreConnectionState();

            if (connectionState != ConnectionState.Connected || !m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                var failureDescription = new ProductFetchFailureDescription(
                    ProductFetchFailureReason.ProviderUnavailable,
                    "PaymentProvider store is not connected.",
                    false
                );
                ProductsCallback?.OnProductsFetchFailed(failureDescription);
                return;
            }

            TryFetchProducts(productDefinitions);
        }

        private async void TryFetchProducts(IReadOnlyCollection<ProductDefinition> productDefinitions)
        {
            try
            {
                var fetchResult = await GetProducts(
                    productDefinitions.Select(product => product.storeSpecificId).ToList()
                    );

                if (!fetchResult.CompletedSuccessfully)
                {
                    var failedAt = fetchResult.LastFailedAfter ?? "<first page>";
                    Debug.unityLogger.LogIAPWarning(
                        $"Catalog paging incomplete after cursor {failedAt}; delivering partial product set.");
                }
                // Key metadata by catalogListingId so multi-listing products (multiple results sharing
                // the same unitySku but with distinct catalogListingIds) don't collapse into one entry.
                var metadata = fetchResult.Results.ToDictionary(
                    result => result.CatalogListingId ?? result.USku,
                    CreateProductMetadataFromCatalogListing
                );

                var productsFetched = MergeResultsWithProductDefinitions(productDefinitions, metadata);
                ProductsCallback?.OnProductsFetched(productsFetched);
            }
            catch (LiveContentAdapterException e)
            {
                var failureDescription = new ProductFetchFailureDescription(
                    ProductFetchFailureReason.Unknown,
                    $"Unknown error: {e.Message}"
                );
                ProductsCallback?.OnProductsFetchFailed(failureDescription);
            }
            catch (LiveContentAdapterCloudError e)
            {
                var failureDescription = new ProductFetchFailureDescription(
                    ProductFetchFailureReason.UserNotAuthenticated,
                    $"User authentication failed: {e.Message}"
                );
                ProductsCallback?.OnProductsFetchFailed(failureDescription);
            }
            catch (Exception e)
            {
                var failureDescription = new ProductFetchFailureDescription(
                    ProductFetchFailureReason.Unknown,
                    $"Unknown error: {e.Message}"
                );
                ProductsCallback?.OnProductsFetchFailed(failureDescription);
            }
        }

        ProductMetadata CreateProductMetadataFromCatalogListing(CatalogListingDto catalogListingDto)
        {
            var localizer = new CatalogListingLocalization(m_CurrencyFormatter);
            var locales = catalogListingDto.ProductDetails
                .Select(pd => pd.Language).OfType<string>()
                .ToList() ?? new List<string>();
            var locale = localizer.SelectLanguage(locales, m_PlayerData.Locale, RegionInfo.CurrentRegion, CultureInfo.CurrentCulture);
            var productDetails = catalogListingDto.ProductDetails
                .FirstOrDefault(pd => pd.Language == locale);

            var currencies = catalogListingDto.Pricing?
                .Select(p => p.CurrencyCode).OfType<string>()
                .ToList() ?? new List<string>();
            var currency = localizer.SelectCurrency(currencies, m_PlayerData.CurrencyCode, RegionInfo.CurrentRegion, CultureInfo.CurrentCulture);

            var pricingDetails = catalogListingDto.Pricing?
                .FirstOrDefault(p => p.CurrencyCode == currency);
            var priceInMicros =  pricingDetails?.Amount ?? 0;
            var price = priceInMicros / 1000000.00m;
            var priceString = localizer.CreatePriceString(price, currency, m_PlayerData.Locale, CultureInfo.CurrentCulture);
            var webshopPriceInMicros = pricingDetails?.WebshopPrice;
            var webshopPrice = webshopPriceInMicros / 1000000.00m;
            var webshopPriceString = webshopPrice != null ? localizer.CreatePriceString((decimal) webshopPrice, currency, m_PlayerData.Locale, CultureInfo.CurrentCulture) : null;


            return new PaymentProviderProductMetadata(
                priceString,
                productDetails?.Title ?? "",
                productDetails?.Description ?? "",
                currency,
                price,
                webshopPrice,
                webshopPriceString,
                catalogListingDto.HasWebshop
            );
        }

        public override void FetchPurchases()
        {
            var connectionState = GetStoreConnectionState();

            if (connectionState != ConnectionState.Connected || !m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                var failureDescription = new PurchasesFetchFailureDescription(
                    PurchasesFetchFailureReason.PurchasingUnavailable,
                    "PaymentProvider store is not connected."
                );
                PurchaseFetchCallback?.OnPurchasesRetrievalFailed(failureDescription);
                return;
            }

            TryFetchPurchases();
        }

        private async void TryFetchPurchases()
        {
            try
            {
                var results = await GetEntitledOrders();

                List<Order> orders = results
                    .Select(CreateOrderFromPurchase)
                    .Where(order => order != null)
                    .ToList()!;

                PurchaseFetchCallback?.OnAllPurchasesRetrieved(orders);
            }
            catch (PaymentProviderException e)
            {
                var failureDescription = new PurchasesFetchFailureDescription(
                    PurchasesFetchFailureReason.Unknown,
                    $"Unknown error: {e.Message}"
                );
                PurchaseFetchCallback?.OnPurchasesRetrievalFailed(failureDescription);
            }
            catch (CloudProjectAuthenticationException e)
            {
                var failureDescription = new PurchasesFetchFailureDescription(
                    PurchasesFetchFailureReason.UserNotAuthenticated,
                    $"User authentication failed: {e.Message}"
                );
                PurchaseFetchCallback?.OnPurchasesRetrievalFailed(failureDescription);
            }
            catch (Exception e)
            {
                var failureDescription = new PurchasesFetchFailureDescription(
                    PurchasesFetchFailureReason.Unknown,
                    $"Unknown error: {e.Message}"
                );
                PurchaseFetchCallback?.OnPurchasesRetrievalFailed(failureDescription);
            }
        }

        private Order? CreateOrderFromPurchase(OrderData orderData)
        {
            try
            {
                var cartValidator = new NonNullCartValidator();
                var cart = CreateCartFromOrderData(orderData, cartValidator);
                var orderInfo = new OrderInfo("", orderData.id.ToFormattedString(), PaymentProvider.Name);
                return orderData.status switch
                {
                    OrderStatus.Created => new DeferredOrder(cart, orderInfo),
                    OrderStatus.Paid => new PendingOrder(cart, orderInfo),
                    OrderStatus.Fulfilled => new ConfirmedOrder(cart, orderInfo),
                    OrderStatus.Cancelled => new FailedOrder(cart, PurchaseFailureReason.OrderCancelled, "The order has been cancelled."),
                    OrderStatus.Failed => new FailedOrder(cart, PurchaseFailureReason.Unknown, "The purchase has failed."),
                    OrderStatus.Revoked => new FailedOrder(cart, PurchaseFailureReason.Unknown, "Revoked order received."),
                    _ => new FailedOrder(cart, PurchaseFailureReason.Unknown, "Invalid purchase status returned from server.")
                };
            }
            catch (Exception)
            {
                return null;
            }
        }

        private ICart CreateCartFromOrderData(OrderData orderData, ICartValidator cartValidator)
        {
            var cartItemList = new List<CartItem>();
            foreach (var lineItem in orderData.lineItems)
            {
                var product = ProductCache.Find(lineItem.unitySku) ?? Product.CreateUnknownProduct(lineItem.unitySku);

                // There is no receipt for PaymentProvider purchases
                var sourceListing = product.baseListing;
// Obsolete: Product.transactionID
#pragma warning disable 618, 612
                var updatedProduct = new Product(sourceListing?.definition, sourceListing?.metadata)
                {
                    transactionID = orderData.id.ToFormattedString()
                };
#pragma warning restore 618, 612

                var cartItem = new CartItem(updatedProduct);
                cartItemList.Add(cartItem);
            }

            var cart = new Cart(cartItemList);
            cartValidator.Validate(cart);

            return cart;
        }

        public override void Purchase(ICart cart)
        {
            PurchaseInternal(cart);
        }

        public void Purchase(ICart cart, string paymentProviderName)
        {
            if (string.IsNullOrEmpty(paymentProviderName))
            {
                PurchaseCallback?.OnPurchaseFailed(new FailedOrder(cart, PurchaseFailureReason.Unknown,
                    $"{nameof(paymentProviderName)} must be non-empty"));
                return;
            }
            PurchaseInternal(cart, paymentProviderName);
        }

        public void PurchaseProduct(string catalogListingId, string paymentProviderName)
        {
            if (string.IsNullOrEmpty(paymentProviderName))
            {
                PurchaseCallback?.OnPurchaseFailed(new FailedOrder(
                    new Cart(Product.CreateUnknownProduct("InvalidProduct")),
                    PurchaseFailureReason.Unknown,
                    $"{nameof(paymentProviderName)} must be non-empty"));
                return;
            }
            var cart = BuildCartForCatalogListing(catalogListingId);
            if (cart is null)
                return; // BuildCartForCatalogListing already emitted OnPurchaseFailed
            PurchaseInternal(cart, paymentProviderName);
        }

        async void PurchaseInternal(ICart cart, string? paymentProviderName = null)
        {
#if DEBUG
            if (!HasAuthAccountChangedSubscriber)
            {
                Debug.unityLogger.LogIAPWarning("IPaymentProvidersExtendedPurchaseService.Purchase called without a callback defined for IStoreService.OnAuthAccountChanged.");
            }
#endif
            var connectionState = GetStoreConnectionState();

            if (connectionState != ConnectionState.Connected || !m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                var failedOrder = new FailedOrder(cart, PurchaseFailureReason.StoreNotConnected, "PaymentProvider store is not connected.");
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
                return;
            }

            s_FlowContext.Value = new PurchaseFlowContext(PurchaseChannel.PaymentProvider, paymentProviderName);
            try
            {
                if (!await RunComplianceCheckAsync(cart))
                {
                    // RunComplianceCheckAsync already emits OnPurchaseFailed;
                    return;
                }
            }
            finally
            {
                s_FlowContext.Value = null;
            }

            TryOpenURL(cart, paymentProviderName: paymentProviderName);
        }

        private async void TryOpenURL(ICart cart, IReadOnlyList<PaymentProviderToken>? externalTokens = null, string? paymentProviderName = null)
        {
            var firstCartItem = cart
                .Items()?
                .Where(item => item?.CatalogListingId is not null)
                .FirstOrDefault();

            var catalogListingId = firstCartItem?.CatalogListingId;

            if (firstCartItem is null || catalogListingId is null)
            {
                var failedOrder = new FailedOrder(cart, PurchaseFailureReason.ProductUnavailable, "Could not find catalog listing id in cart.");
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
                return;
            }

            try
            {
                await OpenURL(catalogListingId, externalTokens, paymentProviderName);
            }
            catch (PaymentProviderException<ConflictError> e)
            {
                var reason = firstCartItem.Product.type switch
                {
                    ProductType.Consumable => PurchaseFailureReason.ExistingPurchasePending,
                    _ => PurchaseFailureReason.DuplicateTransaction
                };
                var failedOrder = new FailedOrder(cart, reason, e.Message);
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
            }
            catch (PaymentProviderException e)
            {
                var failedOrder = new FailedOrder(cart, PurchaseFailureReason.Unknown, e.Message);
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
            }
            catch (CloudProjectAuthenticationException e)
            {
                var failedOrder = new FailedOrder(cart, PurchaseFailureReason.UserNotAuthenticated, $"User authentication failed: {e.Message}");
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
            }
            catch (Exception e)
            {
                var failedOrder = new FailedOrder(cart, PurchaseFailureReason.Unknown, $"Unknown error: {e.Message}");
                PurchaseCallback?.OnPurchaseFailed(failedOrder);
            }
        }

        private async Task OpenURL(string catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens = null, string? paymentProviderName = null)
        {
            var orderData = await GetOrInitiateOrder(catalogListingId, externalTokens, paymentProviderName);

            // Keep track of the order id before redirecting the user.
            // When the user returns to the application, we will have to verify the purchase status for that order id.
            m_UnresolvedOrders.AddOrder(orderData);

            m_OrderDataCache.TryRemove(catalogListingId, out _);

            // Suppress focus-driven polling while the checkout is open.
            CheckoutResult result;
            m_CheckoutInProgress = true;
            try
            {
                result = await m_CheckoutCoordinator.LaunchCheckoutAsync(orderData.paymentProviderUrl, m_PaymentCheckoutMode);
            }
            finally
            {
                m_CheckoutInProgress = false;
            }

            TriggerPollIfWebViewClosed(result);
        }

        protected void TriggerPollIfWebViewClosed(CheckoutResult result)
        {
            // For ExternalBrowser/Unknown/LauncherFailed, the user was sent to the
            // system browser; focus-driven polling will resume on app return.
            // For UserDismissed/DeepLinkReturned, the in-app browser closed without
            // a focus change, so trigger a poll explicitly.
            if (result.CloseReason != CheckoutCloseReason.UserDismissed
                && result.CloseReason != CheckoutCloseReason.DeepLinkReturned)
            {
                return;
            }

            if (m_ProcessingUnresolvedOrders || !m_UnresolvedOrders.HasOrders)
            {
                return;
            }

            FetchOutgoingPurchases();
        }

        public override void FinishTransaction(PendingOrder pendingOrder)
        {
            var connectionState = GetStoreConnectionState();

            if (connectionState != ConnectionState.Connected || !m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                var failedOrder = new FailedOrder(
                    pendingOrder,
                    PurchaseFailureReason.StoreNotConnected,
                    "PaymentProvider store is not connected."
                );
                ConfirmCallback?.OnConfirmOrderFailed(failedOrder);
                return;
            }

            TryFinishTransaction(pendingOrder);
        }

        private async void TryFinishTransaction(PendingOrder pendingOrder)
        {
            var transactionId = pendingOrder.Info.TransactionID;

            if (string.IsNullOrEmpty(transactionId))
            {
                ConfirmCallback?.OnConfirmOrderFailed(
                    new FailedOrder(
                        pendingOrder,
                        PurchaseFailureReason.ValidationFailure,
                        "Could not find transaction ID for order.")
                );
                return;
            }

            try
            {
                var orderData = await FinishOrder(transactionId);
                switch (orderData.status)
                {
                    case OrderStatus.Fulfilled:
                        ConfirmCallback?.OnConfirmOrderSucceeded(transactionId);
                        return;
                    case OrderStatus.Created:
                    case OrderStatus.Paid:
                        ConfirmCallback?.OnConfirmOrderFailed(
                            new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown, "Order confirmation failed.")
                            );
                        return;
                    case OrderStatus.Revoked:
                    case OrderStatus.Failed:
                        ConfirmCallback?.OnConfirmOrderFailed(
                            new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown, "Order is no longer valid.")
                            );
                        return;
                    case OrderStatus.Unknown:
                    default:
                        ConfirmCallback?.OnConfirmOrderFailed(
                            new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown, "Order is in unknown state.")
                        );
                        return;
                }
            }
            catch (PaymentProviderException<ConflictError>)
            {
                var failedOrder = new FailedOrder(pendingOrder, PurchaseFailureReason.OrderStateChanged,
                    "Order could not be confirmed. Ensure the order is a PendingOrder and has not previously been confirmed.");
                ConfirmCallback?.OnConfirmOrderFailed(failedOrder);
            }
            catch (PaymentProviderException e)
            {
                var failedOrder = new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown, e.Message);
                ConfirmCallback?.OnConfirmOrderFailed(failedOrder);
            }
            catch (CloudProjectAuthenticationException e)
            {
                var failedOrder = new FailedOrder(pendingOrder, PurchaseFailureReason.UserNotAuthenticated, $"User authentication failed: {e.Message}");
                ConfirmCallback?.OnConfirmOrderFailed(failedOrder);
            }
            catch (Exception e)
            {
                var failedOrder = new FailedOrder(pendingOrder, PurchaseFailureReason.Unknown, $"Unknown error: {e.Message}");
                ConfirmCallback?.OnConfirmOrderFailed(failedOrder);
            }
        }

        public override void CheckEntitlement(ProductDefinition productDefinition)
        {
            if (!CheckEntitlementValidation(productDefinition))
            {
                return;
            }

            EntitlementStatus status;
            var message = string.Empty;

            var orders = GetFetchedOrders();

            var storeSpecificId = productDefinition.storeSpecificId;
            // TODO: ULO-10739
            bool MatchProductId(Order order) =>
                order.CartOrdered.Items()?.Any(ci =>
                    ci.Product.catalogListings.TryGetValue(ci.CatalogListingId, out var listing)
                    && listing.definition.storeSpecificId == storeSpecificId) ?? false;

            var pendingOrder = orders.FirstOrDefault(
                o => o is PendingOrder && MatchProductId(o)
                );
            var confirmedOrder = orders.FirstOrDefault(
                o => o is ConfirmedOrder && MatchProductId(o)
                );

            switch (productDefinition.type)
            {
                case ProductType.Consumable:
                    if (pendingOrder != null) { status = EntitlementStatus.EntitledUntilConsumed; }
                    else { status = EntitlementStatus.NotEntitled; }
                    break;
                case ProductType.NonConsumable:
                    if (confirmedOrder != null) { status = EntitlementStatus.FullyEntitled; }
                    else if (pendingOrder != null) { status = EntitlementStatus.EntitledButNotFinished; }
                    else { status = EntitlementStatus.NotEntitled; }
                    break;
                case ProductType.Subscription:
                    // TODO ULO-8757 Implement subscription logic.
                    status = EntitlementStatus.Unknown;
                    message = "Subscriptions are not supported by PaymentProvider.";
                    break;
                default:
                    status = EntitlementStatus.Unknown;
                    message = $"Unknown product type {productDefinition.type} for product {productDefinition.storeSpecificId}";
                    break;
            }

            EntitlementCallback?.OnCheckEntitlement(productDefinition, status, message);
        }

        bool CheckEntitlementValidation(ProductDefinition productDefinition)
        {
            var connectionState = GetStoreConnectionState();

            if (connectionState != ConnectionState.Connected || !m_PaymentProviderClientWrapper.PaymentProviderClientIsAvailable)
            {
                EntitlementCallback?.OnCheckEntitlement(
                    productDefinition,
                    EntitlementStatus.Unknown,
                    "PaymentProvider store is not connected.");
                return false;
            }

            return true;
        }

        static ReadOnlyObservableCollection<Order> GetFetchedOrders()
        {
            var purchaseService = UnityIAPServices.Purchase(PaymentProvider.Name);
            var orders = purchaseService.GetPurchases();
            return orders;
        }

        private static List<ProductDescription> MergeResultsWithProductDefinitions(IReadOnlyCollection<ProductDefinition> productDefinitions,
            Dictionary<string, ProductMetadata> keyedMetadata)
        {
            var productsFetched = new List<ProductDescription>();
            try
            {
                foreach (var product in productDefinitions)
                {
                    if (!keyedMetadata.TryGetValue(product.catalogListingId, out var productMetadata))
                    {
                        continue;
                    }

                    productsFetched.Add(
                        new ProductDescription(
                            product.storeSpecificId,
                            productMetadata
                        )
                    );
                }

                return productsFetched;
            }
            catch (Exception e)
            {
                throw new Exception("Unexpected error while merging product definitions: " + e.Message);
            }
        }

        public async Task<EligiblePaymentProviders> GetEligiblePaymentProviders()
        {
            LogVerbose("Calling GetEligiblePaymentProviders to fetch eligible providers for current player.");
            var (providers, paymentOptionPopupEnabled) = await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .GetEligiblePaymentProviders();
            return new EligiblePaymentProviders(providers, paymentOptionPopupEnabled);
        }
        /// <summary>
        /// Returns the URL the current flow will redirect the player to. Intended for use inside
        /// the compliance callback registered with <see cref="SetComplianceCheck"/>:
        /// during a <see cref="Purchase"/> flow it returns the Payment Provider redirect URL;
        /// during a <see cref="RedirectToWebshop"/> flow it fetches (on first call) the webshop URL
        /// and stashes it so the outer redirect reuses it instead of fetching again. Outside any
        /// flow it falls back to creating a Payment Provider order and returning its URL.
        /// <paramref name="catalogListingId"/> may be null only inside a webshop compliance callback
        /// (yields the generic Unity webshop URL); a null id outside webshop flow returns null and
        /// emits <see cref="IPurchaseService.OnPurchaseFailed"/> because the Payment Provider order
        /// path requires a product.
        /// </summary>
        public async Task<string?> GenerateURL(string? catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens = null)
        {
            var context = s_FlowContext.Value;
            if (context?.Channel == PurchaseChannel.Webshop)
            {
                if (!IsWebshopSupported())
                {
                    throw new RestrictedTokensNotAvailable(k_WebshopUnsupportedMessage);
                }
                context.WebshopUrl ??= await FetchWebshopUrl(catalogListingId, null, externalTokens);
                return context.WebshopUrl;
            }

            if (catalogListingId is null)
            {
                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(new Cart(new List<CartItem>()), PurchaseFailureReason.ProductUnavailable,
                        "GenerateURL outside a webshop flow requires a catalog listing id."));
                return null;
            }

            // When called from inside a PaymentProvider purchase's compliance callback, route
            // through the same paymentProviderName the parent picked so the cached order is
            // created against the right provider and the post-compliance OpenURL reuses it.
            var orderData = await GetOrInitiateOrder(catalogListingId, externalTokens, context?.PaymentProviderName);
            return orderData.paymentProviderUrl;
        }

        public async Task RedirectToWebshop(string? catalogListingId = null, IReadOnlyList<PaymentProviderToken>? externalTokens = null)
        {
#if DEBUG
            if (!HasAuthAccountChangedSubscriber)
            {
                Debug.unityLogger.LogIAPWarning("IPaymentProvidersExtendedPurchaseService.RedirectToWebshop called without a callback defined for IStoreService.OnAuthAccountChanged.");
            }
#endif
            // Null catalogListingId opens the generic Unity webshop. There's no product to
            // build a cart from, but the compliance callback still runs with an empty cart so
            // developers can gate webshop access globally (e.g. age, region).
            ICart? cart = string.IsNullOrEmpty(catalogListingId)
                ? new Cart(new List<CartItem>())
                : BuildCartForCatalogListing(catalogListingId);
            if (cart == null)
            {
                // BuildCartForCatalogListing already emits OnPurchaseFailed;
                return;
            }

            if (!IsWebshopSupported())
            {
                PurchaseCallback?.OnPurchaseFailed(
                    new FailedOrder(cart, PurchaseFailureReason.NotSupported,
                        k_WebshopUnsupportedMessage));
                return;
            }

            s_FlowContext.Value = new PurchaseFlowContext(PurchaseChannel.Webshop);
            string? primedUrl;
            try
            {
                if (!await RunComplianceCheckAsync(cart))
                {
                    // RunComplianceCheckAsync already emits OnPurchaseFailed;
                    return;
                }
                primedUrl = s_FlowContext.Value?.WebshopUrl;
            }
            finally
            {
                s_FlowContext.Value = null;
            }

            var url = primedUrl ?? await FetchWebshopUrl(catalogListingId, null, externalTokens);

            // Present the webshop via the shared coordinator using the webshop's own
            // presentation mode. No poll afterwards: a webshop redirect generates no
            // order to resolve (unlike payment checkout's TriggerPollIfWebViewClosed).
            await m_CheckoutCoordinator.LaunchCheckoutAsync(url, m_WebshopMode);
        }

        // Mirrors PurchaseService.PurchaseProduct(string)'s lookup: catalog listing id first,
        // uSku fallback for back-compat. Returns null after emitting OnPurchaseFailed if the
        // listing can't be resolved to a product.
        ICart? BuildCartForCatalogListing(string catalogListingId)
        {
            var product = ProductCache.FindByCatalogListingId(catalogListingId);
            if (product == null && ProductCache.productsByUSku.TryGetValue(catalogListingId, out var byUSku))
            {
                product = byUSku;
            }
            if (product == null)
            {
                PurchaseCallback?.OnPurchaseFailed(new FailedOrder(new Cart(Product.CreateUnknownProduct("InvalidProduct")), PurchaseFailureReason.ProductUnavailable,
                    "Invalid catalog listing id: " + catalogListingId));
                return null;
            }

            try
            {
                return new Cart(new CartItem(product, catalogListingId));
            }
            catch (InvalidCartItemException e)
            {
                PurchaseCallback?.OnPurchaseFailed(new FailedOrder(new Cart(Product.CreateUnknownProduct("InvalidProduct")), PurchaseFailureReason.ProductUnavailable,
                    "Invalid product for catalog listing: " + e.Message));
                return null;
            }
        }

        async Task<string> FetchWebshopUrl(string? catalogListingId, string? impressionId, IReadOnlyList<PaymentProviderToken>? externalTokens)
        {
            LogVerbose($"Fetching webshop link for catalog listing: {catalogListingId ?? "<no catalog listing id>"}.");
            var link = await m_WebshopClientWrapper
                .GetWebshopService()
                .GetWebshopLink(
                    catalogListingId,
                    impressionId,
                    m_PlayerData.Locale,
                    m_PlayerData.CurrencyCode,
                    m_PlayerData.RegionCode,
                    MapWebshopExternalTokens(externalTokens)
                );

            if (!link.Live)
            {
                LogVerbose($"Webshop link for '{catalogListingId ?? "<no catalog listing id>"}' opens a non-live (preview/draft) storefront.");
            }

            return link.Url;
        }

        static IReadOnlyList<WebshopExternalToken> MapWebshopExternalTokens(IReadOnlyList<PaymentProviderToken>? tokens)
        {
            if (tokens == null || tokens.Count == 0)
            {
                return Array.Empty<WebshopExternalToken>();
            }

            var mapped = new List<WebshopExternalToken>(tokens.Count);
            foreach (var token in tokens)
            {
                mapped.Add(new WebshopExternalToken(MapWebshopExternalTokenType(token), token.Token));
            }
            return mapped;
        }

        static WebshopExternalTokenType MapWebshopExternalTokenType(PaymentProviderToken token) => token.Store switch
        {
            PaymentProviderTokenStore.Google => WebshopExternalTokenType.Google,
            PaymentProviderTokenStore.Apple => token.Type switch
            {
                ExternalPurchaseTokenType.Acquisition => WebshopExternalTokenType.AppleAcquisition,
                ExternalPurchaseTokenType.Services => WebshopExternalTokenType.AppleServices,
                ExternalPurchaseTokenType.LinkOut => WebshopExternalTokenType.AppleLinkOut,
                _ => throw new ArgumentException($"Apple PaymentProviderToken requires a type; got: {token.Type}"),
            },
            _ => throw new ArgumentOutOfRangeException(nameof(token), token.Store, "Unknown PaymentProviderToken store."),
        };

        #region InternalServiceCalls

        async Task<CatalogListingResult> GetProducts(List<string> productSkus)
        {
            LogVerbose($"Calling GetProducts to get information about: {string.Join(",", productSkus)}.");
            return await m_CatalogListingClient.GetCatalogListings();
        }

        async Task<List<OrderData>> GetEntitledOrders()
        {
            LogVerbose("Calling GetEntitledOrders to get all active orders for current player.");
            return await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .GetEntitledOrders();
        }

        protected async Task<OrderData> InitiateOrder(
            string catalogListingId,
            IReadOnlyList<PaymentProviderToken>? externalTokens = null,
            string? paymentProviderName = null)
        {
            LogVerbose($"Calling GetUrl to create new order and get redirect URL for catalog listing: {catalogListingId}.");
            var deviceInfo = BuildDeviceInfo();
            return await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .GetUrl(
                    catalogListingId,
                    m_PlayerData.DisplayName,
                    m_PlayerData.Locale,
                    m_PlayerData.CurrencyCode,
                    m_PlayerData.RegionCode,
                    await m_PlayerData.CreatePlayerIdentityAsync(),
                    paymentProviderName ?? m_PaymentProviderOverride,
                    ConvertDeviceInfoToGeneratedModel(deviceInfo),
                    externalTokens
                );
        }

        // Dedups concurrent callers for the same catalogListingId onto a single in-flight InitiateOrder so a forgotten `await`
        // on GenerateURL can't race OpenURL into creating two orders. The provider that was actually used is echoed back in
        // OrderData.paymentProvider — concurrent callers asking for different providers on the same listing will share the
        // first caller's order (rare in practice). Evicts on failure or empty URL so retries actually retry.
        Task<OrderData> GetOrInitiateOrder(string catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens, string? paymentProviderName = null)
        {
            return m_OrderDataCache.GetOrAdd(catalogListingId, _ => InitiateAndEvictOnFailure(catalogListingId, externalTokens, paymentProviderName));
        }

        async Task<OrderData> InitiateAndEvictOnFailure(string catalogListingId, IReadOnlyList<PaymentProviderToken>? externalTokens, string? paymentProviderName = null)
        {
            try
            {
                var orderData = await InitiateOrder(catalogListingId, externalTokens, paymentProviderName);
                if (string.IsNullOrWhiteSpace(orderData.paymentProviderUrl))
                {
                    m_OrderDataCache.TryRemove(catalogListingId, out _);
                }
                return orderData;
            }
            catch
            {
                m_OrderDataCache.TryRemove(catalogListingId, out _);
                throw;
            }
        }

        PaymentProviderService.Models.DeviceInfo? ConvertDeviceInfoToGeneratedModel(DeviceInfo? deviceInfo)
        {
            if (deviceInfo == null)
            {
                return null;
            }

            return new PaymentProviderService.Models.DeviceInfo(
                language: deviceInfo.Language,
                platform: deviceInfo.Platform,
                localeList: deviceInfo.LocaleList,
                deviceModel: deviceInfo.DeviceModel,
                systemBootTime: deviceInfo.SystemBootTime ?? 0,
                osVersion: deviceInfo.OSVersion,
                appBundleId: deviceInfo.AppBundleID,
                totalSpace: deviceInfo.TotalSpace ?? 0
            );
        }

        DeviceInfo? BuildDeviceInfo()
        {
            switch (Application.platform)
            {
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
                case RuntimePlatform.VisionOS:
#endif
                {
                    // Duplicate INativeAppleStore should be fine, as we are not connecting the transaction listener.
                    if (m_NativeStore == null)
                    {
                        var nativeStoreProvider = new NativeStoreProvider();
                        m_NativeStore = nativeStoreProvider.GetStorekit();
                    }
                    return m_DeviceInfo ??= AppleDeviceInfoBuilder.Build(m_NativeStore);
                }
                case RuntimePlatform.Android:
                    return m_DeviceInfo ??= AndroidDeviceInfoBuilder.Build();
                default:
                    return null;
            }
        }

        async Task<OrderData> GetOrder(string transactionId)
        {
            LogVerbose($"Calling GetOrder to get single order for order: {transactionId}.");
            return await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .GetOrder(transactionId);
        }

        internal async Task<OrderData> CancelOrder(string transactionId)
        {
            LogVerbose($"Calling UpdateOrder to cancel order: {transactionId}.");
            return await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .UpdateOrder(transactionId, UpdateOrderStatus.Cancelled);
        }

        async Task<OrderData> FinishOrder(string transactionId)
        {
            LogVerbose($"Calling UpdateOrder to fulfill order: {transactionId}.");
            return await m_PaymentProviderClientWrapper
                .GetPaymentProviderService()
                .UpdateOrder(transactionId, UpdateOrderStatus.Fulfilled);
        }

        #endregion

        #region LoggerHelpers

        void LogVerbose(string message,
            // nameof and CallerMemberName both act as compile time consts and do not use reflection
            string className = nameof(PaymentProviderImpl),
            [CallerMemberName] string callerName = "")
        {
            m_Logger.LogIAPCallVerbose(message, className, callerName);
        }

        #endregion
    }

    enum PurchaseChannel
    {
        PaymentProvider,
        Webshop
    }

    sealed class PurchaseFlowContext
    {
        public PurchaseChannel Channel { get; }

        // Carries the per-call paymentProviderName chosen by the parent purchase
        // call so a developer's compliance callback that invokes GenerateURL gets
        // an order created against the same provider as the surrounding purchase.
        // Without this, GenerateURL would fall back to m_PaymentProviderOverride
        // and the order in the cache would be created against the wrong provider.
        public string? PaymentProviderName { get; }

        // Mutable so GenerateURL can prime the URL during a Webshop compliance callback and have
        // RedirectToWebshop pick it up after the callback returns (shared by-reference via AsyncLocal).
        public string? WebshopUrl { get; set; }

        public PurchaseFlowContext(PurchaseChannel channel, string? paymentProviderName = null, string? webshopUrl = null)
        {
            Channel = channel;
            PaymentProviderName = paymentProviderName;
            WebshopUrl = webshopUrl;
        }
    }
}
