#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
#if UNITY_INSIGHTS_REQUIREMENTS_API && ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS
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
                var pps = await m_PlayerData.CreatePlayerIdentityAsync(impressionId);
                foreach (var item in cart.Items())
                {
                    if (item?.Product == null) continue;
                    var iaps = BuildEnvelope(item, null, pps, new PurchaseIntentStartEvent());
                    Forward(iaps);
                }
            }
            catch (Exception)
            {
                // ignored
            }
        }

        public async void SendPaymentOptionsShownEvent(IReadOnlyList<PaymentOption> optionsShown, string? defaultProvider)
        {
            try
            {
                var impressionId = ImpressionIdContext.Mint();
                var pps = await m_PlayerData.CreatePlayerIdentityAsync(impressionId);
                // No cart yet at modal-show time; envelope's Order stays null.
                var iaps = BuildEnvelope(null, null, pps, new PaymentOptionsShownEvent
                {
                    OptionsShown = new List<PaymentOption>(optionsShown),
                    OptionsDefaultProvider = defaultProvider
                });
                Forward(iaps);
            }
            catch (Exception)
            {
                // ignored
            }
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
            catch (Exception)
            {
                // ignored
            }
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
            catch (Exception)
            {
                // ignored
            }
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
            catch (Exception)
            {
                // ignored
            }
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

            // UNITY_INSIGHTS_REQUIREMENTS_API is a versionDefine (Unity >= 6000.7.0a7):
            // those Editors provide the RequiresInsights attribute (declared in the
            // package's Editor assembly), which guarantees the Insights module is kept
            // in player builds, so events can go through the module.
            // ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS gates the Insights module
            // itself — it is set per-platform, so platforms without Insights (e.g.
            // Linux) fall back to POSTing to the Insights gateway directly.
#if UNITY_INSIGHTS_REQUIREMENTS_API && ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS
            ForwardModule(body);
#else
            ForwardGateway(body);
#endif
        }

#if UNITY_INSIGHTS_REQUIREMENTS_API && ENABLE_CLOUD_SERVICES_ENGINE_DIAGNOSTICS
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
                ImpressionId = pps.UnityImpressionId,
                InstallMode = GetInstallMode(Application.platform),
            };
        }

        // install_mode is Android-only for now; other platforms leave the
        // field unset so it stays absent on the wire (proto3 default).
        internal static string? GetInstallMode(RuntimePlatform platform)
        {
            return platform == RuntimePlatform.Android
                ? MapInstallMode(Application.installMode)
                : null;
        }

        internal static string? MapInstallMode(ApplicationInstallMode mode)
        {
            switch (mode)
            {
                case ApplicationInstallMode.Store: return "store";
                case ApplicationInstallMode.DeveloperBuild: return "dev_release";
                case ApplicationInstallMode.Adhoc: return "adhoc";
                case ApplicationInstallMode.Enterprise: return "enterprise";
                case ApplicationInstallMode.Editor: return "editor";
                default: return null;
            }
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
