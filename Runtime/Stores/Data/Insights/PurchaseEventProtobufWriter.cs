#nullable enable

using System;
using System.IO;
using System.Text;
using UnityEngine.Purchasing.Stores.Data.Insights.Models;
using InsightsProductType = UnityEngine.Purchasing.Stores.Data.Insights.Models.ProductType;
using InsightsOwnershipType = UnityEngine.Purchasing.Stores.Data.Insights.Models.OwnershipType;
using InsightsOrderData = UnityEngine.Purchasing.Stores.Data.Insights.Models.OrderData;
using InsightsDeviceInfo = UnityEngine.Purchasing.Stores.Data.Insights.Models.DeviceInfo;

namespace UnityEngine.Purchasing.Stores.Data.Insights
{
    // Serializes a proto-faithful Insights.Models.IAPSDKEvent to canonical
    // proto3 binary wire format. See
    // Schemas~/insights/producers/iapsdk/v1alpha1/iap_sdk_event.proto for
    // the schema this output must parse against.
    //
    // Proto3 wire conventions enforced here:
    //   - tag = (field_number << 3) | wire_type, encoded as a varint
    //   - wire types used: 0 (varint) for ints/enums, 2 (LEN) for strings/messages
    //   - default values omitted (empty string, 0, unspecified enum, empty list, null message)
    //   - google.protobuf.Timestamp: nested message { int64 seconds = 1, int32 nanos = 2 }
    //   - oneof: only the active variant is emitted
    //   - int32/int64: standard varint of the two's-complement bit pattern
    //     (no ZigZag — that's only for sintN, which the schema doesn't use)
    internal static class PurchaseEventProtobufWriter
    {
        const int WireVarint = 0;
        const int WireLen = 2;

        public static byte[] Write(IAPSDKEvent e)
        {
            using var ms = new MemoryStream(512);
            SerializeIaps(ms, e);
            return ms.ToArray();
        }

        static void SerializeIaps(Stream s, IAPSDKEvent e)
        {
            WriteTimestampField(s, 1, e.Timestamp);
            WriteOptionalString(s, 2, e.EventUuid);
            WriteOptionalString(s, 3, e.SessionId);
            WriteOptionalString(s, 5, e.ProjectId);
            WriteOptionalString(s, 6, e.EnvironmentId);
            WriteOptionalString(s, 7, e.IapSdkVersion);
            WriteOptionalString(s, 8, e.EngineVersion);

            if (e.UnityConsentStateAdsIntent != ConsentState.Unspecified)
            {
                WriteVarintField(s, 9, (long)e.UnityConsentStateAdsIntent);
            }
            if (e.UnityConsentStateAnalyticsIntent != ConsentState.Unspecified)
            {
                WriteVarintField(s, 10, (long)e.UnityConsentStateAnalyticsIntent);
            }

            if (e.UnityIdentities != null)
            {
                WriteMessageField(s, 11, sub => WritePlayerIdentity(sub, e.UnityIdentities!));
            }
            if (e.DeviceInfo != null)
            {
                WriteMessageField(s, 12, sub => WriteDeviceInfo(sub, e.DeviceInfo!));
            }
            if (e.Reporting != null)
            {
                WriteMessageField(s, 13, sub => WriteReporting(sub, e.Reporting!));
            }

            if (e.InstallationTimestamp.HasValue)
            {
                WriteTimestampField(s, 14, e.InstallationTimestamp.Value);
            }

            if (e.Store != Store.Unspecified)
            {
                WriteVarintField(s, 15, (long)e.Store);
            }

            if (e.Order != null)
            {
                WriteMessageField(s, 16, sub => WriteOrder(sub, e.Order!));
            }

            WriteEventVariant(s, e.EventData);

            WriteOptionalString(s, 21, e.ApplicationVersion);
            WriteOptionalString(s, 24, e.ImpressionId);
            WriteOptionalString(s, 100, e.FirebaseAppId);
        }

        static void WriteEventVariant(Stream s, IEventVariant? variant)
        {
            switch (variant)
            {
                case PurchaseIntentStartEvent:
                    WriteMessageField(s, 17, _ => { });
                    break;
                case PurchasePaidEvent paid:
                    WriteMessageField(s, 18, sub => WriteStorePayload(sub, paid.Payload));
                    break;
                case PurchaseFailedEvent failed:
                    WriteMessageField(s, 19, sub => WritePurchaseFailed(sub, failed));
                    break;
                case PurchaseFulfilledEvent fulfilled:
                    WriteMessageField(s, 20, sub => WriteStorePayload(sub, fulfilled.Payload));
                    break;
                case AuthenticationCompleteEvent:
                    WriteMessageField(s, 22, _ => { });
                    break;
                case PaymentOptionsShownEvent shown:
                    WriteMessageField(s, 23, sub => WritePaymentOptionsShown(sub, shown));
                    break;
            }
        }

        static void WritePaymentOptionsShown(Stream s, PaymentOptionsShownEvent e)
        {
            // Repeated field: emit every element. Unlike singular fields, the
            // default-value skip rule doesn't apply — consumers count elements
            // to learn the rendered combination, so silently dropping
            // PAYMENT_OPTION_UNSPECIFIED would corrupt the count.
            if (e.OptionsShown != null)
            {
                foreach (var option in e.OptionsShown)
                {
                    WriteVarintField(s, 1, (long)option);
                }
            }
            WriteOptionalString(s, 2, e.OptionsDefaultProvider);
        }

        static void WritePurchaseFailed(Stream s, PurchaseFailedEvent e)
        {
            if (e.FailureReason != FailureReason.Unspecified)
            {
                WriteVarintField(s, 1, (long)e.FailureReason);
            }
            WriteOptionalString(s, 2, e.FailureMessage);
        }

        // Both PurchasePaidEvent and PurchaseFulfilledEvent carry an identical
        // `oneof payload { AppStorePayload = 1; GooglePlayPayload = 2; }` —
        // emit the active variant or nothing.
        static void WriteStorePayload(Stream s, IStorePayload? payload)
        {
            switch (payload)
            {
                case AppStorePayload apple:
                    WriteMessageField(s, 1, sub => WriteAppStorePayload(sub, apple));
                    break;
                case GooglePlayPayload google:
                    WriteMessageField(s, 2, sub => WriteGooglePlayPayload(sub, google));
                    break;
            }
        }

        static void WriteAppStorePayload(Stream s, AppStorePayload p)
        {
            WriteOptionalString(s, 1, p.AppReceipt);
            WriteOptionalString(s, 2, p.JwsRepresentation);
            WriteOptionalString(s, 3, p.OriginalTransactionId);
            WriteOptionalString(s, 4, p.AppAccountToken);
            if (p.OwnershipType != InsightsOwnershipType.Unspecified)
            {
                WriteVarintField(s, 5, (long)p.OwnershipType);
            }
        }

        static void WriteGooglePlayPayload(Stream s, GooglePlayPayload p)
        {
            WriteOptionalString(s, 1, p.OriginalJson);
            WriteOptionalString(s, 2, p.Signature);
        }

        static void WriteOrder(Stream s, InsightsOrderData o)
        {
            if (o.Sku != null)
            {
                WriteMessageField(s, 1, sub => WriteSku(sub, o.Sku));
            }
            WriteOptionalString(s, 2, o.StoreTransactionId);
        }

        static void WriteSku(Stream s, Sku sku)
        {
            WriteOptionalString(s, 1, sku.SkuId);
            if (sku.ProductType != InsightsProductType.Unspecified)
            {
                WriteVarintField(s, 2, (long)sku.ProductType);
            }
            WriteOptionalString(s, 3, sku.LocalizedTitle);
            WriteOptionalString(s, 4, sku.LocalizedDescription);
            WriteOptionalString(s, 5, sku.LocalizedPriceString);
            if (sku.PriceMicro.HasValue)
            {
                WriteVarintField(s, 6, sku.PriceMicro.Value);
            }
            WriteOptionalString(s, 7, sku.IsoCurrencyCode);
            if (sku.Quantity != 0)
            {
                WriteVarintField(s, 8, sku.Quantity);
            }
        }

        static void WriteReporting(Stream s, Reporting r)
        {
            WriteOptionalString(s, 1, r.Platform);
            WriteOptionalString(s, 2, r.AppBundleId);
        }

        static void WriteDeviceInfo(Stream s, InsightsDeviceInfo d)
        {
            WriteOptionalString(s, 1, d.SystemLanguage);
            if (d.LocaleList != null && d.LocaleList.Count > 0)
            {
                foreach (var locale in d.LocaleList)
                {
                    WriteString(s, 2, locale ?? "");
                }
            }
            WriteOptionalString(s, 3, d.Model);
            WriteOptionalString(s, 4, d.SystemBootTime);
            WriteOptionalString(s, 5, d.OsVersion);
            if (d.TotalSpace != 0UL)
            {
                WriteUInt64Field(s, 6, d.TotalSpace);
            }
        }

        static void WritePlayerIdentity(Stream s, PlayerIdentity p)
        {
            WriteOptionalString(s, 1, p.UnityInstallationId);
            WriteOptionalString(s, 2, p.PlayerId);
            WriteOptionalString(s, 3, p.UserId);
            WriteOptionalString(s, 4, p.AnalyticsId);
            WriteOptionalString(s, 5, p.Idfa);
            WriteOptionalString(s, 6, p.Gaid);
            WriteOptionalString(s, 7, p.Idfv);
            WriteOptionalString(s, 8, p.UnityAdsIdfi);
            WriteOptionalString(s, 9, p.AppInstanceId);
        }

        // == Field-type helpers ==

        // google.protobuf.Timestamp wire format: nested message with
        // int64 seconds (field 1) and int32 nanos (field 2). Skip the inner
        // scalars when they're 0 per proto3 default-skip rules, but always
        // emit the outer message field so the timestamp's presence is
        // signalled (length-zero message body is valid).
        static void WriteTimestampField(Stream s, int fieldNumber, DateTime dt)
        {
            var utc = dt.Kind == DateTimeKind.Utc ? dt : dt.ToUniversalTime();
            var unixEpochTicks = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc).Ticks;
            var elapsedTicks = utc.Ticks - unixEpochTicks;
            var seconds = elapsedTicks / TimeSpan.TicksPerSecond;
            var subSecondTicks = elapsedTicks - seconds * TimeSpan.TicksPerSecond;
            var nanos = (int)(subSecondTicks * 100); // 100ns ticks → 1ns

            WriteMessageField(s, fieldNumber, sub =>
            {
                if (seconds != 0)
                {
                    WriteVarintField(sub, 1, seconds);
                }
                if (nanos != 0)
                {
                    WriteVarintField(sub, 2, nanos);
                }
            });
        }

        static void WriteOptionalString(Stream s, int fieldNumber, string? value)
        {
            if (string.IsNullOrEmpty(value)) return;
            WriteString(s, fieldNumber, value!);
        }

        static void WriteString(Stream s, int fieldNumber, string value)
        {
            var bytes = Encoding.UTF8.GetBytes(value);
            WriteTag(s, fieldNumber, WireLen);
            WriteVarint(s, (ulong)bytes.Length);
            s.Write(bytes, 0, bytes.Length);
        }

        static void WriteVarintField(Stream s, int fieldNumber, long value)
        {
            WriteTag(s, fieldNumber, WireVarint);
            WriteVarint(s, (ulong)value);
        }

        static void WriteUInt64Field(Stream s, int fieldNumber, ulong value)
        {
            WriteTag(s, fieldNumber, WireVarint);
            WriteVarint(s, value);
        }

        // Two-pass for length-prefixed messages: serialize body to a sub-
        // stream so we can prefix with its length. Keeps the writer simple
        // at the cost of one extra MemoryStream per nested message.
        static void WriteMessageField(Stream s, int fieldNumber, Action<Stream> writeBody)
        {
            using var sub = new MemoryStream();
            writeBody(sub);
            WriteTag(s, fieldNumber, WireLen);
            WriteVarint(s, (ulong)sub.Length);
            sub.WriteTo(s);
        }

        // == Primitive encoders ==

        static void WriteTag(Stream s, int fieldNumber, int wireType)
        {
            WriteVarint(s, ((ulong)fieldNumber << 3) | (uint)wireType);
        }

        static void WriteVarint(Stream s, ulong value)
        {
            while (value >= 0x80)
            {
                s.WriteByte((byte)((value & 0x7F) | 0x80));
                value >>= 7;
            }
            s.WriteByte((byte)value);
        }
    }
}
