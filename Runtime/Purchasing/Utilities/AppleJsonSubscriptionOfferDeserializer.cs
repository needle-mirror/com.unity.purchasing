#nullable enable
using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
// using Newtonsoft.Json;

namespace Purchasing.Utilities
{
    internal class AppleDataConverter
    {
        internal Dictionary<string, string> ConvertSubscriptionInfoMapToIntroOfferDictionary(
            Dictionary<string, IAppleSubscriptionInfo> infoMap)
        {
            var introOfferMap = new Dictionary<string, string>();

            foreach (var (key, value) in infoMap)
            {
                if (value?.IntroductoryOffer == null)
                {
                    continue;
                }

                var dto = ConvertOfferToIntroOfferDTO(value.IntroductoryOffer);
                var serialized = MiniJson.JsonEncode(dto);
                if (serialized != null)
                {
                    introOfferMap[key] = serialized;
                }
            }

            return introOfferMap;
        }

        internal AppleIntroOfferDTO? ConvertOfferToIntroOfferDTO(IAppleOffer? offer)
        {
            if (offer == null)
            {
                return null;
            }

            var dto = new AppleIntroOfferDTO
            {
                introductoryPrice = offer.DisplayPrice,
                introductoryPriceLocale = "", // Not available from StoreKit 2
                introductoryPriceNumberOfPeriods = offer.PeriodCount,
                numberOfUnits = offer.PeriodNumberOfUnits,
                unit = (int)(offer.PeriodUnit ?? SubscriptionPeriodUnit.NotAvailable)
            };
            return dto;
        }

        internal Dictionary<string, IAppleSubscriptionInfo> ConvertAppleProductDetailsResponseToSubscriptionInfoMap(AppleProductDetailsResponse appleProductDetailsResponse)
        {
            var subscriptionInfoMap = new Dictionary<string, IAppleSubscriptionInfo>();
            foreach (var (productId, productDetail) in appleProductDetailsResponse.productDetails)
            {
                if (productDetail.subscriptionInfo != null)
                {
                    var subscriptionInfo = ConvertAppleSubscriptionInfoResponseToSubscriptionInfo(productDetail.subscriptionInfo.Value);
                    subscriptionInfoMap[productId] = subscriptionInfo;
                }
            }
            return subscriptionInfoMap;
        }
        internal IAppleSubscriptionInfo ConvertAppleSubscriptionInfoResponseToSubscriptionInfo(AppleSubscriptionInfoResponse response)
        {
            var deserializer = new AppleJsonSubscriptionInfoDeserializer();
            return deserializer.AppleSubscriptionInfoResponseToSubscriptionInfo(response);
        }
    }

    internal class AppleJsonProductDetailsDeserializer
    {
        internal AppleProductDetailsResponse DeserializeAppleProductDetailsResponse(Dictionary<string, object> data)
        {
            var deserializer = new AppleJsonProductDetailDeserializer();

            var productDetails = new Dictionary<string, AppleProductDetailResponse>();
            foreach (var (key, value) in data)
            {
                if (value is Dictionary<string, object> productDetailDict)
                {
                    var productDetail = deserializer.DeserializeAppleProductDetailResponse(productDetailDict);
                    productDetails[key] = productDetail;
                }
            }

            return new AppleProductDetailsResponse(productDetails);
        }

        // internal AppleProductDetailsResponse DeserializeAppleProductDetailsResponse(string json)
        // {
        //     var response = JsonConvert.DeserializeObject<AppleProductDetailsResponse>(json);
        //     return response;
        // }

    }
    internal class AppleJsonProductDetailDeserializer
    {
        internal AppleProductDetailResponse DeserializeAppleProductDetailResponse(Dictionary<string, object> data)
        {
            AppleSubscriptionInfoResponse? subscriptionInfo = null;
            if (data.TryGetValue("subscriptionInfo", out var subscriptionInfoData) && subscriptionInfoData is Dictionary<string, object> subscriptionInfoDict)
            {
                var subscriptionInfoDeserializer = new AppleJsonSubscriptionInfoDeserializer();
                subscriptionInfo = subscriptionInfoDeserializer.DeserializeAppleSubscriptionInfoResponse(subscriptionInfoDict);
            }
            return new AppleProductDetailResponse(subscriptionInfo);
        }
    }
    internal class AppleJsonSubscriptionInfoDeserializer
    {
        internal AppleSubscriptionInfoResponse DeserializeAppleSubscriptionInfoResponse(Dictionary<string, object> data)
        {
            var introductoryOffer = GetIntroductoryOffer(data);
            var promotionalOffers = GetPromotionalOffers(data);

            return new AppleSubscriptionInfoResponse(introductoryOffer, promotionalOffers);
        }

        private AppleOfferInfoResponse? GetIntroductoryOffer(Dictionary<string, object> data)
        {
            if (data.TryGetValue("introductoryOffer", out var introOfferData) && introOfferData is Dictionary<string, object> introOfferDict)
            {
                var offerDeserializer = new AppleJsonSubscriptionOfferDeserializer();
                return offerDeserializer.DeserializeAppleOfferInfoResponse(introOfferDict);
            }
            return null;
        }

        private List<AppleOfferInfoResponse> GetPromotionalOffers(Dictionary<string, object> data)
        {
            var promotionalOffers = new List<AppleOfferInfoResponse>();
            if (data.TryGetValue("promotionalOffers", out var promoOffersData) && promoOffersData is List<object> promoOffersList)
            {
                var offerDeserializer = new AppleJsonSubscriptionOfferDeserializer();
                foreach (Dictionary<string, object> promoOffer in promoOffersList)
                {
                    var offerInfo = offerDeserializer.DeserializeAppleOfferInfoResponse(promoOffer);
                    promotionalOffers.Add(offerInfo);
                }
            }
            return promotionalOffers;
        }

        internal IAppleSubscriptionInfo AppleSubscriptionInfoResponseToSubscriptionInfo(AppleSubscriptionInfoResponse response)
        {
            var offerDeserializer = new AppleJsonSubscriptionOfferDeserializer();

            IAppleOffer? introductoryOffer = null;
            if (response.introductoryOffer != null)
            {
                introductoryOffer = offerDeserializer.AppleOfferInfoResponseToOffer(response.introductoryOffer.Value);
            }

            var promotionalOffers = new List<IAppleOffer>();
            foreach (var promoOfferInfo in response.promotionalOffers)
            {
                var promoOffer = offerDeserializer.AppleOfferInfoResponseToOffer(promoOfferInfo);
                promotionalOffers.Add(promoOffer);
            }

            return new AppleSubscriptionInfo(introductoryOffer, promotionalOffers.AsReadOnly());
        }
    }
    internal class AppleJsonSubscriptionOfferDeserializer
    {
        internal AppleOfferInfoResponse DeserializeAppleOfferInfoResponse(Dictionary<string, object> data)
        {
            var paymentMode = data.TryGetString("paymentMode");
            decimal? price = null;
            if (data.TryGetValue("price", out var priceData) && priceData is decimal priceValueDecimal)
            {
                price = priceValueDecimal;
            }
            var displayPrice = data.TryGetString("displayPrice");

            var periodUnit = data.TryGetString("period.unit");
            long? periodValue = null;
            if (data.TryGetValue("period.value", out var periodValueData) && periodValueData is long periodValueLong)
            {
                periodValue = periodValueLong;
            }
            long? periodCount = null;
            if (data.TryGetValue("periodCount", out var periodCountData) && periodCountData is long periodCountValueLong)
            {
                periodCount = periodCountValueLong;
            }

            var type = data.TryGetString("type");
            var id = data.TryGetString("id");

            return new AppleOfferInfoResponse()
            {
                paymentMode = paymentMode,
                price = price,
                displayPrice = displayPrice,
                periodUnit = periodUnit,
                periodValue = periodValue,
                periodCount = periodCount,
                type = type,
                id = id
            };
        }

        internal IAppleOffer AppleOfferInfoResponseToOffer(AppleOfferInfoResponse response)
        {
            var offerType = OfferTypeFromString(response.type);
            var id = response.id;

            var paymentMode = PaymentModeFromString(response.paymentMode);
            var price = response.price;
            var displayPrice = response.displayPrice;

            var periodUnit = ParseSubscriptionPeriodUnit(response.periodUnit);
            var periodValue = response.periodValue;
            var period = CreateTimeSpanFromApplePeriod(periodUnit, periodValue);
            var periodCount = Convert.ToInt32(response.periodCount);

            var periodNumberOfUnits = periodValue.HasValue ? Convert.ToInt32(periodValue.Value) : 0;

            return new AppleOffer(offerType, id, paymentMode, price, displayPrice, period, periodUnit, periodNumberOfUnits, periodCount);
        }

        internal SubscriptionPeriodUnit ParseSubscriptionPeriodUnit(string periodUnit)
        {
            switch (periodUnit)
            {
                case "Day":
                    return SubscriptionPeriodUnit.Day;
                case "Week":
                    return SubscriptionPeriodUnit.Week;
                case "Month":
                    return SubscriptionPeriodUnit.Month;
                case "Year":
                    return SubscriptionPeriodUnit.Year;
                default:
                    return SubscriptionPeriodUnit.NotAvailable;
            }
        }

        TimeSpanUnits? CreateTimeSpanFromApplePeriod(SubscriptionPeriodUnit periodUnit, long? periodValue)
        {
            if (periodValue == null || periodValue <= 0 || periodUnit == SubscriptionPeriodUnit.NotAvailable)
            {
                return null;
            }

            switch (periodUnit)
            {
                case SubscriptionPeriodUnit.Day:
                    return new TimeSpanUnits(Convert.ToDouble(periodValue), 0, 0);
                case SubscriptionPeriodUnit.Week:
                    return new TimeSpanUnits(Convert.ToDouble(periodValue) * 7, 0, 0);
                case SubscriptionPeriodUnit.Month:
                    return new TimeSpanUnits(0.0, Convert.ToInt32(periodValue), 0);
                case SubscriptionPeriodUnit.Year:
                    return new TimeSpanUnits(0.0, 0, Convert.ToInt32(periodValue));
                default:
                    return null;
            }
        }

        ApplePaymentMode PaymentModeFromString(string? paymentModeString)
        {
            switch (paymentModeString)
            {
                case "FreeTrial":
                    return ApplePaymentMode.FreeTrial;
                case "PayAsYouGo":
                    return ApplePaymentMode.PayAsYouGo;
                case "PayUpFront":
                    return ApplePaymentMode.PayUpFront;
                default:
                    return ApplePaymentMode.Unknown;
            }
        }

        AppleOfferType OfferTypeFromString(string? offerTypeString)
        {
            switch (offerTypeString)
            {
                case "IntroOffer":
                    return AppleOfferType.Introductory;
                // This shouldn't be a regular code offer,
                // as there is no way to get info about Code Offers from StoreKit 2 products.
                // Transactions can have code offer types,
                // but offer types on transactions use different string values.
                case "AdhocOffer":
                    return AppleOfferType.Promotional;
                case "Winback":
                    return AppleOfferType.WinBack;
                default:
                    return AppleOfferType.Unknown;
            }
        }
    }
}
