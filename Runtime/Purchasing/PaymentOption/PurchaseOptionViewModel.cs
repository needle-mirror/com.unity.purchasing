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

        // The language the picker shows: SetLocale's, else the device's as checkout reads it.
        internal string Locale => m_LocaleOverride ?? (m_DeviceLocale ??= PaymentOptionProviderTranslations.ResolveLocale(
            NativeDeviceLocale.Read().LocaleTagOrCurrentCulture));

        // Read on first use, when the picker draws on the main thread: the native read is a JNI call
        // on Android, and the strings file is a Resources.Load.
        string? m_DeviceLocale;

        string? m_LocaleOverride;

        // Null follows the device again; a language without a translation shows English.
        // Call from the main thread: matching the tag loads the strings file (Resources.Load).
        internal void SetLocale(string? localeTag)
        {
            if (localeTag == null)
            {
                m_LocaleOverride = null;
                return;
            }

            var match = PaymentOptionProviderTranslations.MatchLocale(localeTag);
            if (match == null)
            {
                Debug.unityLogger.LogIAPWarning(
                    $"UntranslatedLocale: the Purchase Options UI has no translation for '{localeTag}'; showing English. " +
                    $"Supported: {string.Join(", ", PaymentOptionProviderTranslations.SupportedLocales)}");
            }
            m_LocaleOverride = match ?? PaymentOptionProviderTranslations.Fallback;
        }

        // Analytics: the open window's impression_id, handed over by the provider at
        // show time and re-entered around the pick, which arrives in a click handler
        // outside the provider's scope. Set and cleared together with m_TcsOption.
        string? m_WindowImpressionId;

        // Service resolvers — overridden by tests to inject fakes.
        // Defaults resolve the store's own services through the providers, so the picker doesn't depend on the
        // public entry points (UnityIAPServices, StoreController) that wrap them.
        internal virtual IPurchaseService ResolvePurchaseService(string storeName)
            => ServiceStoreName(storeName) is { } name
                ? PurchaseServiceProvider.GetPurchaseService(name)
                : PurchaseServiceProvider.GetDefaultPurchaseService();

        internal virtual IProductService ResolveProductService(string storeName)
            => ServiceStoreName(storeName) is { } name
                ? ProductServiceProvider.GetProductService(name)
                : ProductServiceProvider.GetDefaultProductService();

        // Metadata of the requested listing, not the product's base listing: listings of one product
        // can carry different prices and webshop settings.
        ProductMetadata? GetListingMetadata(string storeName, string catalogListingId)
        {
            var product = ResolveProductService(storeName).GetProductByCatalogListingId(catalogListingId);
            return product != null && product.catalogListings.TryGetValue(catalogListingId, out var listing)
                ? listing.metadata
                : null;
        }

        // PSP purchase extension used by the webshop branch of CompleteChoice
        // to invoke RedirectToWebshop, and by the PSP-identifier branch of
        // PurchaseSelectedOption to invoke the per-call PurchaseProduct overload.
        // Distinct from FetchEligibility's IPaymentProvidersExtendedService reach
        // (that one owns the eligibility/picker-config surface); these methods
        // live on the purchase side. Virtual so tests can fake the extension.
        internal virtual IPaymentProvidersExtendedPurchaseService? ResolvePaymentProviders()
            => PurchaseServiceProvider.GetPurchaseService(PaymentProvider.Name).PaymentProviders;

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
            return ValidateAcrossStores(WithoutPaymentProvidersForSubscription(stores, catalogListingId), catalogListingId);
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
                if (IsSubscriptionOnPaymentProvider(opt.StoreName, opt.CatalogListingId))
                    return false;

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
            var metadata = GetListingMetadata(storeName, catalogListingId);
            if (metadata == null)
                return null;
            if (metadata.localizedPrice <= 0m)
                return null;
            var iso = metadata.isoCurrencyCode;
            if (string.IsNullOrEmpty(iso))
                return null;
            return (metadata.localizedPrice, iso);
        }

        public Task<PurchaseOption?> BeginShowPurchaseOption(
            IReadOnlyList<PurchaseOption> options,
            string? impressionId,
            out IReadOnlyList<PurchaseOpt> internalOptions)
        {
            CheckPreconditionsOption(options);

            options = WithoutPaymentProviderSubscriptionOptions(options);
            if (options.Count == 0)
            {
                Debug.unityLogger.LogIAPWarning("NoEligibleStore: every option is a subscription on a Payment Provider, which doesn't sell them; returning null");
                internalOptions = Array.Empty<PurchaseOpt>();
                return Task.FromResult<PurchaseOption?>(null);
            }

            if (options.Count == 1)
            {
                var only = options[0];
                Debug.unityLogger.LogIAPVerbose($"SkipPicker: single option {DisplayStoreName(only.StoreName)} / {only.CatalogListingId}; routing directly");
                PurchaseSelectedOption(only.StoreName, only.CatalogListingId);
                internalOptions = Array.Empty<PurchaseOpt>();
                return Task.FromResult<PurchaseOption?>(only);
            }

            internalOptions = CapToMaxButtons(BuildOptsFromOptions(options));
            m_TcsOption = new TaskCompletionSource<PurchaseOption?>();
            m_WindowImpressionId = impressionId;
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
            string? impressionId,
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
                // Webshop leads (spec mocks): webshop, then the payment provider(s), then native.
                opts.Insert(0, new PurchaseOpt
                {
                    IsWebshop = true,
                    Badge = webshopBadge,
                    StoreName = IPaymentOptionProvider.WebshopAlias,
                    CatalogListingId = catalogListingId,
                    Option = new PurchaseOption(
                        IPaymentOptionProvider.WebshopAlias,
                        catalogListingId,
                        webshopBadge),
                });
            }
            MoveNativeLast(opts);
            internalOptions = opts;
            m_TcsOption = new TaskCompletionSource<PurchaseOption?>();
            m_WindowImpressionId = impressionId;
            return m_TcsOption.Task;
        }

        // The picker leads with the webshop, then the payment provider(s), and keeps the
        // platform store at the bottom of the list (spec mocks). Applied here rather than in
        // GetDefaultPurchaseOrder because the webshop opt is added after that order is
        // resolved, so this is the only point where "last" is actually last.
        // BeginShowPurchaseOption is deliberately left alone: a cross-product caller
        // passes its own order and the SDK should not second-guess it.
        static void MoveNativeLast(List<PurchaseOpt> opts)
        {
            var nativeIndex = opts.FindIndex(o => o.IsNative);
            if (nativeIndex < 0 || nativeIndex == opts.Count - 1)
                return;
            var native = opts[nativeIndex];
            opts.RemoveAt(nativeIndex);
            opts.Add(native);
        }

        // Spec: "Max buttons = 3". The POP has no scroll, and RequiredPopHeight / the
        // fit-to-screen scale assume a short list. The single-listing picker can't exceed it
        // (native, the first eligible PSP, webshop); a cross-product caller can, so its first
        // three options, in its own order, are shown and the rest dropped with a warning.
        internal const int MaxButtons = 3;

        static IReadOnlyList<PurchaseOpt> CapToMaxButtons(IReadOnlyList<PurchaseOpt> opts)
        {
            if (opts.Count <= MaxButtons)
                return opts;
            var dropped = new List<string>(opts.Count - MaxButtons);
            for (var i = MaxButtons; i < opts.Count; i++)
                dropped.Add($"{DisplayStoreName(opts[i].StoreName)} / {opts[i].CatalogListingId}");
            Debug.unityLogger.LogIAPWarning(
                $"TooManyOptions: the purchase options UI shows at most {MaxButtons} options; {opts.Count} were provided. Not shown: {string.Join(", ", dropped)}");
            var kept = new List<PurchaseOpt>(MaxButtons);
            for (var i = 0; i < MaxButtons; i++)
                kept.Add(opts[i]);
            return kept;
        }

        // Virtual: test seam, so a test can see whether a click reached the ViewModel.
        public virtual async Task CompleteChoice(PurchaseOpt? choice)
        {
            if (m_Disposed)
                return;

            var tcs = m_TcsOption;
            if (tcs is null)
                throw new InvalidOperationException("No picker flow in progress");

            var impressionId = m_WindowImpressionId;
            m_TcsOption = null;
            m_WindowImpressionId = null;

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
                    using (EnterWindowImpression(impressionId))
                    {
                        var redirect = ResolvePaymentProviders()?.RedirectToWebshop(choice.CatalogListingId);
                        if (redirect is not null)
                            await redirect;
                    }
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
                using (EnterWindowImpression(impressionId))
                {
                    PurchaseSelectedOption(choice.StoreName, choice.CatalogListingId);
                }
            }
            catch (Exception ex)
            {
                tcs.TrySetException(new PurchaseChoiceFailedException(pick, ex));
                return;
            }

            tcs.TrySetResult(pick);
        }

        // Resolves a pending picker Task with null ("user cancelled"); no-op when nothing is
        // pending. For a view whose panel went away mid-choice: without it the caller's await
        // never returns and every later Show fails with "already in progress". Clears the
        // window's impression_id with the TCS, as CompleteChoice does.
        internal void CancelPendingChoice()
        {
            var tcs = m_TcsOption;
            m_TcsOption = null;
            m_WindowImpressionId = null;
            tcs?.TrySetResult(null);
        }

        // Analytics: the pick's purchase carries the window's impression_id. No id (a
        // window shown outside any scope) means no scope; the purchase's entry point then
        // mints its own.
        static IDisposable? EnterWindowImpression(string? impressionId)
            => impressionId != null ? ImpressionScope.Shared.Enter(impressionId) : null;

        public void Dispose()
        {
            if (m_Disposed) return;
            m_TcsOption?.TrySetResult(null);
            m_TcsOption = null;
            m_WindowImpressionId = null;
            m_Disposed = true;
        }

        internal IReadOnlyList<PurchaseOpt> BuildOptsFromOptions(IReadOnlyList<PurchaseOption> options)
        {
            var opts = new List<PurchaseOpt>(options.Count);
            foreach (var option in options)
            {
                opts.Add(new PurchaseOpt
                {
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
        /// flag is set and the listing isn't a subscription (the webshop sells through the
        /// Payment Provider backend, which doesn't sell subscriptions). Used by the providers
        /// to decide whether to render the webshop slot at all.
        internal bool HasWebshop(string catalogListingId)
            => !IsSubscriptionOnPaymentProvider(PaymentProvider.Name, catalogListingId)
                && HasWebshopFlag(catalogListingId);

        /// The listing's <see cref="PaymentProviderProductMetadata.hasWebshop"/> flag. Virtual
        /// so tests can stub the webshop branch without fabricating a full
        /// PaymentProviderProductMetadata.
        internal virtual bool HasWebshopFlag(string catalogListingId)
        {
            var pspMeta = GetListingMetadata(PaymentProvider.Name, catalogListingId)?.GetPaymentProviderProductMetadata();
            return pspMeta?.hasWebshop ?? false;
        }

        /// Computes a "{N}% off" badge for the webshop option, comparing the
        /// listing's webshop price (from <see cref="PaymentProviderProductMetadata"/>)
        /// against its regular PSP price. Returns null when no webshop price is
        /// configured, when the regular price isn't cached, or when the webshop
        /// price isn't actually cheaper. Virtual for the same reason as HasWebshopFlag.
        internal virtual string? ComputeWebshopBadge(string catalogListingId)
        {
            var metadata = GetListingMetadata(PaymentProvider.Name, catalogListingId);
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


        // The store whose services back a picker option: null (the default store) for the native alias, the store
        // itself for SDK store names. Any other name is a PSP-specific identifier (e.g. "stripe", "coda", "mock"),
        // backed by the PaymentProvider store; callers that route a purchase to that specific provider use
        // IPaymentProvidersExtendedPurchaseService.PurchaseProduct(listing, name).
        static string? ServiceStoreName(string storeName)
        {
            if (storeName == IPaymentOptionProvider.NativeAlias)
                return null;

            return IsKnownSdkStoreName(storeName) ? storeName : PaymentProvider.Name;
        }

        // True for the PaymentProvider store and every PSP-specific identifier it backs, and for the native
        // alias when the PaymentProvider store is the default store (UnityIAPServices.SetStoreAsDefault).
        static bool IsPaymentProviderOption(string storeName)
            => (ServiceStoreName(storeName) ?? DefaultStoreHelper.GetDefaultStoreName()) == PaymentProvider.Name;

        // Payment Providers sell consumables and non-consumables only: PaymentProviderImpl fails a
        // subscription purchase with ProductTypeNotSupported, so the picker never offers one.
        bool IsSubscriptionOnPaymentProvider(string storeName, string catalogListingId)
            => IsPaymentProviderOption(storeName)
                && ResolveProductService(storeName).GetProductByCatalogListingId(catalogListingId)?.type == ProductType.Subscription;

        IReadOnlyList<string> WithoutPaymentProvidersForSubscription(IReadOnlyList<string> stores, string catalogListingId)
            => stores.Where(name => !IsSubscriptionOnPaymentProvider(name, catalogListingId)).ToList();

        IReadOnlyList<PurchaseOption> WithoutPaymentProviderSubscriptionOptions(IReadOnlyList<PurchaseOption> options)
        {
            var kept = options.Where(o => !IsSubscriptionOnPaymentProvider(o.StoreName, o.CatalogListingId)).ToList();
            if (kept.Count < options.Count)
            {
                Debug.unityLogger.LogIAPVerbose(
                    $"SubscriptionNotSupported: hiding {options.Count - kept.Count} Payment Provider option(s) for subscriptions");
            }
            return kept;
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
            // The store's name in the logs (and SpecStoreName's fallback, which names the test
            // PSP itself). Spelled out so the logs read as the testing option.
            if (PaymentBrandRegistry.IsMockProvider(storeName))
                return "Mock Provider";
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
        internal virtual async Task<IReadOnlyList<PurchaseOption>> BuildDefaultOptions(string catalogListingId)
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
                if (product == null)
                {
                    Debug.unityLogger.LogIAPVerbose(
                        $"ListingNotConfigured: {DisplayStoreName(name)} has no product for {catalogListingId} — hiding option");
                }
                else if (IsPaymentProviderOption(name) && product.type == ProductType.Subscription)
                {
                    Debug.unityLogger.LogIAPVerbose(
                        $"SubscriptionNotSupported: {DisplayStoreName(name)} doesn't sell subscription {catalogListingId} — hiding option");
                }
                else
                {
                    kept.Add(name);
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
                var ext = StoreServiceProvider.GetStoreService(PaymentProvider.Name).PaymentProviders;
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
            public string?            Badge;
            public bool               IsNative;
            public bool               IsWebshop;
            public string             StoreName = "";
            public string             CatalogListingId = "";
            public PurchaseOption?    Option;
        }

        // What a button shows, so the providers only render it: which brand treatment it
        // gets (the key into PaymentOptionPopPalette for its background, text color and logo
        // tone), its text and its badge. Logo sprites stay with the providers — the sprite is
        // a Unity asset.
        //
        // Brand visuals are classified per PSP here, which Invariant 2 allows; nothing in
        // the flow or routing reads Kind.
        internal enum ButtonKind
        {
            // Text only: a store with no brand logo and no treatment of its own (a new PSP
            // without a registry entry, ...).
            Generic,
            Webshop,
            Apple,
            GooglePlay,
            Coda,
            Stripe,
            // A store with a logo in PaymentBrandRegistry but no brand treatment of its own:
            // its logo on the generic button. A new PSP gets this from its registry entry alone.
            Branded,
            // The native store when it has no lockup — the fake store in the editor and on
            // platforms without a store brand: text on the native store's colors.
            NativeText,
            // The test PSP ("mock"): text on gray, as in the mocks.
            MockProvider,
        }

        // English pop.button lead text, for tests.
        internal const string SpecLeadText = "Continue with";

        internal readonly struct ButtonDescriptor
        {
            public readonly ButtonKind Kind;
            // The whole text of a text-only button, and the fallback when a brand logo is missing.
            public readonly string Label;
            // Text before the brand logo, possibly empty; null for a text-only button.
            public readonly string? LeadText;
            public readonly string? TrailText;
            public readonly string? Badge;

            public ButtonDescriptor(ButtonKind kind, string label, string? leadText, string? badge, string? trailText = null)
            {
                Kind = kind;
                Label = label;
                LeadText = leadText;
                TrailText = trailText;
                Badge = badge;
            }

            // Whether the button shows a logo from PaymentBrandRegistry.
            public bool HasBrand => ShowsBrandLogo(Kind);

            // For a brand whose logo failed to load: rendered as a text-only button instead.
            public ButtonDescriptor WithoutBrand() => new ButtonDescriptor(ButtonKind.Generic, Label, null, Badge);
        }

        // Computed per call rather than stored on PurchaseOpt: "native" resolves to a
        // brand per platform (PaymentBrandRegistry), which the editor can override.
        internal static ButtonDescriptor DescribeButton(PurchaseOpt opt,
            string locale = PaymentOptionProviderTranslations.Fallback)
        {
            if (opt.IsWebshop)
                return new ButtonDescriptor(ButtonKind.Webshop,
                    PaymentOptionProviderTranslations.ButtonLabel(locale, DisplayStoreName(IPaymentOptionProvider.WebshopAlias)),
                    null, opt.Badge);

            var kind = PaymentBrandRegistry.IsApplePay(opt.StoreName) ? ButtonKind.Apple
                : PaymentBrandRegistry.IsGooglePay(opt.StoreName) ? ButtonKind.GooglePlay
                : PaymentBrandRegistry.IsCoda(opt.StoreName) ? ButtonKind.Coda
                : PaymentBrandRegistry.IsStripe(opt.StoreName) ? ButtonKind.Stripe
                : PaymentBrandRegistry.HasBrand(opt.StoreName) ? ButtonKind.Branded
                : PaymentBrandRegistry.IsMockProvider(opt.StoreName) ? ButtonKind.MockProvider
                : opt.IsNative ? ButtonKind.NativeText
                : ButtonKind.Generic;

            // The logo stands in for pop.button's {0}.
            var (lead, trail) = PaymentOptionProviderTranslations.ButtonParts(locale);

            // Every button reads "Continue with …": the logo after it, the native lockup
            // included, or the store's name.
            var logo = ShowsBrandLogo(kind);
            return new ButtonDescriptor(kind,
                PaymentOptionProviderTranslations.ButtonLabel(locale, SpecDisplayStoreName(opt.StoreName)),
                logo ? lead : null, opt.Badge, logo ? trail : null);
        }

        static bool ShowsBrandLogo(ButtonKind kind) => kind == ButtonKind.Apple || kind == ButtonKind.GooglePlay
            || kind == ButtonKind.Coda || kind == ButtonKind.Stripe || kind == ButtonKind.Branded;

        // The store's name after "Continue with", as the mocks write it; DisplayStoreName is
        // the one the logs use. A brand's name is only shown when its logo is missing, and then
        // reads as its logo does ("In-App Purchase" for Apple's lockup).
        internal static string SpecDisplayStoreName(string storeName)
        {
            if (!string.Equals(storeName, IPaymentOptionProvider.NativeAlias, StringComparison.OrdinalIgnoreCase))
                return SpecStoreName(storeName);

            // "native" shows the brand the registry resolves it to on this platform (or the
            // editor's override), so the label matches the lockup. Without one, it is named
            // after the store a purchase would actually go to, resolved per call like the
            // purchase itself: the fake store in the editor, the Xbox store on GDK, a custom
            // store set with DefaultStoreHelper.OverrideDefaultStoreName, ...
            var brandKey = PaymentBrandRegistry.ResolveNativeBrandKey();
            if (brandKey != null)
                return SpecStoreName(brandKey);
            var defaultStore = DefaultStoreHelper.GetDefaultStoreName();
            return defaultStore == FakeAppStore.Name ? "fake store" : SpecStoreName(defaultStore);
        }

        static string SpecStoreName(string storeName)
        {
            if (storeName == AppleAppStore.Name || storeName == MacAppStore.Name)
                return "In-App Purchase";
            if (storeName == GooglePlay.Name)
                return GooglePlay.DisplayName;
            if (storeName == XboxStore.Name)
                return XboxStore.DisplayName;
            if (PaymentBrandRegistry.IsStripe(storeName))
                return "Stripe";
            if (PaymentBrandRegistry.IsCoda(storeName))
                return "CODA";
            if (PaymentBrandRegistry.IsMockProvider(storeName))
                return "mock provider";
            return DisplayStoreName(storeName);
        }
    }
}
