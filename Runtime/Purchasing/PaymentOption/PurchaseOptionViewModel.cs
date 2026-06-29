#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing
{
    class PurchaseOptionViewModel : IDisposable
    {
        bool m_Disposed;
        TaskCompletionSource<PurchaseOption?>? m_TcsOption;

        // Service resolvers — overridden by tests to inject fakes.
        // Defaults return a real StoreController, which implements both interfaces.
        internal virtual IPurchaseService ResolvePurchaseService(string storeName)
            => ResolveController(storeName);

        internal virtual IProductService ResolveProductService(string storeName)
            => ResolveController(storeName);

        // PSP purchase extension used by the webshop branch of CompleteChoice
        // to invoke RedirectToWebshop, and by the PSP-identifier branch of
        // PurchaseSelectedOption to invoke the per-call PurchaseProduct overload.
        // Distinct from FetchEligibility's IPaymentProvidersExtendedService reach
        // (that one owns the eligibility/picker-config surface); these methods
        // live on the purchase side. Virtual so tests can fake the extension.
        internal virtual IPaymentProvidersExtendedPurchaseService? ResolvePaymentProviders()
            => UnityIAPServices.Purchase(PaymentProvider.Name).PaymentProviders;

        // Routes a non-webshop pick to the right purchase method without
        // mutating SetPaymentProviderOverride state. Known SDK stores (native,
        // Apple, Google, the default PaymentProvider, etc.) go through the
        // standard IPurchaseService.PurchaseProduct path. PSP-specific
        // identifiers (e.g. "stripe", "coda") go through the per-call overload
        // IPaymentProvidersExtendedPurchaseService.PurchaseProduct(listing, name).
        void PurchaseSelectedOption(string storeName, string catalogListingId)
        {
            if (storeName == IPaymentOptionProvider.NativeAlias || IsKnownSdkStoreName(storeName))
            {
                ResolvePurchaseService(storeName).PurchaseProduct(catalogListingId);
                return;
            }

            var psp = ResolvePaymentProviders();
            if (psp is null)
                throw new InvalidOperationException(
                    "PaymentProvider extension unavailable; cannot route purchase through PSP-specific identifier '" + storeName + "'");
            psp.PurchaseProduct(catalogListingId, storeName);
        }

        public async Task<bool> ValidatePurchaseOption(string catalogListingId)
        {
            if (m_Disposed)
                throw new InvalidOperationException("PurchaseOptionViewModel has been disposed");
            if (string.IsNullOrEmpty(catalogListingId))
                throw new ArgumentException("catalogListingId required", nameof(catalogListingId));

            var stores = await GetDefaultPurchaseOrder();
            return ValidateAcrossStores(stores, catalogListingId);
        }

        public bool ValidatePurchaseOption(IReadOnlyList<PurchaseOption> options)
        {
            if (m_Disposed)
                throw new InvalidOperationException("PurchaseOptionViewModel has been disposed");
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Count == 0)
                return false;

            string? currency = null;
            foreach (var opt in options)
            {
                var info = GetPriceInfo(opt.StoreName, opt.CatalogListingId);
                if (info == null)
                    return false;

                if (currency == null)
                    currency = info.Value.currency;
                else if (currency != info.Value.currency)
                    return false;
            }
            return true;
        }

        bool ValidateAcrossStores(IReadOnlyList<string> stores, string catalogListingId)
        {
            if (stores.Count == 0)
                return false;

            string? currency = null;
            foreach (var name in stores)
            {
                var info = GetPriceInfo(name, catalogListingId);
                if (info == null)
                    return false;

                if (currency == null)
                    currency = info.Value.currency;
                else if (currency != info.Value.currency)
                    return false;
            }
            return true;
        }

        (decimal price, string currency)? GetPriceInfo(string storeName, string catalogListingId)
        {
            var product = ResolveProductService(storeName).GetProductByCatalogListingId(catalogListingId);
            if (product?.metadata == null)
                return null;
            if (product.metadata.localizedPrice <= 0m)
                return null;
            var iso = product.metadata.isoCurrencyCode;
            if (string.IsNullOrEmpty(iso))
                return null;
            return (product.metadata.localizedPrice, iso);
        }

        public Task<PurchaseOption?> BeginShowPurchaseOption(
            IReadOnlyList<PurchaseOption> options,
            out IReadOnlyList<PurchaseOpt> internalOptions)
        {
            CheckPreconditionsOption(options);

            if (options.Count == 1)
            {
                var only = options[0];
                Debug.unityLogger.LogIAPVerbose($"SkipPicker: single option {DisplayStoreName(only.StoreName)} / {only.CatalogListingId}; routing directly");
                PurchaseSelectedOption(only.StoreName, only.CatalogListingId);
                internalOptions = Array.Empty<PurchaseOpt>();
                return Task.FromResult<PurchaseOption?>(only);
            }

            internalOptions = BuildOptsFromOptions(options);
            m_TcsOption = new TaskCompletionSource<PurchaseOption?>();
            return m_TcsOption.Task;
        }

        /// Single-listing variant: same shape as <see cref="BeginShowPurchaseOption"/>
        /// but folds in the webshop button when the listing has
        /// <see cref="PaymentProviderProductMetadata.hasWebshop"/> set. Single-option
        /// short-circuit only fires when there's one store-routed option AND no webshop
        /// is being appended — otherwise the picker stays open so the user can interact
        /// with the webshop button. Empty <paramref name="storeOptions"/> is permitted
        /// when the listing has a webshop (the picker shows the webshop button alone);
        /// otherwise an empty list throws. Cross-product callers should keep using
        /// <see cref="BeginShowPurchaseOption"/>; webshop is single-listing only.
        public Task<PurchaseOption?> BeginShowPurchaseOptionForListing(
            IReadOnlyList<PurchaseOption> storeOptions,
            string catalogListingId,
            out IReadOnlyList<PurchaseOpt> internalOptions)
        {
            if (m_Disposed)
                throw new InvalidOperationException("PurchaseOptionViewModel has been disposed");
            if (storeOptions == null)
                throw new ArgumentNullException(nameof(storeOptions));
            if (string.IsNullOrEmpty(catalogListingId))
                throw new ArgumentException("catalogListingId required", nameof(catalogListingId));
            if (m_TcsOption != null)
                throw new InvalidOperationException("ShowPurchaseOption is already in progress");

            var includeWebshop = HasWebshop(catalogListingId);
            if (storeOptions.Count == 0 && !includeWebshop)
                throw new ArgumentException(
                    "At least one storeOption is required when the listing has no webshop",
                    nameof(storeOptions));

            if (storeOptions.Count == 1 && !includeWebshop)
            {
                var only = storeOptions[0];
                Debug.unityLogger.LogIAPVerbose($"SkipPicker: single option {DisplayStoreName(only.StoreName)} / {only.CatalogListingId}; routing directly");
                PurchaseSelectedOption(only.StoreName, only.CatalogListingId);
                internalOptions = Array.Empty<PurchaseOpt>();
                return Task.FromResult<PurchaseOption?>(only);
            }

            var opts = new List<PurchaseOpt>(storeOptions.Count + 1);
            opts.AddRange(BuildOptsFromOptions(storeOptions));
            if (includeWebshop)
            {
                var webshopBadge = ComputeWebshopBadge(catalogListingId);
                opts.Add(new PurchaseOpt
                {
                    IsWebshop = true,
                    Label = "Continue with webshop",
                    Badge = webshopBadge,
                    StoreName = IPaymentOptionProvider.WebshopAlias,
                    CatalogListingId = catalogListingId,
                    Option = new PurchaseOption(
                        IPaymentOptionProvider.WebshopAlias,
                        catalogListingId,
                        webshopBadge),
                });
            }
            internalOptions = opts;
            m_TcsOption = new TaskCompletionSource<PurchaseOption?>();
            return m_TcsOption.Task;
        }

        public async Task CompleteChoice(PurchaseOpt? choice)
        {
            if (m_Disposed)
                return;

            var tcs = m_TcsOption;
            if (tcs is null)
                throw new InvalidOperationException("No picker flow in progress");

            m_TcsOption = null;

            if (choice is null)
            {
                tcs.TrySetResult(null);
                return;
            }

            var pick = choice.Option ?? new PurchaseOption(choice.StoreName, choice.CatalogListingId, choice.Badge);

            if (choice.IsWebshop)
            {
                // Await so async failures on the URL-fetch path (which throw
                // on the returned task, not via OnPurchaseFailed) surface here
                // instead of vanishing into UnobservedTaskException.
                try
                {
                    var redirect = ResolvePaymentProviders()?.RedirectToWebshop(choice.CatalogListingId);
                    if (redirect is not null)
                        await redirect;
                }
                catch (Exception ex)
                {
                    Debug.unityLogger.LogIAPWarning($"RedirectToWebshop failed: {ex.GetType().Name}: {ex.Message}");
                    tcs.TrySetException(new PurchaseChoiceFailedException(pick, ex));
                    return;
                }
                tcs.TrySetResult(pick);
                return;
            }

            // Called from a UI click handler; Unity swallows sync exceptions
            // raised inside event dispatch. Route them through the TCS so
            // awaiters of ShowPurchaseOption fault instead of hanging forever.
            try
            {
                PurchaseSelectedOption(choice.StoreName, choice.CatalogListingId);
            }
            catch (Exception ex)
            {
                tcs.TrySetException(new PurchaseChoiceFailedException(pick, ex));
                return;
            }

            tcs.TrySetResult(pick);
        }

        public void Dispose()
        {
            if (m_Disposed) return;
            m_TcsOption?.TrySetResult(null);
            m_TcsOption = null;
            m_Disposed = true;
        }

        internal IReadOnlyList<PurchaseOpt> BuildOptsFromOptions(IReadOnlyList<PurchaseOption> options)
        {
            var opts = new List<PurchaseOpt>(options.Count);
            foreach (var option in options)
            {
                opts.Add(new PurchaseOpt
                {
                    Label = $"Pay with {DisplayStoreName(option.StoreName)}",
                    Badge = option.Badge,
                    IsNative = string.Equals(option.StoreName, IPaymentOptionProvider.NativeAlias, StringComparison.OrdinalIgnoreCase),
                    StoreName = option.StoreName,
                    CatalogListingId = option.CatalogListingId,
                    Option = option
                });
            }
            return opts;
        }

        void CheckPreconditionsOption(IReadOnlyList<PurchaseOption> options)
        {
            if (m_Disposed)
                throw new InvalidOperationException("PurchaseOptionViewModel has been disposed");
            if (options == null)
                throw new ArgumentNullException(nameof(options));
            if (options.Count == 0)
                throw new ArgumentException("At least one option required", nameof(options));
            if (m_TcsOption != null)
                throw new InvalidOperationException("ShowPurchaseOption is already in progress");
        }

        /// True when the listing's <see cref="PaymentProviderProductMetadata.hasWebshop"/>
        /// flag is set. Used by the providers to decide whether to render the webshop
        /// slot at all. Virtual so tests can stub the webshop branch without
        /// fabricating a full PaymentProviderProductMetadata.
        internal virtual bool HasWebshop(string catalogListingId)
        {
            var product = ResolveProductService(PaymentProvider.Name).GetProductByCatalogListingId(catalogListingId);
            var pspMeta = product?.metadata?.GetPaymentProviderProductMetadata();
            return pspMeta?.hasWebshop ?? false;
        }

        /// Computes a "{N}% off" badge for the webshop option, comparing the
        /// listing's webshop price (from <see cref="PaymentProviderProductMetadata"/>)
        /// against its regular PSP price. Returns null when no webshop price is
        /// configured, when the regular price isn't cached, or when the webshop
        /// price isn't actually cheaper. Virtual for the same reason as HasWebshop.
        internal virtual string? ComputeWebshopBadge(string catalogListingId)
        {
            var product = ResolveProductService(PaymentProvider.Name).GetProductByCatalogListingId(catalogListingId);
            var metadata = product?.metadata;
            if (metadata == null)
                return null;
            var webshopPrice = metadata.GetPaymentProviderProductMetadata()?.localizedWebshopPrice;
            if (webshopPrice == null || webshopPrice.Value <= 0m)
                return null;
            var regularPrice = metadata.localizedPrice;
            if (regularPrice <= 0m || webshopPrice.Value >= regularPrice)
                return null;
            var savings = (regularPrice - webshopPrice.Value) / regularPrice * 100m;
            return $"{savings:F0}% off";
        }


        static StoreController ResolveController(string storeName)
        {
            if (storeName == IPaymentOptionProvider.NativeAlias)
                return new StoreController();

            if (IsKnownSdkStoreName(storeName))
                return new StoreController(storeName);

            // Unknown name — treat as a PSP-specific identifier (e.g. "stripe", "coda",
            // "mock"). Returns the base PaymentProvider controller; callers that need to
            // route a purchase to this specific provider use
            // IPaymentProvidersExtendedPurchaseService.PurchaseProduct(listing, name).
            return new StoreController(PaymentProvider.Name);
        }

        internal static string DisplayStoreName(string storeName)
        {
            if (string.Equals(storeName, IPaymentOptionProvider.NativeAlias, StringComparison.OrdinalIgnoreCase))
                return NativeDisplayLabel();
            if (string.Equals(storeName, PaymentProvider.Name, StringComparison.OrdinalIgnoreCase))
                return "Stripe";
            if (storeName == "AppleAppStore")
                return "App Store";
            if (storeName == "GooglePlay")
                return "Play Store";
            return storeName;
        }

        /// Computes a per-store "{N}% off" badge by comparing localizedPrice
        /// across store names for a single catalogListingId. Returns a dict keyed by
        /// every store name in input; values are null when no discount applies.
        /// Cheapest non-native store wins; native is never badged. Skipped
        /// silently when currencies differ, a product isn't cached yet, or
        /// fewer than two stores have valid prices.
        internal Dictionary<string, string?> ComputeAutoBadges(
            IReadOnlyList<string> storeNames,
            string catalogListingId)
        {
            var result = new Dictionary<string, string?>();
            if (storeNames == null)
                return result;

            foreach (var name in storeNames)
            {
                result[name] = null;
            }

            if (storeNames.Count < 2 || string.IsNullOrEmpty(catalogListingId))
            {
                return result;
            }

            var entries = new List<(string store, decimal price, string currency)>(storeNames.Count);
            foreach (var name in storeNames)
            {
                var info = GetPriceInfo(name, catalogListingId);
                if (info == null)
                    return result;

                entries.Add((name, info.Value.price, info.Value.currency));
            }

            var currency = entries[0].currency;
            if (entries.Any(e => e.currency != currency))
            {
                return result;
            }

            var cheapest = entries.OrderBy(e => e.price).First();
            var dearest = entries.OrderByDescending(e => e.price).First();
            if (cheapest.price >= dearest.price)
            {
                return result;
            }
            if (cheapest.store == IPaymentOptionProvider.NativeAlias)
            {
                return result;
            }

            var savings = (dearest.price - cheapest.price) / dearest.price * 100m;
            result[cheapest.store] = $"{savings:F0}% off";
            return result;
        }

        /// Build the default-order <see cref="PurchaseOption"/> list for the single-listing
        /// picker form: native + the first eligible PSP (from
        /// <see cref="GetDefaultPurchaseOrder"/>), filtered to stores that have the listing
        /// cached (see <see cref="FilterStoresWithListing"/>), with the cross-store discount
        /// badge from <see cref="ComputeAutoBadges"/> attached to the cheapest non-native
        /// store. Returns an empty list when no eligible store has the listing — callers
        /// should treat that as "nothing to render" (combine with <see cref="HasWebshop"/>
        /// to decide whether the picker can still open as webshop-only).
        internal async Task<IReadOnlyList<PurchaseOption>> BuildDefaultOptions(string catalogListingId)
        {
            var rawOrder = await GetDefaultPurchaseOrder();
            var order = FilterStoresWithListing(rawOrder, catalogListingId);
            if (order.Count == 0)
                return Array.Empty<PurchaseOption>();
            var badges = ComputeAutoBadges(order, catalogListingId);
            var options = new List<PurchaseOption>(order.Count);
            foreach (var storeName in order)
            {
                badges.TryGetValue(storeName, out var badge);
                options.Add(new PurchaseOption(storeName, catalogListingId, badge));
            }
            return options;
        }

        /// Filters a store-name list down to those whose local product cache
        /// actually contains <paramref name="catalogListingId"/>. Use this
        /// after <see cref="GetDefaultPurchaseOrder"/> to avoid rendering a
        /// picker button for a store that would fail with ProductUnavailable
        /// at purchase time (e.g. the listing isn't configured for that PSP).
        /// Stores with no product are dropped silently — verbose log only,
        /// not a warning, since unconfigured listings are a normal expected
        /// state across PSPs.
        internal virtual IReadOnlyList<string> FilterStoresWithListing(
            IReadOnlyList<string> stores, string catalogListingId)
        {
            var kept = new List<string>(stores.Count);
            foreach (var name in stores)
            {
                var product = ResolveProductService(name).GetProductByCatalogListingId(catalogListingId);
                if (product != null)
                {
                    kept.Add(name);
                }
                else
                {
                    Debug.unityLogger.LogIAPVerbose(
                        $"ListingNotConfigured: {DisplayStoreName(name)} has no product for {catalogListingId} — hiding option");
                }
            }
            return kept;
        }

        /// Default purchase order: ["native", firstEligiblePSP?]. Native is
        /// always present; the first PSP from GetEligiblePaymentProviders is
        /// appended when at least one is eligible AND the server hasn't killed
        /// the picker via PaymentOptionPopupEnabled=false (in which case we route
        /// to native only, which trips the single-store short-circuit downstream).
        internal static async Task<List<string>> GetDefaultPurchaseOrder()
        {
            var order = new List<string> { IPaymentOptionProvider.NativeAlias };
            var eligibility = await FetchEligibility();
            if (!eligibility.PaymentOptionPopupEnabled)
            {
                Debug.unityLogger.LogIAPVerbose("PopupDisabledByServer: routing to native only");
                return order;
            }
            var providers = eligibility.Providers;
            if (providers.Count > 0)
            {
                order.Add(providers[0]);
                Debug.unityLogger.LogIAPVerbose($"PspAvailable: eligible=[{string.Join(", ", providers)}] using {providers[0]}");
            }
            else
            {
                Debug.unityLogger.LogIAPVerbose("NoEligibleProviders: Falling back to native only");
            }
            return order;
        }

        /// Bundle of eligible PSP names + popup killswitch from the SDK's
        /// GetEligiblePaymentProviders. On error or unavailable extension,
        /// returns an empty-providers / popup-enabled default so the picker
        /// falls back to native-only without a hard failure.
        static async Task<EligiblePaymentProviders> FetchEligibility()
        {
            try
            {
                var ext = UnityIAPServices.Store(PaymentProvider.Name).PaymentProviders;
                if (ext == null)
                    return new EligiblePaymentProviders(Array.Empty<string>());

                return await ext.GetEligiblePaymentProviders()
                    ?? new EligiblePaymentProviders(Array.Empty<string>());
            }
            catch (Exception e)
            {
                Debug.unityLogger.LogIAPWarning($"EligibilityCheckFailed: {e.GetType().Name}: {e.Message}");
                return new EligiblePaymentProviders(Array.Empty<string>());
            }
        }

        internal static bool IsKnownSdkStoreName(string name) =>
            name == PaymentProvider.Name
                || name == XboxStore.Name
                || name == AppleAppStore.Name
                || name == GooglePlay.Name
                || name == MacAppStore.Name
                || name == FakeAppStore.Name;

        static string NativeDisplayLabel()
        {
            if (Application.isEditor) return "Mock Store";
            return Application.platform switch
            {
                RuntimePlatform.IPhonePlayer => "App Store",
                RuntimePlatform.tvOS => "App Store",
                RuntimePlatform.OSXPlayer => "App Store",
                RuntimePlatform.Android => "Play Store",
                _ => "Native"
            };
        }

        internal sealed class PurchaseOpt
        {
            public string             Label = "";
            public string?            Badge;
            public bool               IsNative;
            public bool               IsWebshop;
            public string             StoreName = "";
            public string             CatalogListingId = "";
            public PurchaseOption?    Option;
        }
    }
}
