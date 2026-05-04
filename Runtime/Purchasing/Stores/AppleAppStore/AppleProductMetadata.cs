using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
// using Newtonsoft.Json;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Product definition used by Apple App Store.
    /// This is a representation of Product
    /// <a href="https://developer.apple.com/documentation/storekit/product">Apple documentation</a>
    /// </summary>
    public class AppleProductMetadata : ProductMetadata
    {
        /// <summary>
        /// A Boolean value that indicates whether the product is available for family sharing in App Store Connect.
        /// </summary>
        public bool isFamilyShareable { get; }
        /// <summary>
        /// Additional information for Apple subscription products.
        /// </summary>
        IAppleSubscriptionInfo subscriptionInfo { get; }

        internal AppleProductMetadata(ProductMetadata baseProductMetadata, bool isFamilyShareable, IAppleSubscriptionInfo subscriptionInfo = null)
            : base(baseProductMetadata)
        {
            this.isFamilyShareable = isFamilyShareable;
            this.subscriptionInfo = subscriptionInfo;
        }

        internal AppleProductMetadata(string priceString, string title, string description, string currencyCode, decimal localizedPrice, bool isFamilyShareable, IAppleSubscriptionInfo subscriptionInfo = null)
            : base(priceString, title, description, currencyCode, localizedPrice)
        {
            this.isFamilyShareable = isFamilyShareable;
            this.subscriptionInfo = subscriptionInfo;
        }
    }

    internal interface IAppleSubscriptionInfo
    {
        public IAppleOffer IntroductoryOffer { get; }
        public ReadOnlyCollection<IAppleOffer> PromotionalOffers { get; }
    }

    internal class AppleSubscriptionInfo : IAppleSubscriptionInfo
    {
        public IAppleOffer IntroductoryOffer { get; }
        public ReadOnlyCollection<IAppleOffer> PromotionalOffers { get; }

        internal AppleSubscriptionInfo(IAppleOffer introductoryOffer, ReadOnlyCollection<IAppleOffer> promotionalOffers)
        {
            IntroductoryOffer = introductoryOffer;
            PromotionalOffers = promotionalOffers;
        }
    }

    internal struct AppleProductDetailsResponse
    {
        public Dictionary<string, AppleProductDetailResponse> productDetails;
        internal AppleProductDetailsResponse(Dictionary<string, AppleProductDetailResponse> productDetails)
        {
            this.productDetails = productDetails;
        }
    }

    internal struct AppleProductDetailResponse
    {
        public AppleSubscriptionInfoResponse? subscriptionInfo;
        internal AppleProductDetailResponse(AppleSubscriptionInfoResponse? subscriptionInfo)
        {
            this.subscriptionInfo = subscriptionInfo;
        }
    }

    internal struct AppleSubscriptionInfoResponse
    {
        // public string subscriptionGroupId;
        // public string subscriptionPeriodUnit;
        // public int subscriptionPeriodValue;
        public AppleOfferInfoResponse? introductoryOffer;
        public IList<AppleOfferInfoResponse> promotionalOffers;
        // public List<AppleOfferInfoResponse> winBackOffers;

        internal AppleSubscriptionInfoResponse(AppleOfferInfoResponse? introductoryOffer,
            IList<AppleOfferInfoResponse> promotionalOffers)
        {
            this.introductoryOffer = introductoryOffer;
            this.promotionalOffers = promotionalOffers;
        }
    }

    internal struct AppleOfferInfoResponse
    {
        public string paymentMode;
        public decimal? price;
        public string displayPrice;
        // [JsonProperty("period.unit")]
        public string periodUnit;
        // [JsonProperty("period.value")]
        public long? periodValue;
        public long? periodCount;
        public string type;
        public string id;
    }

    internal struct AppleIntroOfferDTO
    {
        public string introductoryPrice;
        public string introductoryPriceLocale; // empty from SK2
        public long? introductoryPriceNumberOfPeriods;
        public long? numberOfUnits;
        public long? unit;
    }

    internal interface IAppleOffer
    {
        public AppleOfferType OfferType { get; }
        public string Identifier { get; }
        public ApplePaymentMode PaymentMode { get; }
        public decimal? Price { get; }
        public string DisplayPrice { get; }
        public TimeSpanUnits Period { get; }
        public SubscriptionPeriodUnit? PeriodUnit { get; }
        public int? PeriodNumberOfUnits { get; }
        public int? PeriodCount { get; }
    }

    internal class AppleOffer : IAppleOffer
    {
        public AppleOfferType OfferType { get; }
        public string Identifier { get; }
        public ApplePaymentMode PaymentMode { get; }
        public decimal? Price { get; }
        public string DisplayPrice { get; }
        public TimeSpanUnits Period { get; }
        public SubscriptionPeriodUnit? PeriodUnit { get; }
        public int? PeriodNumberOfUnits { get; }
        public int? PeriodCount { get; }

        internal AppleOffer(AppleOfferType offerType, string identifier, ApplePaymentMode paymentMode, decimal? price, string displayPrice, TimeSpanUnits period, SubscriptionPeriodUnit? periodUnit, int? periodNumberOfUnits, int? periodCount)
        {
            OfferType = offerType;
            Identifier = identifier;
            PaymentMode = paymentMode;
            Price = price;
            DisplayPrice = displayPrice;
            Period = period;
            PeriodUnit = periodUnit;
            PeriodNumberOfUnits = periodNumberOfUnits;
            PeriodCount = periodCount;
        }
    }

    internal enum AppleOfferType
    {
        Introductory = 1,
        Promotional = 2,
        Code = 3,
        WinBack = 4,
        Unknown = -1
    }

    internal enum ApplePaymentMode
    {
        FreeTrial,
        PayAsYouGo,
        PayUpFront,
        Unknown
    }
}
