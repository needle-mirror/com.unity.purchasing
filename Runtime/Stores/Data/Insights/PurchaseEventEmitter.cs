#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
// TODO ULO-10723: Remove Insights Module Enabled defines
#if UNITY_6000_5_OR_NEWER && IAP_INSIGHTS_MODULE_ENABLED
using Unity.EngineDiagnostics;
#endif
using UnityEngine.Networking;
using UnityEngine.Purchasing.PaymentProviderService;
using UnityEngine.Purchasing.Registration;
using UnityEngine.Purchasing.Stores.Data.Insights.Models;
using UnityEngine.Scripting;
using InsightsProductType = UnityEngine.Purchasing.Stores.Data.Insights.Models.ProductType;
using InsightsOwnershipType = UnityEngine.Purchasing.Stores.Data.Insights.Models.OwnershipType;
using InsightsDeviceInfo = UnityEngine.Purchasing.Stores.Data.Insights.Models.DeviceInfo;
using InsightsOrderData = UnityEngine.Purchasing.Stores.Data.Insights.Models.OrderData;
using InsightsEventType = UnityEngine.Purchasing.Stores.Data.Insights.Models.EventType;

namespace UnityEngine.Purchasing.Stores.Data.Insights
{
    // Concrete implementation of IPurchaseEventEmitter. Per Emit*Async call:
    //   1. Awaits IPlayerData to resolve identity / session / consent.
    //   2. Builds the proto-faithful Insights.Models.IAPSDKEvent from
    //      caller-provided SDK data (Product, transactionId, etc.) plus
    //      environment data (Application.cloudProjectId, platform,
    //      DeviceInfo from the platform-specific builders).
    //   3. Hands the IAPSDKEvent to PurchaseEventProtobufWriter for
    //      serialization to canonical proto3 binary wire format.
    //   4. Forwards the bytes — to the Insights gateway via HTTP POST on
    //      pre-6000.5 Unity, or to the runtime-module wrapper on 6000.5+
    //      (currently a Debug.Log stub pending the LogEvent API).
    //
    // Fields the SDK can't reasonably source on its own are left for the
    // runtime-module wrapper to augment on the wire:
    //   - event_uuid             (proto explicit: wrapper-generated)
    //
    // installation_timestamp (ULO-10536) is sourced via AppInstallInfo,
    // which mirrors the engine PR (ULO-10238, unity/unity #105897) when
    // the engine module is not available: Android PackageInfo.firstInstallTime
    // via JNI, iOS bundle directory creation date.
    internal sealed class PurchaseEventEmitter : IPurchaseEventEmitter
    {
        readonly IPlayerData m_PlayerData;
        readonly ICoreRegistryHelper m_CoreRegistry;
        readonly string m_StoreName;

        [Preserve]
        public PurchaseEventEmitter(IPlayerData playerData, ICoreRegistryHelper coreRegistry, IStoreWrapper storeWrapper)
        {
            m_PlayerData = playerData;
            m_CoreRegistry = coreRegistry;
            m_StoreName = storeWrapper.name;

            // Pre-warm install timestamp cache on the main thread (DI runs here).
            // BuildEnvelope is called from async Send*Event flows where the await
            // continuation could resume on a background thread, in which case the
            // Unity APIs used by AppInstallInfo (Application.persistentDataPath,
            // AndroidJavaObject) would throw UnityException, be caught, and
            // poison the cache with null.
            _ = AppInstallInfo.GetInstallTimestamp();
        }

        // Every public Send* method is fire-and-forget from the SDK's
        // perspective: telemetry must never break a purchase, so the entire
        // body is guarded. Any failure (player-data fetch, DeviceInfo native
        // call, writer, transport) is logged at verbose and swallowed so the
        // call always returns cleanly to PurchaseService.
        public async void SendPurchaseIntentStartEvent(ICart cart)
        {
            try
            {
                // Take any impression_id staged by a preceding PaymentOptionsShownEvent;
                // otherwise mint one for this journey. Single id for every item in
                // the cart — they all belong to the same purchase journey.
                var impressionId = ImpressionIdContext.TakeOrMint();
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                foreach (var item in cart.Items())
                {
                    if (item?.Product == null) continue;
                    var iaps = BuildEnvelope(item, null, pps, new PurchaseIntentStartEvent());
                    iaps.ImpressionId = impressionId;
                    Forward(iaps);
                }
            }
            catch (Exception e) { LogEmissionFailure(nameof(PurchaseIntentStartEvent), e); }
        }

        public async void SendPaymentOptionsShownEvent(IReadOnlyList<PaymentOption> optionsShown, string? defaultProvider)
        {
            try
            {
                var impressionId = ImpressionIdContext.Mint();
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                // No cart yet at modal-show time; envelope's Order stays null.
                var iaps = BuildEnvelope(null, null, pps, new PaymentOptionsShownEvent
                {
                    OptionsShown = new List<PaymentOption>(optionsShown),
                    OptionsDefaultProvider = defaultProvider
                });
                iaps.ImpressionId = impressionId;
                Forward(iaps);
            }
            catch (Exception e) { LogEmissionFailure(nameof(PaymentOptionsShownEvent), e); }
        }

        public async void SendPurchasePaidEvent(PendingOrder order, IPurchaseFulfilledPayload? payload)
        {
            try
            {
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                var insightsPayload = MapPayload(payload);
                foreach (var item in order.CartOrdered.Items())
                {
                    if (item?.Product == null) continue;
                    var iaps = BuildEnvelope(item, order.Info.TransactionID, pps, new PurchasePaidEvent
                    {
                        Payload = insightsPayload
                    });
                    Forward(iaps);
                }
            }
            catch (Exception e) { LogEmissionFailure(nameof(PurchasePaidEvent), e); }
        }

        public async void SendPurchaseFailedEvent(FailedOrder order)
        {
            try
            {
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                var variant = new PurchaseFailedEvent
                {
                    FailureReason = MapFailureReason(order.FailureReason),
                    FailureMessage = order.Details
                };
                foreach (var item in order.CartOrdered.Items())
                {
                    if (item?.Product == null) continue;
                    var iaps = BuildEnvelope(item, null, pps, variant);
                    Forward(iaps);
                }
            }
            catch (Exception e) { LogEmissionFailure(nameof(PurchaseFailedEvent), e); }
        }

        public async void SendPurchaseFulfilledEvent(ConfirmedOrder order, IPurchaseFulfilledPayload? payload)
        {
            try
            {
                var pps = await m_PlayerData.CreatePlayerIdentityAsync();
                var insightsPayload = MapPayload(payload);
                foreach (var item in order.CartOrdered.Items())
                {
                    if (item?.Product == null) continue;
                    var iaps = BuildEnvelope(item, order.Info.TransactionID, pps, new PurchaseFulfilledEvent
                    {
                        Payload = insightsPayload
                    });
                    Forward(iaps);
                }
            }
            catch (Exception e) { LogEmissionFailure(nameof(PurchaseFulfilledEvent), e); }
        }

        static void LogEmissionFailure(string variant, Exception e)
        {
            Debug.unityLogger.LogIAPVerbose($"Insights emission failed for {variant}: {e.Message}");
        }

        static IStorePayload? MapPayload(IPurchaseFulfilledPayload? payload)
        {
            switch (payload)
            {
                case ApplePurchaseFulfilledPayload apple:
                    return new AppStorePayload
                    {
                        AppReceipt = apple.AppReceipt,
                        JwsRepresentation = apple.JwsRepresentation,
                        OriginalTransactionId = apple.OriginalTransactionId,
                        AppAccountToken = apple.AppAccountToken,
                        OwnershipType = MapOwnership(apple.Ownership)
                    };
                case GooglePurchaseFulfilledPayload google:
                    return new GooglePlayPayload
                    {
                        OriginalJson = google.OriginalJson,
                        Signature = google.Signature
                    };
                default:
                    return null;
            }
        }

        const string k_InsightsIngestUrl = "https://prd.insights.analytics.unity3d.com/v1/ingest/producer";

        internal static void Forward(IAPSDKEvent iaps)
        {
            var body = PurchaseEventProtobufWriter.Write(iaps);

            // TODO: temporary script define: IAP_INSIGHTS_MODULE_ENABLED
            // There will be a more official feature flag coming soon that will replace this.
            // TODO ULO-10723: Remove Insights Module Enabled defines
#if UNITY_6000_5_OR_NEWER && IAP_INSIGHTS_MODULE_ENABLED
            ForwardModule(body);
#else
            ForwardGateway(body);
#endif
        }

#if UNITY_6000_5_OR_NEWER && IAP_INSIGHTS_MODULE_ENABLED
        static void ForwardModule(byte[] body)
        {
            // EngineDiagnostics.LogEvent takes a ReadOnlySpan<char>, so the
            // proto3 binary body is base64-encoded to fit a text channel
            // losslessly. The receiving side base64-decodes back to the same
            // bytes that ForwardGateway POSTs.
            EngineDiagnostics.LogEvent((int)InsightsEventType.IapSdk, Convert.ToBase64String(body));
        }
#endif

        static void ForwardGateway(byte[] body)
        {
            var request = new UnityWebRequest(k_InsightsIngestUrl, UnityWebRequest.kHttpVerbPOST)
            {
                uploadHandler = new UploadHandlerRaw(body),
                downloadHandler = new DownloadHandlerBuffer()
            };
            request.SetRequestHeader("Content-Type", "application/x-protobuf");
            request.SetRequestHeader("Accept", "application/x-protobuf");
            request.SetRequestHeader("X-Tenant", "iapSdk");
            request.SetRequestHeader("X-Schema-Name", "insights.producers.iapsdk.v1alpha1.IAPSDKEvent");
            request.SetRequestHeader("X-Schema-Version", "1");
            request.SetRequestHeader("X-Proto-Sdk-Version", "1");
            request.SetRequestHeader("X-SDK-Release-Version", IAPVersion.Current);

            var op = request.SendWebRequest();
            op.completed += _ =>
            {
                if (request.result != UnityWebRequest.Result.Success)
                {
                    Debug.unityLogger.LogError("IAPSDKEvent",
                        $"Insights ingest failed ({(long)request.responseCode}): {request.error}");
                }
                request.Dispose();
            };
        }

        // == Envelope construction (SDK + Unity APIs -> Insights model) ==

        IAPSDKEvent BuildEnvelope(
            CartItem? cartItem,
            string? transactionId,
            PaymentProviderService.Models.PlayerIdentity pps,
            IEventVariant variant)
        {
            var sdkDeviceInfo = BuildSdkDeviceInfo(Application.platform);
            return new IAPSDKEvent
            {
                // this uuid must stay the same between retries.
                EventUuid = Guid.NewGuid().ToString(),
                Timestamp = DateTime.UtcNow,
                SessionId = pps.SessionId,
                FirebaseSessionId = pps.UnityFirebaseSessionId,
                FirebaseAppId = pps.UnityFirebaseAppId,
                ProjectId = m_CoreRegistry.CloudProjectId ?? "",
                EnvironmentId = m_CoreRegistry.EnvironmentId ?? "",
                IapSdkVersion = IAPVersion.Current,
                EngineVersion = Application.unityVersion,
                UnityConsentStateAdsIntent = ParseConsent(pps.UnityConsentStateAdsIntent),
                UnityConsentStateAnalyticsIntent = ParseConsent(pps.UnityConsentStateAnalyticsIntent),
                UnityIdentities = MapIdentity(pps),
                DeviceInfo = MapDeviceInfo(sdkDeviceInfo),
                Reporting = new Reporting
                {
                    Platform = sdkDeviceInfo?.Platform ?? "",
                    AppBundleId = sdkDeviceInfo?.AppBundleID ?? Application.identifier
                },
                Store = DeriveStore(m_StoreName),
                Order = BuildOrder(cartItem, transactionId),
                EventData = variant,
                ApplicationVersion = Application.version,
                InstallationTimestamp = AppInstallInfo.GetInstallTimestamp(),
            };
        }

        internal static DeviceInfo? BuildSdkDeviceInfo(RuntimePlatform platform)
        {
            switch (platform)
            {
                case RuntimePlatform.IPhonePlayer:
                case RuntimePlatform.OSXPlayer:
                case RuntimePlatform.tvOS:
#if UNITY_VISIONOS
                case RuntimePlatform.VisionOS:
#endif
                    return AppleDeviceInfoBuilder.Build(new NativeStoreProvider().GetStorekit());
                case RuntimePlatform.Android:
                    return AndroidDeviceInfoBuilder.Build();
                default:
                    return null;
            }
        }

        // Map from the SDK's existing store name constants
        // (AppleAppStore.Name, etc.) to the Insights Store enum. The store
        // name is the source of truth
        // Note: Xbox / FakeAppStore / custom stores fall through to
        // Unspecified — extend here if their events need to be tracked.
        // STORE_WEBSHOP exists in the schema but has no SDK trigger yet.
        static Store DeriveStore(string storeName)
        {
            return storeName switch
            {
                AppleAppStore.Name or MacAppStore.Name => Store.AppStore,
                GooglePlay.Name                        => Store.GooglePlay,
                PaymentProvider.Name                   => Store.PaymentProvider,
                _                                      => Store.Unspecified
            };
        }

        // PlayerData / EndUserConsent surface consent as short lowercase
        // strings ("unspecified" / "granted" / "denied"). Map to proto enum.
        internal static ConsentState ParseConsent(string? s)
        {
            switch (s)
            {
                case "granted": return ConsentState.Granted;
                case "denied": return ConsentState.Denied;
                case "unspecified": return ConsentState.Unspecified;
                default: return ConsentState.Unspecified;
            }
        }

        // UnityAdsIdfi doesn't have an equivalent in PPS; would be populated
        // by the runtime-module wrapper from Unity Ads if needed.
        //
        // UserId (ULO-10535) is sourced from the IExternalUserId service
        // component in core package. On Unity 6000.6+ this routes through
        // the new Identifiers.userId API (per Arnaud's ULO-10301 + companion
        // operate-services-sdk #11509); on older Unity versions it falls
        // back to the existing UnityServices.ExternalUserId implementation.
        // Single read site, version-transparent.
        PlayerIdentity MapIdentity(PaymentProviderService.Models.PlayerIdentity pps) =>
            MapIdentity(pps, m_CoreRegistry);

        internal static PlayerIdentity MapIdentity(PaymentProviderService.Models.PlayerIdentity pps, ICoreRegistryHelper coreRegistry)
        {
            return new PlayerIdentity
            {
                UnityInstallationId = pps.UnityInstallationId,
                PlayerId = coreRegistry.PlayerId,
                UserId = coreRegistry.ExternalUserId ?? "",
                AnalyticsId = pps.UnityAnalyticsId,
                Idfa = pps.UnityIdfa,
                Gaid = pps.UnityGaid,
                Idfv = pps.UnityIdfv,
                AppInstanceId = pps.UnityAppInstanceId,
            };
        }

        internal static InsightsDeviceInfo? MapDeviceInfo(DeviceInfo? sdk)
        {
            if (sdk == null) return null;
            return new InsightsDeviceInfo
            {
                SystemLanguage = sdk.Language ?? "",
                LocaleList = sdk.LocaleList ?? new List<string>(),
                Model = sdk.DeviceModel ?? "",
                SystemBootTime = sdk.SystemBootTime?.ToString(CultureInfo.InvariantCulture) ?? "",
                OsVersion = sdk.OSVersion ?? "",
                TotalSpace = sdk.TotalSpace.HasValue && sdk.TotalSpace.Value > 0
                    ? (ulong)sdk.TotalSpace.Value
                    : 0UL
            };
        }

        static InsightsOrderData? BuildOrder(CartItem? cartItem, string? transactionId)
        {
            if (cartItem?.Product == null) return null;
            return new InsightsOrderData
            {
                Sku = BuildSku(cartItem),
                StoreTransactionId = transactionId
            };
        }

        static Sku BuildSku(CartItem cartItem)
        {
            var product = cartItem.Product;
            product.catalogListings.TryGetValue(cartItem.CatalogListingId, out var listing);
            var def = listing?.definition;
            var meta = listing?.metadata;
            return new Sku
            {
                SkuId = def?.storeSpecificId ?? "",
                ProductType = MapProductType(def?.type ?? ProductType.Unknown),
                LocalizedTitle = meta?.localizedTitle,
                LocalizedDescription = meta?.localizedDescription,
                LocalizedPriceString = meta?.localizedPriceString,
                PriceMicro = meta != null ? (long?)(meta.localizedPrice * 1_000_000m) : null,
                IsoCurrencyCode = meta?.isoCurrencyCode,
                Quantity = cartItem.Quantity
            };
        }

        // == SDK enum -> Insights enum mapping ==

        static InsightsProductType MapProductType(ProductType v)
        {
            switch (v)
            {
                case ProductType.Consumable: return InsightsProductType.Consumable;
                case ProductType.NonConsumable: return InsightsProductType.NonConsumable;
                case ProductType.Subscription: return InsightsProductType.Subscription;
                case ProductType.Unknown: return InsightsProductType.Unknown;
                default: return InsightsProductType.Unspecified;
            }
        }

        static FailureReason MapFailureReason(PurchaseFailureReason v)
        {
            switch (v)
            {
                case PurchaseFailureReason.PurchasingUnavailable: return FailureReason.PurchasingUnavailable;
                case PurchaseFailureReason.ExistingPurchasePending: return FailureReason.ExistingPurchasePending;
                case PurchaseFailureReason.ProductUnavailable: return FailureReason.ProductUnavailable;
                case PurchaseFailureReason.SignatureInvalid: return FailureReason.SignatureInvalid;
                case PurchaseFailureReason.UserCancelled: return FailureReason.UserCancelled;
                case PurchaseFailureReason.PaymentDeclined: return FailureReason.PaymentDeclined;
                case PurchaseFailureReason.DuplicateTransaction: return FailureReason.DuplicateTransaction;
                case PurchaseFailureReason.ValidationFailure: return FailureReason.ValidationFailure;
                case PurchaseFailureReason.StoreNotConnected: return FailureReason.StoreNotConnected;
                case PurchaseFailureReason.PurchaseMissing: return FailureReason.PurchaseMissing;
                case PurchaseFailureReason.Unknown: return FailureReason.Unknown;
                case PurchaseFailureReason.UserNotAuthenticated: return FailureReason.UserNotAuthenticated;
                case PurchaseFailureReason.NotSupported: return FailureReason.NotSupported;
                case PurchaseFailureReason.OrderCancelled: return FailureReason.OrderCancelled;
                case PurchaseFailureReason.OrderStateChanged: return FailureReason.OrderStateChanged;
                // FAILURE_REASON_UNSPECIFIED is a proto3 zero-value with no
                // SDK equivalent; fall back to Unknown for anything we don't
                // recognize.
                default: return FailureReason.Unknown;
            }
        }

        // store_payload.proto explicitly warns against casting — values are
        // intentionally misaligned so the proto3 zero is UNSPECIFIED. Translate
        // by name.
        static InsightsOwnershipType MapOwnership(OwnershipType v)
        {
            switch (v)
            {
                case OwnershipType.Purchased: return InsightsOwnershipType.Purchased;
                case OwnershipType.FamilyShared: return InsightsOwnershipType.FamilyShared;
                default: return InsightsOwnershipType.Unspecified;
            }
        }

    }
}
