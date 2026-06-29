#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Stores.Data.Insights.Models
{
    // Mirrors insights.producers.iapsdk.v1alpha1.iap_sdk_event.proto
    //
    // Top-level envelope. Proto-faithful; the writer constructs an instance
    // from the SDK-facing PurchaseEventEmitter and serializes it.
    internal sealed class IAPSDKEvent
    {
        public DateTime Timestamp { get; set; }                              // field 1
        public string EventUuid { get; set; } = "";                          // field 2  (must be set by SDK for gateway flow)
        public string? SessionId { get; set; }                               // field 3
        public string? FirebaseSessionId { get; set; }                       // field 4  (wrapper-supplied)
        public string ProjectId { get; set; } = "";                          // field 5
        public string EnvironmentId { get; set; } = "";                      // field 6
        public string IapSdkVersion { get; set; } = "";                      // field 7
        public string EngineVersion { get; set; } = "";                      // field 8
        public ConsentState UnityConsentStateAdsIntent { get; set; }         // field 9
        public ConsentState UnityConsentStateAnalyticsIntent { get; set; }   // field 10
        public PlayerIdentity? UnityIdentities { get; set; }                 // field 11
        public DeviceInfo? DeviceInfo { get; set; }                          // field 12
        public Reporting? Reporting { get; set; }                            // field 13
        public DateTime? InstallationTimestamp { get; set; }                 // field 14 (wrapper-supplied)
        public Store Store { get; set; }                                     // field 15
        public OrderData? Order { get; set; }                                // field 16
        public IEventVariant? EventData { get; set; }                        // oneof fields 17-20, 22-23
        public string ApplicationVersion { get; set; } = "";                 // field 21
        public string? ImpressionId { get; set; }                            // field 24  (wrapper-supplied, optional)
        public string? FirebaseAppId { get; set; }                           // field 100 (wrapper-supplied; mobilesdk_app_id from google-services.json)
    }

    // Marker for the `oneof event_data` in IAPSDKEvent.
    internal interface IEventVariant { }

    internal sealed class PurchaseIntentStartEvent : IEventVariant { }

    internal sealed class PurchasePaidEvent : IEventVariant
    {
        public IStorePayload? Payload { get; set; }
    }

    internal sealed class PurchaseFailedEvent : IEventVariant
    {
        public FailureReason FailureReason { get; set; }
        public string? FailureMessage { get; set; }
    }

    internal sealed class PurchaseFulfilledEvent : IEventVariant
    {
        public IStorePayload? Payload { get; set; }
    }

    internal sealed class AuthenticationCompleteEvent : IEventVariant { }

    internal sealed class PaymentOptionsShownEvent : IEventVariant
    {
        public List<PaymentOption> OptionsShown { get; set; } = new List<PaymentOption>();
        public string? OptionsDefaultProvider { get; set; }
    }

    internal enum FailureReason
    {
        Unspecified = 0,
        PurchasingUnavailable = 1,
        ExistingPurchasePending = 2,
        ProductUnavailable = 3,
        SignatureInvalid = 4,
        UserCancelled = 5,
        PaymentDeclined = 6,
        DuplicateTransaction = 7,
        ValidationFailure = 8,
        StoreNotConnected = 9,
        PurchaseMissing = 10,
        Unknown = 11,
        UserNotAuthenticated = 12,
        NotSupported = 13,
        OrderCancelled = 14,
        OrderStateChanged = 15
    }
}
