
using System;
using System.Collections.Generic;
using System.Xml;
using UnityEngine;
using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// A container for a Product's subscription-related information.
    /// </summary>
    /// <seealso cref="SubscriptionInfoHelper.GetSubscriptionInfo"/>
    public class SubscriptionInfo
    {
        readonly Result m_IsSubscribed;
        readonly Result m_IsExpired;
        readonly Result m_IsCancelled;
        readonly Result m_IsFreeTrial;
        readonly Result m_IsAutoRenewing;
        readonly Result m_IsIntroductoryPricePeriod;
        readonly string m_ProductId;
        readonly DateTime m_PurchaseDate;
        readonly DateTime m_SubscriptionExpireDate;
        readonly DateTime m_SubscriptionCancelDate;
        readonly TimeSpan m_RemainedTime;
        readonly string m_IntroductoryPrice;
        readonly TimeSpan m_IntroductoryPricePeriod;
        readonly long m_IntroductoryPriceCycles;

        readonly TimeSpan m_FreeTrialPeriod;
        readonly TimeSpan m_SubscriptionPeriod;

        // for test
        readonly string m_FreeTrialPeriodString;
        readonly string m_SKUDetails;

        /// <summary>
        /// Unpack Apple receipt subscription data.
        /// </summary>
        /// <param name="r">The Apple receipt from <typeparamref name="CrossPlatformValidator"/></param>
        /// <param name="introJson">From <typeparamref name="IAppleExtensions.GetIntroductoryPriceDictionary"/>. Keys:
        /// <c>introductoryPriceLocale</c>, <c>introductoryPrice</c>, <c>introductoryPriceNumberOfPeriods</c>, <c>numberOfUnits</c>,
        /// <c>unit</c>, which can be fetched from Apple's remote service.</param>
        /// <exception cref="InvalidProductTypeException">Error found involving an invalid product type.</exception>
        /// <seealso cref="CrossPlatformValidator"/>
        public SubscriptionInfo(AppleInAppPurchaseReceipt r, string introJson)
        {

            var productType = (AppleStoreProductType)Enum.Parse(typeof(AppleStoreProductType), r.productType.ToString());

            if (productType == AppleStoreProductType.Consumable || productType == AppleStoreProductType.NonConsumable)
            {
                throw new InvalidProductTypeException();
            }

            if (!string.IsNullOrEmpty(introJson))
            {
                var introWrapper = (Dictionary<string, object>)MiniJson.JsonDecode(introJson);
                var nunit = -1;
                var unit = SubscriptionPeriodUnit.NotAvailable;
                m_IntroductoryPrice = introWrapper.TryGetString("introductoryPrice") + introWrapper.TryGetString("introductoryPriceLocale");
                if (string.IsNullOrEmpty(m_IntroductoryPrice))
                {
                    m_IntroductoryPrice = "not available";
                }
                else
                {
                    try
                    {
                        m_IntroductoryPriceCycles = Convert.ToInt64(introWrapper.TryGetString("introductoryPriceNumberOfPeriods"));
                        nunit = Convert.ToInt32(introWrapper.TryGetString("numberOfUnits"));
                        unit = (SubscriptionPeriodUnit)Convert.ToInt32(introWrapper.TryGetString("unit"));
                    }
                    catch (Exception e)
                    {
                        Debug.unityLogger.LogIAP("Unable to parse introductory period cycles and duration, " +
                            $"this product does not have configuration of introductory price period: {e}");
                        unit = SubscriptionPeriodUnit.NotAvailable;
                    }
                }
                var now = DateTime.Now;
                switch (unit)
                {
                    case SubscriptionPeriodUnit.Day:
                        m_IntroductoryPricePeriod = TimeSpan.FromTicks(TimeSpan.FromDays(1).Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Month:
                        var monthSpan = now.AddMonths(1) - now;
                        m_IntroductoryPricePeriod = TimeSpan.FromTicks(monthSpan.Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Week:
                        m_IntroductoryPricePeriod = TimeSpan.FromTicks(TimeSpan.FromDays(7).Ticks * nunit);
                        break;
                    case SubscriptionPeriodUnit.Year:
                        var yearSpan = now.AddYears(1) - now;
                        m_IntroductoryPricePeriod = TimeSpan.FromTicks(yearSpan.Ticks * nunit);
                        break;
                    default:
                        m_IntroductoryPricePeriod = TimeSpan.Zero;
                        m_IntroductoryPriceCycles = 0;
                        break;
                }
            }
            else
            {
                m_IntroductoryPrice = "not available";
                m_IntroductoryPricePeriod = TimeSpan.Zero;
                m_IntroductoryPriceCycles = 0;
            }

            var currentDate = DateTime.UtcNow;
            m_PurchaseDate = r.purchaseDate;
            m_ProductId = r.productID;

            m_SubscriptionExpireDate = r.subscriptionExpirationDate;
            m_SubscriptionCancelDate = r.cancellationDate;

            // if the product is non-renewing subscription, apple store will not return expiration date for this product
            if (productType == AppleStoreProductType.NonRenewingSubscription)
            {
                m_IsSubscribed = Result.Unsupported;
                m_IsExpired = Result.Unsupported;
                m_IsCancelled = Result.Unsupported;
                m_IsFreeTrial = Result.Unsupported;
                m_IsAutoRenewing = Result.Unsupported;
                m_IsIntroductoryPricePeriod = Result.Unsupported;
            }
            else
            {
                m_IsCancelled = (r.cancellationDate.Ticks > 0) && (r.cancellationDate.Ticks < currentDate.Ticks) ? Result.True : Result.False;
                m_IsSubscribed = r.subscriptionExpirationDate.Ticks >= currentDate.Ticks ? Result.True : Result.False;
                m_IsExpired = (r.subscriptionExpirationDate.Ticks > 0 && r.subscriptionExpirationDate.Ticks < currentDate.Ticks) ? Result.True : Result.False;
                m_IsFreeTrial = (r.isFreeTrial == 1) ? Result.True : Result.False;
                m_IsAutoRenewing = ((productType == AppleStoreProductType.AutoRenewingSubscription) && m_IsCancelled == Result.False
                        && m_IsExpired == Result.False) ? Result.True : Result.False;
                m_IsIntroductoryPricePeriod = r.isIntroductoryPricePeriod == 1 ? Result.True : Result.False;
            }

            m_RemainedTime = m_IsSubscribed == Result.True ? r.subscriptionExpirationDate.Subtract(currentDate) : TimeSpan.Zero;
        }

        /// <summary>
        /// Especially crucial values relating to Google subscription products.
        /// Note this is intended to be called internally.
        /// </summary>
        /// <param name="skuDetails">The raw JSON from <c>SkuDetail.getOriginalJson</c></param>
        /// <param name="isAutoRenewing">Whether this subscription is expected to auto-renew</param>
        /// <param name="purchaseDate">A date this subscription was billed</param>
        /// <param name="isFreeTrial">Indicates whether this Product is a free trial</param>
        /// <param name="hasIntroductoryPriceTrial">Indicates whether this Product may be owned with an introductory price period.</param>
        /// <param name="purchaseHistorySupported">Unsupported</param>
        /// <param name="updateMetadata">Unsupported. Mechanism previously propagated subscription upgrade information to new subscription. </param>
        /// <exception cref="InvalidProductTypeException">For non-subscription product types. </exception>
        public SubscriptionInfo(string skuDetails, bool isAutoRenewing, DateTime purchaseDate, bool isFreeTrial,
                bool hasIntroductoryPriceTrial, bool purchaseHistorySupported, string updateMetadata)
        {
            var skuDetailsWrapper = (Dictionary<string, object>)MiniJson.JsonDecode(skuDetails);
            object typeObject = null;
            var validTypeKey = skuDetailsWrapper != null && skuDetailsWrapper.TryGetValue("type", out typeObject);

            if (!validTypeKey || (string)typeObject == "inapp")
            {
                throw new InvalidProductTypeException();
            }

            var validProductIdKey = skuDetailsWrapper.TryGetValue("productId", out var productIdObject);
            m_ProductId = null;
            if (validProductIdKey)
            {
                m_ProductId = productIdObject as string;
            }

            m_PurchaseDate = purchaseDate;
            m_IsSubscribed = Result.True;
            m_IsAutoRenewing = isAutoRenewing ? Result.True : Result.False;
            m_IsExpired = Result.False;
            m_IsCancelled = isAutoRenewing ? Result.False : Result.True;
            m_IsFreeTrial = Result.False;


            string subPeriod = null;
            if (skuDetailsWrapper.ContainsKey("subscriptionPeriod"))
            {
                subPeriod = (string)skuDetailsWrapper["subscriptionPeriod"];
            }
            string freeTrialPeriod = null;
            if (skuDetailsWrapper.ContainsKey("freeTrialPeriod"))
            {
                freeTrialPeriod = (string)skuDetailsWrapper["freeTrialPeriod"];
            }
            string introductoryPrice = null;
            if (skuDetailsWrapper.ContainsKey("introductoryPrice"))
            {
                introductoryPrice = (string)skuDetailsWrapper["introductoryPrice"];
            }
            string introductoryPricePeriodString = null;
            if (skuDetailsWrapper.ContainsKey("introductoryPricePeriod"))
            {
                introductoryPricePeriodString = (string)skuDetailsWrapper["introductoryPricePeriod"];
            }
            long introductoryPriceCycles = 0;
            if (skuDetailsWrapper.ContainsKey("introductoryPriceCycles"))
            {
                introductoryPriceCycles = (long)skuDetailsWrapper["introductoryPriceCycles"];
            }

            // for test
            m_FreeTrialPeriodString = freeTrialPeriod;

            m_SubscriptionPeriod = ComputePeriodTimeSpan(ParsePeriodTimeSpanUnits(subPeriod));

            m_FreeTrialPeriod = TimeSpan.Zero;
            if (isFreeTrial)
            {
                m_FreeTrialPeriod = ParseTimeSpan(freeTrialPeriod);
            }

            m_IntroductoryPrice = introductoryPrice;
            m_IntroductoryPriceCycles = introductoryPriceCycles;
            m_IntroductoryPricePeriod = TimeSpan.Zero;
            m_IsIntroductoryPricePeriod = Result.False;
            var totalIntroductoryDuration = TimeSpan.Zero;

            if (hasIntroductoryPriceTrial)
            {
                m_IntroductoryPricePeriod = introductoryPricePeriodString != null && introductoryPricePeriodString.Equals(subPeriod)
                    ? m_SubscriptionPeriod
                    : ParseTimeSpan(introductoryPricePeriodString);
                // compute the total introductory duration according to the introductory price period and period cycles
                totalIntroductoryDuration = AccumulateIntroductoryDuration(ParsePeriodTimeSpanUnits(introductoryPricePeriodString), m_IntroductoryPriceCycles);
            }

            // if this subscription is updated from other subscription, the remaining time will be applied to this subscription
            var extraTime = TimeSpan.FromSeconds(updateMetadata == null ? 0.0 : ComputeExtraTime(updateMetadata, m_SubscriptionPeriod.TotalSeconds));

            var timeSincePurchased = DateTime.UtcNow.Subtract(purchaseDate);


            // this subscription is still in the extra time (the time left by the previous subscription when updated to the current one)
            if (timeSincePurchased <= extraTime)
            {
                // this subscription is in the remaining credits from the previous updated one
                m_SubscriptionExpireDate = purchaseDate.Add(extraTime);
            }
            else if (timeSincePurchased <= m_FreeTrialPeriod.Add(extraTime))
            {
                // this subscription is in the free trial period
                // this product will be valid until free trial ends, the beginning of next billing date
                m_IsFreeTrial = Result.True;
                m_SubscriptionExpireDate = purchaseDate.Add(m_FreeTrialPeriod.Add(extraTime));
            }
            else if (timeSincePurchased < m_FreeTrialPeriod.Add(extraTime).Add(totalIntroductoryDuration))
            {
                // this subscription is in the introductory price period
                m_IsIntroductoryPricePeriod = Result.True;
                var introductoryPriceBeginDate = m_PurchaseDate.Add(m_FreeTrialPeriod.Add(extraTime));
                m_SubscriptionExpireDate = NextBillingDate(introductoryPriceBeginDate, ParsePeriodTimeSpanUnits(introductoryPricePeriodString));
            }
            else
            {
                // no matter sub is cancelled or not, the expire date will be next billing date
                var billingBeginDate = m_PurchaseDate.Add(m_FreeTrialPeriod.Add(extraTime).Add(totalIntroductoryDuration));
                m_SubscriptionExpireDate = NextBillingDate(billingBeginDate, ParsePeriodTimeSpanUnits(subPeriod));
            }

            m_RemainedTime = m_SubscriptionExpireDate.Subtract(DateTime.UtcNow);
            m_SKUDetails = skuDetails;
        }

        /// <summary>
        /// Especially crucial values relating to subscription products.
        /// Note this is intended to be called internally.
        /// </summary>
        /// <param name="productId">This subscription's product identifier</param>
        public SubscriptionInfo(string productId)
        {
            m_ProductId = productId;
            m_IsSubscribed = Result.True;
            m_IsExpired = Result.False;
            m_IsCancelled = Result.Unsupported;
            m_IsFreeTrial = Result.Unsupported;
            m_IsAutoRenewing = Result.Unsupported;
            m_RemainedTime = TimeSpan.MaxValue;
            m_IsIntroductoryPricePeriod = Result.Unsupported;
            m_IntroductoryPricePeriod = TimeSpan.MaxValue;
            m_IntroductoryPrice = null;
            m_IntroductoryPriceCycles = 0;
        }

        /// <summary>
        /// Store specific product identifier.
        /// </summary>
        /// <returns>The product identifier from the store receipt.</returns>
        public string GetProductId() { return m_ProductId; }

        /// <summary>
        /// Store specific product identifier.
        /// </summary>
        /// <returns>The product identifier from the store receipt.</returns>
        [Obsolete("getProductId is deprecated. Please use GetProductId instead.", false)]
        public string getProductId() { return GetProductId(); }

        /// <summary>
        /// A date this subscription was billed.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Apple, the purchase date is the date when the subscription was either purchased or renewed.
        /// For Google, the purchase date is the date when the subscription was originally purchased.
        /// </returns>
        public DateTime GetPurchaseDate() { return m_PurchaseDate; }

        /// <summary>
        /// A date this subscription was billed.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Apple, the purchase date is the date when the subscription was either purchased or renewed.
        /// For Google, the purchase date is the date when the subscription was originally purchased.
        /// </returns>
        [Obsolete("getPurchaseDate is deprecated. Please use GetPurchaseDate instead.", false)]
        public DateTime getPurchaseDate() { return GetPurchaseDate(); }

        /// <summary>
        /// Indicates whether this auto-renewable subscription Product is currently subscribed or not.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground. It is therefore recommended to remember
        /// subscription expiration state at app-launch, and ignore the fact that a subscription may expire later during
        /// this app launch runtime session.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Subscription status if the store receipt's expiration date is
        /// after the device's current time.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        /// <seealso cref="IsExpired"/>
        /// <seealso cref="DateTime.UtcNow"/>
        public Result IsSubscribed() { return m_IsSubscribed; }

        /// <summary>
        /// Indicates whether this auto-renewable subscription Product is currently subscribed or not.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground. It is therefore recommended to remember
        /// subscription expiration state at app-launch, and ignore the fact that a subscription may expire later during
        /// this app launch runtime session.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Subscription status if the store receipt's expiration date is
        /// after the device's current time.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        /// <seealso cref="IsExpired"/>
        /// <seealso cref="DateTime.UtcNow"/>
        [Obsolete("isSubscribed is deprecated. Please use IsSubscribed instead.", false)]
        public Result isSubscribed() { return IsSubscribed(); }

        /// <summary>
        /// Indicates whether this auto-renewable subscription Product is currently unsubscribed or not.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground. It is therefore recommended to remember
        /// subscription expiration state at app-launch, and ignore the fact that a subscription may expire later during
        /// this app launch runtime session.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Subscription status if the store receipt's expiration date is
        /// before the device's current time.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        /// <seealso cref="IsSubscribed"/>
        /// <seealso cref="DateTime.UtcNow"/>
        public Result IsExpired() { return m_IsExpired; }

        /// <summary>
        /// Indicates whether this auto-renewable subscription Product is currently unsubscribed or not.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground. It is therefore recommended to remember
        /// subscription expiration state at app-launch, and ignore the fact that a subscription may expire later during
        /// this app launch runtime session.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Subscription status if the store receipt's expiration date is
        /// before the device's current time.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        /// <seealso cref="IsSubscribed"/>
        /// <seealso cref="DateTime.UtcNow"/>
        [Obsolete("isExpired is deprecated. Please use IsExpired instead.", false)]
        public Result isExpired() { return IsExpired(); }

        /// <summary>
        /// Indicates whether this Product has been cancelled by Apple customer support or
        /// by upgrading an auto-renewable subscription plan.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Cancellation status if the store receipt's indicates this subscription is cancelled.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        public Result IsCancelled() { return m_IsCancelled; }

        /// <summary>
        /// Indicates whether this Product has been cancelled by Apple customer support or
        /// by upgrading an auto-renewable subscription plan.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> Cancellation status if the store receipt's indicates this subscription is cancelled.
        /// <typeparamref name="Result.False"/> otherwise.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        [Obsolete("isCancelled is deprecated. Please use IsCancelled instead.", false)]
        public Result isCancelled() { return IsCancelled(); }

        /// <summary>
        /// Indicates whether this Product is a free trial.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> This subscription is a free trial according to the store receipt.
        /// <typeparamref name="Result.False"/> This subscription is not a free trial according to the store receipt.
        /// Non-renewable subscriptions in the Apple store
        /// and Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        public Result IsFreeTrial() { return m_IsFreeTrial; }

        /// <summary>
        /// Indicates whether this Product is a free trial.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> This subscription is a free trial according to the store receipt.
        /// <typeparamref name="Result.False"/> This subscription is not a free trial according to the store receipt.
        /// Non-renewable subscriptions in the Apple store
        /// and Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        [Obsolete("isFreeTrial is deprecated. Please use IsFreeTrial instead.", false)]
        public Result isFreeTrial() { return IsFreeTrial(); }

        /// <summary>
        /// Indicates whether this Product is expected to auto-renew. The product must be auto-renewable, not canceled, and not expired.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> The store receipt's indicates this subscription is auto-renewing.
        /// <typeparamref name="Result.False"/> The store receipt's indicates this subscription is not auto-renewing.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        public Result IsAutoRenewing() { return m_IsAutoRenewing; }

        /// <summary>
        /// Indicates whether this Product is expected to auto-renew. The product must be auto-renewable, not canceled, and not expired.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> The store receipt's indicates this subscription is auto-renewing.
        /// <typeparamref name="Result.False"/> The store receipt's indicates this subscription is not auto-renewing.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        [Obsolete("isAutoRenewing is deprecated. Please use IsAutoRenewing instead.", false)]
        public Result isAutoRenewing() { return IsAutoRenewing(); }

        /// <summary>
        /// Indicates how much time remains until the next billing date.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground.
        /// </summary>
        /// <returns>
        /// A time duration from now until subscription billing occurs.
        /// Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return <typeparamref name="TimeSpan.MaxValue"/>.
        /// </returns>
        /// <seealso cref="DateTime.UtcNow"/>
        public TimeSpan GetRemainingTime() { return m_RemainedTime; }

        /// <summary>
        /// Indicates how much time remains until the next billing date.
        /// Note the store-specific behavior.
        /// Note also that the receipt may update and change this subscription expiration status if the user sends
        /// their iOS app to the background and then returns it to the foreground.
        /// </summary>
        /// <returns>
        /// A time duration from now until subscription billing occurs.
        /// Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return <typeparamref name="TimeSpan.MaxValue"/>.
        /// </returns>
        /// <seealso cref="DateTime.UtcNow"/>
        [Obsolete("getRemainingTime is deprecated. Please use GetRemainingTime instead.", false)]
        public TimeSpan getRemainingTime() { return GetRemainingTime(); }

        /// <summary>
        /// Indicates whether this Product is currently owned within an introductory price period.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> The store receipt's indicates this subscription is within its introductory price period.
        /// <typeparamref name="Result.False"/> The store receipt's indicates this subscription is not within its introductory price period.
        /// <typeparamref name="Result.False"/> If the product is not configured to have an introductory period.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        public Result IsIntroductoryPricePeriod() { return m_IsIntroductoryPricePeriod; }

        /// <summary>
        /// Indicates whether this Product is currently owned within an introductory price period.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// <typeparamref name="Result.True"/> The store receipt's indicates this subscription is within its introductory price period.
        /// <typeparamref name="Result.False"/> The store receipt's indicates this subscription is not within its introductory price period.
        /// <typeparamref name="Result.False"/> If the product is not configured to have an introductory period.
        /// Non-renewable subscriptions in the Apple store return a <typeparamref name="Result.Unsupported"/> value.
        /// Google subscriptions queried on devices with version lower than 6 of the Android in-app billing API return a <typeparamref name="Result.Unsupported"/> value.
        /// </returns>
        [Obsolete("isIntroductoryPricePeriod is deprecated. Please use IsIntroductoryPricePeriod instead.", false)]
        public Result isIntroductoryPricePeriod() { return IsIntroductoryPricePeriod(); }

        /// <summary>
        /// Indicates how much time remains for the introductory price period.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// Duration remaining in this product's introductory price period.
        /// Subscription products with no introductory price period return <typeparamref name="TimeSpan.Zero"/>.
        /// Products in the Apple store return <typeparamref name="TimeSpan.Zero"/> if the application does
        /// not support iOS version 11.2+, macOS 10.13.2+, or tvOS 11.2+.
        /// <typeparamref name="TimeSpan.Zero"/> returned also for products which do not have an introductory period configured.
        /// </returns>
        public TimeSpan GetIntroductoryPricePeriod() { return m_IntroductoryPricePeriod; }

        /// <summary>
        /// Indicates how much time remains for the introductory price period.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// Duration remaining in this product's introductory price period.
        /// Subscription products with no introductory price period return <typeparamref name="TimeSpan.Zero"/>.
        /// Products in the Apple store return <typeparamref name="TimeSpan.Zero"/> if the application does
        /// not support iOS version 11.2+, macOS 10.13.2+, or tvOS 11.2+.
        /// <typeparamref name="TimeSpan.Zero"/> returned also for products which do not have an introductory period configured.
        /// </returns>
        [Obsolete("getIntroductoryPricePeriod is deprecated. Please use GetIntroductoryPricePeriod instead.", false)]
        public TimeSpan getIntroductoryPricePeriod() { return GetIntroductoryPricePeriod(); }

        /// <summary>
        /// For subscriptions with an introductory price, get this price.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For subscriptions with a introductory price, a localized price string.
        /// For Google store the price may not include the currency symbol (e.g. $) and the currency code is available in <typeparamref name="ProductMetadata.isoCurrencyCode"/>.
        /// For all other product configurations, the string <c>"not available"</c>.
        /// </returns>
        /// <seealso cref="ProductMetadata.isoCurrencyCode"/>
        public string GetIntroductoryPrice() { return string.IsNullOrEmpty(m_IntroductoryPrice) ? "not available" : m_IntroductoryPrice; }

        /// <summary>
        /// For subscriptions with an introductory price, get this price.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For subscriptions with a introductory price, a localized price string.
        /// For Google store the price may not include the currency symbol (e.g. $) and the currency code is available in <typeparamref name="ProductMetadata.isoCurrencyCode"/>.
        /// For all other product configurations, the string <c>"not available"</c>.
        /// </returns>
        /// <seealso cref="ProductMetadata.isoCurrencyCode"/>
        [Obsolete("getIntroductoryPrice is deprecated. Please use GetIntroductoryPrice instead.", false)]
        public string getIntroductoryPrice() { return GetIntroductoryPrice(); }

        /// <summary>
        /// Indicates the number of introductory price billing periods that can be applied to this subscription Product.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// Products in the Apple store return <c>0</c> if the application does not support iOS version 11.2+, macOS 10.13.2+, or tvOS 11.2+.
        /// <c>0</c> returned also for products which do not have an introductory period configured.
        /// </returns>
        /// <seealso cref="intro"/>
        public long GetIntroductoryPricePeriodCycles() { return m_IntroductoryPriceCycles; }

        /// <summary>
        /// Indicates the number of introductory price billing periods that can be applied to this subscription Product.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// Products in the Apple store return <c>0</c> if the application does not support iOS version 11.2+, macOS 10.13.2+, or tvOS 11.2+.
        /// <c>0</c> returned also for products which do not have an introductory period configured.
        /// </returns>
        /// <seealso cref="intro"/>
        [Obsolete("getIntroductoryPricePeriodCycles is deprecated. Please use GetIntroductoryPricePeriodCycles instead.", false)]
        public long getIntroductoryPricePeriodCycles() { return GetIntroductoryPricePeriodCycles(); }

        /// <summary>
        /// When this auto-renewable receipt expires.
        /// </summary>
        /// <returns>
        /// An absolute date when this receipt will expire.
        /// </returns>
        public DateTime GetExpireDate() { return m_SubscriptionExpireDate; }

        /// <summary>
        /// When this auto-renewable receipt expires.
        /// </summary>
        /// <returns>
        /// An absolute date when this receipt will expire.
        /// </returns>
        [Obsolete("getExpireDate is deprecated. Please use GetExpireDate instead.", false)]
        public DateTime getExpireDate() { return GetExpireDate(); }

        /// <summary>
        /// When this auto-renewable receipt was canceled.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Apple store, the date when this receipt was canceled.
        /// For other stores this will be <c>null</c>.
        /// </returns>
        public DateTime GetCancelDate() { return m_SubscriptionCancelDate; }

        /// <summary>
        /// When this auto-renewable receipt was canceled.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Apple store, the date when this receipt was canceled.
        /// For other stores this will be <c>null</c>.
        /// </returns>
        [Obsolete("getCancelDate is deprecated. Please use GetCancelDate instead.", false)]
        public DateTime getCancelDate() { return GetCancelDate(); }

        /// <summary>
        /// The period duration of the free trial for this subscription, if enabled.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Google Play store if the product is configured with a free trial, this will be the period duration.
        /// For Apple store this will be <c> null </c>.
        /// </returns>
        public TimeSpan GetFreeTrialPeriod() { return m_FreeTrialPeriod; }

        /// <summary>
        /// The period duration of the free trial for this subscription, if enabled.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Google Play store if the product is configured with a free trial, this will be the period duration.
        /// For Apple store this will be <c> null </c>.
        /// </returns>
        [Obsolete("getFreeTrialPeriod is deprecated. Please use GetFreeTrialPeriod instead.", false)]
        public TimeSpan getFreeTrialPeriod() { return GetFreeTrialPeriod(); }

        /// <summary>
        /// The duration of this subscription.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// A duration this subscription is valid for.
        /// <typeparamref name="TimeSpan.Zero"/> returned for Apple products.
        /// </returns>
        public TimeSpan GetSubscriptionPeriod() { return m_SubscriptionPeriod; }

        /// <summary>
        /// The duration of this subscription.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// A duration this subscription is valid for.
        /// <typeparamref name="TimeSpan.Zero"/> returned for Apple products.
        /// </returns>
        [Obsolete("getSubscriptionPeriod is deprecated. Please use GetSubscriptionPeriod instead.", false)]
        public TimeSpan getSubscriptionPeriod() { return GetSubscriptionPeriod(); }

        /// <summary>
        /// The string representation of the period in ISO8601 format this subscription is free for.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Google Play store on configured subscription this will be the period which the can own this product for free, unless
        /// the user is ineligible for this free trial.
        /// For Apple store this will be <c> null </c>.
        /// </returns>
        public string GetFreeTrialPeriodString() { return m_FreeTrialPeriodString; }

        /// <summary>
        /// The string representation of the period in ISO8601 format this subscription is free for.
        /// Note the store-specific behavior.
        /// </summary>
        /// <returns>
        /// For Google Play store on configured subscription this will be the period which the can own this product for free, unless
        /// the user is ineligible for this free trial.
        /// For Apple store this will be <c> null </c>.
        /// </returns>
        [Obsolete("getFreeTrialPeriodString is deprecated. Please use GetFreeTrialPeriodString instead.", false)]
        public string getFreeTrialPeriodString() { return GetFreeTrialPeriodString(); }

        /// <summary>
        /// The raw JSON SkuDetails from the underlying Google API.
        /// Note the store-specific behavior.
        /// Note this is not supported.
        /// </summary>
        /// <returns>
        /// For Google store the <c> SkuDetails#getOriginalJson </c> results.
        /// For Apple this returns <c>null</c>.
        /// </returns>
        public string GetSkuDetails() { return m_SKUDetails; }

        /// <summary>
        /// The raw JSON SkuDetails from the underlying Google API.
        /// Note the store-specific behavior.
        /// Note this is not supported.
        /// </summary>
        /// <returns>
        /// For Google store the <c> SkuDetails#getOriginalJson </c> results.
        /// For Apple this returns <c>null</c>.
        /// </returns>
        [Obsolete("getSkuDetails is deprecated. Please use GetSkuDetails instead.", false)]
        public string getSkuDetails() { return GetSkuDetails(); }

        /// <summary>
        /// A JSON including a collection of data involving free-trial and introductory prices.
        /// Note the store-specific behavior.
        /// Used internally for subscription updating on Google store.
        /// </summary>
        /// <returns>
        /// A JSON with keys: <c>productId</c>, <c>is_free_trial</c>, <c>is_introductory_price_period</c>, <c>remaining_time_in_seconds</c>.
        /// </returns>
        /// <seealso cref="SubscriptionInfoHelper.UpdateSubscription"/>
        public string GetSubscriptionInfoJsonString()
        {
            var dict = new Dictionary<string, object>
            {
                { "productId", m_ProductId },
                { "is_free_trial", m_IsFreeTrial },
                { "is_introductory_price_period", m_IsIntroductoryPricePeriod == Result.True },
                { "remaining_time_in_seconds", m_RemainedTime.TotalSeconds }
            };
            return MiniJson.JsonEncode(dict);
        }

        /// <summary>
        /// A JSON including a collection of data involving free-trial and introductory prices.
        /// Note the store-specific behavior.
        /// Used internally for subscription updating on Google store.
        /// </summary>
        /// <returns>
        /// A JSON with keys: <c>productId</c>, <c>is_free_trial</c>, <c>is_introductory_price_period</c>, <c>remaining_time_in_seconds</c>.
        /// </returns>
        /// <seealso cref="SubscriptionInfoHelper.UpdateSubscription"/>
        [Obsolete("getSubscriptionInfoJsonString is deprecated. Please use GetSubscriptionInfoJsonString instead.", false)]
        public string getSubscriptionInfoJsonString() { return GetSubscriptionInfoJsonString(); }

        static DateTime NextBillingDate(DateTime billingBeginDate, TimeSpanUnits units)
        {

            if (units.days == 0.0 && units.months == 0 && units.years == 0)
            {
                return new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            }

            var nextBillingDate = billingBeginDate;
            // find the next billing date that after the current date
            while (DateTime.Compare(nextBillingDate, DateTime.UtcNow) <= 0)
            {

                nextBillingDate = nextBillingDate.AddDays(units.days).AddMonths(units.months).AddYears(units.years);
            }
            return nextBillingDate;
        }

        static TimeSpan AccumulateIntroductoryDuration(TimeSpanUnits units, long cycles)
        {
            var result = TimeSpan.Zero;
            for (long i = 0; i < cycles; i++)
            {
                result = result.Add(ComputePeriodTimeSpan(units));
            }
            return result;
        }

        static TimeSpan ComputePeriodTimeSpan(TimeSpanUnits units)
        {
            var now = DateTime.Now;
            return now.AddDays(units.days).AddMonths(units.months).AddYears(units.years).Subtract(now);
        }

        static double ComputeExtraTime(string metadata, double newSKUPeriodInSeconds)
        {
            var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(metadata);
            var oldSKURemainingSeconds = (long)wrapper["old_sku_remaining_seconds"];
            var oldSKUPriceInMicros = (long)wrapper["old_sku_price_in_micros"];

            var oldSKUPeriodInSeconds = ParseTimeSpan((string)wrapper["old_sku_period_string"]).TotalSeconds;
            var newSKUPriceInMicros = (long)wrapper["new_sku_price_in_micros"];
            var result = oldSKURemainingSeconds / oldSKUPeriodInSeconds * oldSKUPriceInMicros / newSKUPriceInMicros * newSKUPeriodInSeconds;
            return result;
        }

        static TimeSpan ParseTimeSpan(string periodString)
        {
            TimeSpan result;
            try
            {
                result = XmlConvert.ToTimeSpan(periodString);
            }
            catch (Exception)
            {
                if (string.IsNullOrEmpty(periodString))
                {
                    result = TimeSpan.Zero;
                }
                else
                {
                    // .Net "P1W" is not supported and throws a FormatException
                    // not sure if only weekly billing contains "W"
                    // need more testing
                    result = new TimeSpan(7, 0, 0, 0);
                }
            }
            return result;
        }

        static TimeSpanUnits ParsePeriodTimeSpanUnits(string timeSpan)
        {
            switch (timeSpan)
            {
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
                    return new TimeSpanUnits(ParseTimeSpan(timeSpan).Days, 0, 0);
            }
        }
    }

    /// <summary>
    /// For representing boolean values which may also be not available.
    /// </summary>
    public enum Result
    {
        /// <summary>
        /// Corresponds to boolean <c> true </c>.
        /// </summary>
        True,
        /// <summary>
        /// Corresponds to boolean <c> false </c>.
        /// </summary>
        False,
        /// <summary>
        /// Corresponds to no value, such as for situations where no result is available.
        /// </summary>
        Unsupported,
    }

    /// <summary>
    /// Used internally to parse Apple receipts. Corresponds to Apple SKProductPeriodUnit.
    /// </summary>
    /// <seealso cref="https://developer.apple.com/documentation/storekit/skproductperiodunit?language=objc"/>
    public enum SubscriptionPeriodUnit
    {
        /// <summary>
        /// An interval lasting one day.
        /// </summary>
        Day = 0,
        /// <summary>
        /// An interval lasting one week.
        /// </summary>
        Week = 1,
        /// <summary>
        /// An interval lasting one month.
        /// </summary>
        Month = 2,
        /// <summary>
        /// An interval lasting one year.
        /// </summary>
        Year = 3,
        /// <summary>
        /// Default value when no value is available.
        /// </summary>
        NotAvailable = 4,
    }

    enum AppleStoreProductType
    {
        NonConsumable = 0,
        Consumable = 1,
        NonRenewingSubscription = 2,
        AutoRenewingSubscription = 3,
    }
}
