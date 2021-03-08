using System;
using System.Xml;
using System.Reflection;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Security;
using UnityEngine;

namespace UnityEngine.Purchasing {

    public class TimeSpanUnits {
        public double days;
        public int months;
        public int years;
        public TimeSpanUnits (double d, int m, int y) {
            this.days = d;
            this.months = m;
            this.years = y;
        }
    }

    public class SubscriptionManager {

        private string receipt;
        private string productId;
        private string intro_json;

        public static void UpdateSubscription(Product newProduct, Product oldProduct, string developerPayload, Action<Product, string> appleStore, Action<string, string> googleStore) {
            if (oldProduct.receipt == null) {
                Debug.Log("The product has not been purchased, a subscription can only be upgrade/downgrade when has already been purchased");
                return;
            }
            var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(oldProduct.receipt);
            if (receipt_wrapper == null || !receipt_wrapper.ContainsKey("Store") || !receipt_wrapper.ContainsKey("Payload")) {
                Debug.Log("The product receipt does not contain enough information");
                return;
            }
            var store = (string)receipt_wrapper ["Store"];
            var payload = (string)receipt_wrapper ["Payload"];

            if (payload != null ) {
                switch (store) {
                case "GooglePlay":
                    {
                        SubscriptionManager oldSubscriptionManager = new SubscriptionManager(oldProduct, null);
                        SubscriptionInfo oldSubscriptionInfo = null;
                        try {
                            oldSubscriptionInfo = oldSubscriptionManager.getSubscriptionInfo();
                        } catch (Exception e) {
                            Debug.unityLogger.Log("Error: the product that will be updated does not have a valid receipt", e);
                            return;
                        }
                        string newSubscriptionId = newProduct.definition.storeSpecificId;
                        googleStore(oldSubscriptionInfo.getSubscriptionInfoJsonString(), newSubscriptionId);
                        return;
                    }
                case "AppleAppStore":
                case "MacAppStore":
                    {
                        appleStore(newProduct, developerPayload);
                        return;
                    }
                default:
                    {
                        Debug.Log("This store does not support update subscriptions");
                        return;
                    }
                }
            }
        }

        public static void UpdateSubscriptionInGooglePlayStore(Product oldProduct, Product newProduct, Action<string, string> googlePlayUpdateCallback) {
            SubscriptionManager oldSubscriptionManager = new SubscriptionManager(oldProduct, null);
            SubscriptionInfo oldSubscriptionInfo = null;
            try {
                oldSubscriptionInfo = oldSubscriptionManager.getSubscriptionInfo();
            } catch (Exception e) {
                Debug.unityLogger.Log("Error: the product that will be updated does not have a valid receipt", e);
                return;
            }
            string newSubscriptionId = newProduct.definition.storeSpecificId;
            googlePlayUpdateCallback(oldSubscriptionInfo.getSubscriptionInfoJsonString(), newSubscriptionId);
        }

        public static void UpdateSubscriptionInAppleStore(Product newProduct, string developerPayload, Action<Product, string> appleStoreUpdateCallback) {
            appleStoreUpdateCallback(newProduct, developerPayload);
        }

        // the receipt is Unity IAP UnifiedReceipt
        public SubscriptionManager(Product product, string intro_json) {
            this.receipt = product.receipt;
            this.productId = product.definition.storeSpecificId;
            this.intro_json = intro_json;
        }

        public SubscriptionManager(string receipt, string id, string intro_json) {
            this.receipt = receipt;
            this.productId = id;
            this.intro_json = intro_json;
        }

        // parse the "payload" part to get the subscription
        // info from the platform based native receipt
        public SubscriptionInfo getSubscriptionInfo() {

            if (this.receipt != null) {
				var receipt_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(receipt);

                var validPayload = receipt_wrapper.TryGetValue("Payload", out var payloadAsObject);
                var validStore  = receipt_wrapper.TryGetValue("Store", out var storeAsObject);

                if (validPayload && validStore)
                {

                    var payload = payloadAsObject as string;
                    var store = storeAsObject as string;

                    switch (store) {
                    case GooglePlay.Name:
                        {
                            return getGooglePlayStoreSubInfo(payload);
                        }
                    case AppleAppStore.Name:
                    case MacAppStore.Name:
                        {
                            if (this.productId == null) {
                                throw new NullProductIdException();
                            }
                            return getAppleAppStoreSubInfo(payload, this.productId);
                        }
                    case AmazonApps.Name:
                        {
                            return getAmazonAppStoreSubInfo(this.productId);
                        }
                    default:
                        {
                            throw new StoreSubscriptionInfoNotSupportedException("Store not supported: " + store);
                        }
                    }
                }
            }

            throw new NullReceiptException();

        }

        private SubscriptionInfo getAmazonAppStoreSubInfo(string productId) {
            return new SubscriptionInfo(productId);
        }
        private SubscriptionInfo getAppleAppStoreSubInfo(string payload, string productId) {

            AppleReceipt receipt = null;

            var logger = UnityEngine.Debug.unityLogger;

            try {
                receipt = new AppleReceiptParser().Parse(Convert.FromBase64String(payload));
            } catch (ArgumentException e) {
				logger.Log ("Unable to parse Apple receipt", e);
            } catch (Security.IAPSecurityException e) {
				logger.Log ("Unable to parse Apple receipt", e);
            } catch (NullReferenceException e) {
				logger.Log ("Unable to parse Apple receipt", e);
            }

            List<AppleInAppPurchaseReceipt> inAppPurchaseReceipts = new List<AppleInAppPurchaseReceipt>();

            if (receipt != null && receipt.inAppPurchaseReceipts != null && receipt.inAppPurchaseReceipts.Length > 0) {
                foreach (AppleInAppPurchaseReceipt r in receipt.inAppPurchaseReceipts) {
                    if (r.productID.Equals(productId)) {
                        inAppPurchaseReceipts.Add(r);
                    }
                }
            }
            return inAppPurchaseReceipts.Count == 0 ? null : new SubscriptionInfo(findMostRecentReceipt(inAppPurchaseReceipts), this.intro_json);
        }

        private AppleInAppPurchaseReceipt findMostRecentReceipt(List<AppleInAppPurchaseReceipt> receipts) {
            receipts.Sort((b, a) => (a.purchaseDate.CompareTo(b.purchaseDate)));
            return receipts[0];
        }

        private SubscriptionInfo getGooglePlayStoreSubInfo(string payload)
        {
            var payload_wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(payload);
            var validSkuDetailsKey = payload_wrapper.TryGetValue("skuDetails", out var skuDetailsObject);

            string skuDetails = null;
            if (validSkuDetailsKey) skuDetails = skuDetailsObject as string;

            var purchaseHistorySupported = false;

            var original_json_payload_wrapper =
                (Dictionary<string, object>) MiniJson.JsonDecode((string) payload_wrapper["json"]);

            var validIsAutoRenewingKey =
                original_json_payload_wrapper.TryGetValue("autoRenewing", out var autoRenewingObject);

            var isAutoRenewing = false;
            if (validIsAutoRenewingKey) isAutoRenewing = (bool) autoRenewingObject;

            // Google specifies times in milliseconds since 1970.
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            var validPurchaseTimeKey =
                original_json_payload_wrapper.TryGetValue("purchaseTime", out var purchaseTimeObject);

            long purchaseTime = 0;

            if (validPurchaseTimeKey) purchaseTime = (long) purchaseTimeObject;

            var purchaseDate = epoch.AddMilliseconds(purchaseTime);

            var validDeveloperPayloadKey =
                original_json_payload_wrapper.TryGetValue("developerPayload", out var developerPayloadObject);

            var isFreeTrial = false;
            var hasIntroductoryPrice = false;
            string updateMetadata = null;

            if (validDeveloperPayloadKey)
            {
                var developerPayloadJSON = (string) developerPayloadObject;
                var developerPayload_wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(developerPayloadJSON);
                var validIsFreeTrialKey =
                    developerPayload_wrapper.TryGetValue("is_free_trial", out var isFreeTrialObject);
                if (validIsFreeTrialKey) isFreeTrial = (bool) isFreeTrialObject;

                var validHasIntroductoryPriceKey =
                    developerPayload_wrapper.TryGetValue("has_introductory_price_trial",
                        out var hasIntroductoryPriceObject);

                if (validHasIntroductoryPriceKey) hasIntroductoryPrice = (bool) hasIntroductoryPriceObject;

                var validIsUpdatedKey = developerPayload_wrapper.TryGetValue("is_updated", out var isUpdatedObject);

                var isUpdated = false;

                if (validIsUpdatedKey) isUpdated = (bool) isUpdatedObject;

                if (isUpdated)
                {
                    var isValidUpdateMetaKey = developerPayload_wrapper.TryGetValue("update_subscription_metadata",
                        out var updateMetadataObject);

                    if (isValidUpdateMetaKey) updateMetadata = (string) updateMetadataObject;
                }
            }

            return new SubscriptionInfo(skuDetails, isAutoRenewing, purchaseDate, isFreeTrial, hasIntroductoryPrice,
                purchaseHistorySupported, updateMetadata);
        }


    }

    public class SubscriptionInfo {
        private Result is_subscribed;
        private Result is_expired;
        private Result is_cancelled;
        private Result is_free_trial;
        private Result is_auto_renewing;
        private Result is_introductory_price_period;
        private string productId;
        private DateTime purchaseDate;
        private DateTime subscriptionExpireDate;
        private DateTime subscriptionCancelDate;
        private TimeSpan remainedTime;
        private string introductory_price;
        private TimeSpan introductory_price_period;
        private long introductory_price_cycles;

        private TimeSpan freeTrialPeriod;
        private TimeSpan subscriptionPeriod;

        // for test
        private string free_trial_period_string;
        private string sku_details;

        public SubscriptionInfo(AppleInAppPurchaseReceipt r, string intro_json) {

            var productType = (AppleStoreProductType) Enum.Parse(typeof(AppleStoreProductType), r.productType.ToString());

            if (productType == AppleStoreProductType.Consumable || productType == AppleStoreProductType.NonConsumable) {
                throw new InvalidProductTypeException();
            }

            if (!string.IsNullOrEmpty(intro_json)) {
                var intro_wrapper = (Dictionary<string, object>) MiniJson.JsonDecode(intro_json);
                var nunit = -1;
                var unit = SubscriptionPeriodUnit.NotAvailable;
                this.introductory_price = intro_wrapper.TryGetString("introductoryPrice") + intro_wrapper.TryGetString("introductoryPriceLocale");
                if (string.IsNullOrEmpty(this.introductory_price)) {
                    this.introductory_price = "not available";
                } else {
                    try {
                        this.introductory_price_cycles = Convert.ToInt64(intro_wrapper.TryGetString("introductoryPriceNumberOfPeriods"));
                        nunit = Convert.ToInt32(intro_wrapper.TryGetString("numberOfUnits"));
                        unit = (SubscriptionPeriodUnit)Convert.ToInt32(intro_wrapper.TryGetString("unit"));
                    } catch(Exception e) {
                        Debug.unityLogger.Log ("Unable to parse introductory period cycles and duration, this product does not have configuration of introductory price period", e);
                        unit = SubscriptionPeriodUnit.NotAvailable;
                    }
                }
                DateTime now = DateTime.Now;
                switch (unit) {
                    case SubscriptionPeriodUnit.Day:
                        this.introductory_price_period = TimeSpan.FromTicks(TimeSpan.FromDays(1).Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Month:
                        TimeSpan month_span = now.AddMonths(1) - now;
                        this.introductory_price_period = TimeSpan.FromTicks(month_span.Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Week:
                        this.introductory_price_period = TimeSpan.FromTicks(TimeSpan.FromDays(7).Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Year:
                        TimeSpan year_span = now.AddYears(1) - now;
                        this.introductory_price_period = TimeSpan.FromTicks(year_span.Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.NotAvailable:
                        this.introductory_price_period = TimeSpan.Zero;
                        this.introductory_price_cycles = 0;
                        break;
                }
            } else {
                this.introductory_price = "not available";
                this.introductory_price_period = TimeSpan.Zero;
                this.introductory_price_cycles = 0;
            }

            DateTime current_date = DateTime.UtcNow;
            this.purchaseDate = r.purchaseDate;
            this.productId = r.productID;

            this.subscriptionExpireDate = r.subscriptionExpirationDate;
            this.subscriptionCancelDate = r.cancellationDate;

            // if the product is non-renewing subscription, apple store will not return expiration date for this product
            if (productType == AppleStoreProductType.NonRenewingSubscription) {
                this.is_subscribed = Result.Unsupported;
                this.is_expired = Result.Unsupported;
                this.is_cancelled = Result.Unsupported;
                this.is_free_trial = Result.Unsupported;
                this.is_auto_renewing = Result.Unsupported;
                this.is_introductory_price_period = Result.Unsupported;
            } else {
                this.is_cancelled = (r.cancellationDate.Ticks > 0) && (r.cancellationDate.Ticks < current_date.Ticks) ? Result.True : Result.False;
                this.is_subscribed = r.subscriptionExpirationDate.Ticks >= current_date.Ticks ? Result.True : Result.False;
                this.is_expired = (r.subscriptionExpirationDate.Ticks > 0 && r.subscriptionExpirationDate.Ticks < current_date.Ticks) ? Result.True : Result.False;
                this.is_free_trial = (r.isFreeTrial == 1) ? Result.True : Result.False;
                this.is_auto_renewing = ((productType == AppleStoreProductType.AutoRenewingSubscription) && this.is_cancelled == Result.False
                        && this.is_expired == Result.False) ? Result.True : Result.False;
                this.is_introductory_price_period = r.isIntroductoryPricePeriod == 1 ? Result.True : Result.False;
            }

            if (this.is_subscribed == Result.True) {
                this.remainedTime = r.subscriptionExpirationDate.Subtract(current_date);
            } else {
                this.remainedTime = TimeSpan.Zero;
            }


        }

        public SubscriptionInfo(string skuDetails, bool isAutoRenewing, DateTime purchaseDate, bool isFreeTrial,
                bool hasIntroductoryPriceTrial, bool purchaseHistorySupported, string updateMetadata) {

            var skuDetails_wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(skuDetails);
            var validTypeKey = skuDetails_wrapper.TryGetValue("type", out var typeObject);

            if (!validTypeKey || (string)typeObject == "inapp") {
                throw new InvalidProductTypeException();
            }

            var validProductIdKey = skuDetails_wrapper.TryGetValue("productId", out var productIdObject);
            productId = null;
            if (validProductIdKey) productId = productIdObject as string;

		    this.purchaseDate = purchaseDate;
            this.is_subscribed = Result.True;
            this.is_auto_renewing = isAutoRenewing ? Result.True : Result.False;
            this.is_expired = Result.False;
            this.is_cancelled = isAutoRenewing ? Result.False : Result.True;
            this.is_free_trial = Result.False;


            string sub_period = null;
            if (skuDetails_wrapper.ContainsKey("subscriptionPeriod")) {
                sub_period = (string)skuDetails_wrapper["subscriptionPeriod"];
            }
            string free_trial_period = null;
            if (skuDetails_wrapper.ContainsKey("freeTrialPeriod")) {
                free_trial_period = (string)skuDetails_wrapper["freeTrialPeriod"];
            }
            string introductory_price = null;
            if (skuDetails_wrapper.ContainsKey("introductoryPrice")) {
                introductory_price = (string)skuDetails_wrapper["introductoryPrice"];
            }
            string introductory_price_period_string = null;
            if (skuDetails_wrapper.ContainsKey("introductoryPricePeriod")) {
                introductory_price_period_string = (string)skuDetails_wrapper["introductoryPricePeriod"];
            }
            long introductory_price_cycles = 0;
            if (skuDetails_wrapper.ContainsKey("introductoryPriceCycles")) {
                introductory_price_cycles = (long)skuDetails_wrapper["introductoryPriceCycles"];
            }

            // for test
            free_trial_period_string = free_trial_period;

            this.subscriptionPeriod = computePeriodTimeSpan(parsePeriodTimeSpanUnits(sub_period));

            this.freeTrialPeriod = TimeSpan.Zero;
            if (isFreeTrial) {
                this.freeTrialPeriod = parseTimeSpan(free_trial_period);
            }

            this.introductory_price = introductory_price;
            this.introductory_price_cycles = introductory_price_cycles;
            this.introductory_price_period = TimeSpan.Zero;
            this.is_introductory_price_period = Result.False;
            TimeSpan total_introductory_duration = TimeSpan.Zero;

            if (hasIntroductoryPriceTrial) {
                if (introductory_price_period_string != null && introductory_price_period_string.Equals(sub_period)) {
                    this.introductory_price_period = this.subscriptionPeriod;
                } else {
                    this.introductory_price_period = parseTimeSpan(introductory_price_period_string);
                }
                // compute the total introductory duration according to the introductory price period and period cycles
                total_introductory_duration = accumulateIntroductoryDuration(parsePeriodTimeSpanUnits(introductory_price_period_string), this.introductory_price_cycles);
            }

            // if this subscription is updated from other subscription, the remaining time will be applied to this subscription
            TimeSpan extra_time = TimeSpan.FromSeconds(updateMetadata == null ? 0.0 : computeExtraTime(updateMetadata, this.subscriptionPeriod.TotalSeconds));

            TimeSpan time_since_purchased = DateTime.UtcNow.Subtract(purchaseDate);


            // this subscription is still in the extra time (the time left by the previous subscription when updated to the current one)
            if (time_since_purchased <= extra_time) {
                // this subscription is in the remaining credits from the previous updated one
                this.subscriptionExpireDate = purchaseDate.Add(extra_time);
            } else if (time_since_purchased <= this.freeTrialPeriod.Add(extra_time)) {
                // this subscription is in the free trial period
                // this product will be valid until free trial ends, the beginning of next billing date
                this.is_free_trial = Result.True;
                this.subscriptionExpireDate = purchaseDate.Add(this.freeTrialPeriod.Add(extra_time));
            } else if (time_since_purchased < this.freeTrialPeriod.Add(extra_time).Add(total_introductory_duration)) {
                // this subscription is in the introductory price period
                this.is_introductory_price_period = Result.True;
                DateTime introductory_price_begin_date = this.purchaseDate.Add(this.freeTrialPeriod.Add(extra_time));
                this.subscriptionExpireDate = nextBillingDate(introductory_price_begin_date, parsePeriodTimeSpanUnits(introductory_price_period_string));
            } else {
                // no matter sub is cancelled or not, the expire date will be next billing date
                DateTime billing_begin_date = this.purchaseDate.Add(this.freeTrialPeriod.Add(extra_time).Add(total_introductory_duration));
                this.subscriptionExpireDate = nextBillingDate(billing_begin_date, parsePeriodTimeSpanUnits(sub_period));
            }

            this.remainedTime = this.subscriptionExpireDate.Subtract(DateTime.UtcNow);
            this.sku_details = skuDetails;
        }

        public SubscriptionInfo(string productId) {
            this.productId = productId;
            this.is_subscribed = Result.True;
            this.is_expired = Result.False;
            this.is_cancelled = Result.Unsupported;
            this.is_free_trial = Result.Unsupported;
            this.is_auto_renewing = Result.Unsupported;
            this.remainedTime = TimeSpan.MaxValue;
            this.is_introductory_price_period = Result.Unsupported;
            this.introductory_price_period = TimeSpan.MaxValue;
            this.introductory_price = null;
            this.introductory_price_cycles = 0;
        }


        public string getProductId() { return this.productId; }
        public DateTime getPurchaseDate() { return this.purchaseDate; }
        public Result isSubscribed() { return this.is_subscribed; }
        public Result isExpired() { return this.is_expired; }
        public Result isCancelled() { return this.is_cancelled; }
        public Result isFreeTrial() { return this.is_free_trial; }
        public Result isAutoRenewing() { return this.is_auto_renewing; }
        public TimeSpan getRemainingTime() { return this.remainedTime; }
        public Result isIntroductoryPricePeriod() { return this.is_introductory_price_period; }
        public TimeSpan getIntroductoryPricePeriod() { return this.introductory_price_period; }
        public string getIntroductoryPrice() { return string.IsNullOrEmpty(this.introductory_price) ? "not available" : this.introductory_price; }
        public long getIntroductoryPricePeriodCycles() { return this.introductory_price_cycles; }

        // these two dates are only for test Apple Store
        public DateTime getExpireDate() { return this.subscriptionExpireDate; }
        public DateTime getCancelDate() { return this.subscriptionCancelDate; }

        // these two are for test Google Play store
        public TimeSpan getFreeTrialPeriod() { return this.freeTrialPeriod; }
        public TimeSpan getSubscriptionPeriod() { return this.subscriptionPeriod; }
        public string getFreeTrialPeriodString() { return this.free_trial_period_string; }
        public string getSkuDetails() { return this.sku_details; }
        public string getSubscriptionInfoJsonString() {
            Dictionary<string, object> dict = new Dictionary<string, object>();
            dict.Add("productId", this.productId);
            dict.Add("is_free_trial", this.is_free_trial);
            dict.Add("is_introductory_price_period", this.is_introductory_price_period == Result.True);
            dict.Add("remaining_time_in_seconds", this.remainedTime.TotalSeconds);
            return MiniJson.JsonEncode(dict);
        }

        private DateTime nextBillingDate(DateTime billing_begin_date, TimeSpanUnits units) {

            if (units.days == 0.0 && units.months == 0 && units.years == 0) return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);

            DateTime next_billing_date = billing_begin_date;
            // find the next billing date that after the current date
            while (DateTime.Compare(next_billing_date, DateTime.UtcNow) <= 0) {

                next_billing_date = next_billing_date.AddDays(units.days).AddMonths(units.months).AddYears(units.years);
            }
            return next_billing_date;
        }

        private TimeSpan accumulateIntroductoryDuration(TimeSpanUnits units, long cycles) {
            TimeSpan result = TimeSpan.Zero;
            for (long i = 0; i < cycles; i++) {
                result = result.Add(computePeriodTimeSpan(units));
            }
            return result;
        }

        private TimeSpan computePeriodTimeSpan(TimeSpanUnits units) {
            DateTime now = DateTime.Now;
            return now.AddDays(units.days).AddMonths(units.months).AddYears(units.years).Subtract(now);
        }


        private double computeExtraTime(string metadata, double new_sku_period_in_seconds) {
            var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(metadata);
            long old_sku_remaining_seconds = (long)wrapper["old_sku_remaining_seconds"];
            long old_sku_price_in_micros = (long)wrapper["old_sku_price_in_micros"];

            double old_sku_period_in_seconds = (parseTimeSpan((string)wrapper["old_sku_period_string"])).TotalSeconds;
            long new_sku_price_in_micros = (long)wrapper["new_sku_price_in_micros"];
            double result = ((((double)old_sku_remaining_seconds / (double)old_sku_period_in_seconds ) * (double)old_sku_price_in_micros) / (double)new_sku_price_in_micros) * new_sku_period_in_seconds;
            return result;
        }

        private TimeSpan parseTimeSpan(string period_string) {
            TimeSpan result = TimeSpan.Zero;
            try {
                result = XmlConvert.ToTimeSpan(period_string);
            } catch(Exception) {
                if (period_string == null || period_string.Length == 0) {
                    result = TimeSpan.Zero;
                } else {
                    // .Net "P1W" is not supported and throws a FormatException
                    // not sure if only weekly billing contains "W"
                    // need more testing
                    result = new TimeSpan(7, 0, 0, 0);
                }
            }
            return result;
        }

        private TimeSpanUnits parsePeriodTimeSpanUnits(string time_span) {
            switch (time_span) {
            case "P1W":
                // weekly subscription
                return new TimeSpanUnits(7.0, 0, 0);
            case "P1M":
                // monthly subscription
                return new TimeSpanUnits(0.0, 1, 0);
            case "P3M":
                // 3 months subscription
                return new TimeSpanUnits(0.0, 3, 0);
            case "P6M":
                // 6 months subscription
                return new TimeSpanUnits(0.0, 6, 0);
            case "P1Y":
                // yearly subscription
                return new TimeSpanUnits(0.0, 0, 1);
            default:
                // seasonal subscription or duration in days
                return new TimeSpanUnits((double)parseTimeSpan(time_span).Days, 0, 0);
            }
        }


    }


    public enum Result {
        True,
        False,
        Unsupported,
    };

    public enum SubscriptionPeriodUnit {
        Day = 0,
        Month = 1,
        Week = 2,
        Year = 3,
        NotAvailable = 4,
    };

    enum AppleStoreProductType {
        NonConsumable = 0,
        Consumable = 1,
        NonRenewingSubscription = 2,
        AutoRenewingSubscription = 3,
    };

	public class ReceiptParserException : System.Exception {
		public ReceiptParserException() { }
		public ReceiptParserException(string message) : base(message) { }
	}

    public class InvalidProductTypeException : ReceiptParserException {}
    public class NullProductIdException : ReceiptParserException {}
    public class NullReceiptException : ReceiptParserException {}
    public class StoreSubscriptionInfoNotSupportedException : ReceiptParserException {
        public StoreSubscriptionInfoNotSupportedException (string message) : base (message) {
        }
    }
}
