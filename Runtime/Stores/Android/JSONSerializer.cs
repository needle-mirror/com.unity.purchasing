using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    static class SerializationExtensions
    {
        public static string TryGetString(this Dictionary<string, object> dic, string key)
        {
            if (dic.ContainsKey(key))
            {
                if (dic[key] != null)
                {
                    return dic[key].ToString();
                }
            }

            return null;
        }
    }

    internal class JSONSerializer
    {
        public static string SerializeProductDef(ProductDefinition product)
        {
            return MiniJson.JsonEncode(EncodeProductDef(product));
        }

        public static string SerializeProductDefs(IEnumerable<ProductDefinition> products)
        {
            var result = new List<object>();
            foreach (var product in products)
            {
                result.Add(EncodeProductDef(product));
            }

            return MiniJson.JsonEncode(result);
        }

        public static string SerializeProductDescs(ProductDescription product)
        {
            return MiniJson.JsonEncode(EncodeProductDesc(product));
        }

        public static string SerializeProductDescs(IEnumerable<ProductDescription> products)
        {
            var result = new List<object>();
            foreach (var product in products)
            {
                result.Add(EncodeProductDesc(product));
            }

            return MiniJson.JsonEncode(result);
        }

        public static Dictionary<string, string> DeserializeSubscriptionDescriptions(string json)
        {
            var objects = (List<object>)MiniJson.JsonDecode(json);
            var result = new Dictionary<string, string>();
            foreach (Dictionary<string, object> obj in objects)
            {
                var subscription = new Dictionary<string, string>();
                if (obj.TryGetValue("metadata", out var metadata))
                {
                    var metadataDict = (Dictionary<string, object>)metadata;
                    subscription["introductoryPrice"] = metadataDict.TryGetString("introductoryPrice");
                    subscription["introductoryPriceLocale"] = metadataDict.TryGetString("introductoryPriceLocale");
                    subscription["introductoryPriceNumberOfPeriods"] = metadataDict.TryGetString("introductoryPriceNumberOfPeriods");
                    subscription["numberOfUnits"] = metadataDict.TryGetString("numberOfUnits");
                    subscription["unit"] = metadataDict.TryGetString("unit");

                    // this is a double check for Apple side's bug
                    if (!string.IsNullOrEmpty(subscription["numberOfUnits"]) && string.IsNullOrEmpty(subscription["unit"]))
                    {
                        subscription["unit"] = "0";
                    }
                }
                else
                {
                    Debug.LogWarning("metadata key not found in subscription description json");
                }

                if (obj.TryGetValue("storeSpecificId", out var id))
                {
                    var idStr = (string)id;
                    result.Add(idStr, MiniJson.JsonEncode(subscription));
                }
                else
                {
                    Debug.LogWarning("storeSpecificId key not found in subscription description json");
                }
            }

            return result;
        }

        public static Dictionary<string, string> DeserializeProductDetails(string json)
        {
            var objects = (List<object>)MiniJson.JsonDecode(json);
            var result = new Dictionary<string, string>();
            foreach (Dictionary<string, object> obj in objects)
            {
                var details = new Dictionary<string, string>();
                if (obj.TryGetValue("metadata", out var metadata))
                {
                    var metadataStr = (Dictionary<string, object>)metadata;
                    details["subscriptionNumberOfUnits"] = metadataStr.TryGetString("subscriptionNumberOfUnits");
                    details["subscriptionPeriodUnit"] = metadataStr.TryGetString("subscriptionPeriodUnit");
                    details["localizedPrice"] = metadataStr.TryGetString("localizedPrice");
                    details["isoCurrencyCode"] = metadataStr.TryGetString("isoCurrencyCode");
                    details["localizedPriceString"] = metadataStr.TryGetString("localizedPriceString");
                    details["localizedTitle"] = metadataStr.TryGetString("localizedTitle");
                    details["localizedDescription"] = metadataStr.TryGetString("localizedDescription");
                    details["introductoryPrice"] = metadataStr.TryGetString("introductoryPrice");
                    details["introductoryPriceLocale"] = metadataStr.TryGetString("introductoryPriceLocale");
                    details["introductoryPriceNumberOfPeriods"] = metadataStr.TryGetString("introductoryPriceNumberOfPeriods");
                    details["numberOfUnits"] = metadataStr.TryGetString("numberOfUnits");
                    details["unit"] = metadataStr.TryGetString("unit");

                    // this is a double check for Apple side's bug
                    if (!string.IsNullOrEmpty(details["subscriptionNumberOfUnits"]) && string.IsNullOrEmpty(details["subscriptionPeriodUnit"]))
                    {
                        details["subscriptionPeriodUnit"] = "0";
                    }

                    // this is a double check for Apple side's bug
                    if (!string.IsNullOrEmpty(details["numberOfUnits"]) && string.IsNullOrEmpty(details["unit"]))
                    {
                        details["unit"] = "0";
                    }
                }
                else
                {
                    Debug.LogWarning("metadata key not found in product details json");
                }

                if (obj.TryGetValue("storeSpecificId", out var id))
                {
                    var idStr = (string)id;
                    result.Add(idStr, MiniJson.JsonEncode(details));
                }
                else
                {
                    Debug.LogWarning("storeSpecificId key not found in product details json");
                }
            }

            return result;
        }

        public static PurchaseFailureDescription DeserializeFailureReason(string json)
        {
            var dic = (Dictionary<string, object>)MiniJson.JsonDecode(json);
            var reason = PurchaseFailureReason.Unknown;

            if (dic.TryGetValue("reason", out var reasonStr))
            {
                if (Enum.IsDefined(typeof(PurchaseFailureReason), (string)reasonStr))
                {
                    reason = (PurchaseFailureReason)Enum.Parse(typeof(PurchaseFailureReason), (string)reasonStr);
                }

                if (dic.TryGetValue("productId", out var productId))
                {
                    return new PurchaseFailureDescription((string)productId, reason, BuildPurchaseFailureDescriptionMessage(dic));
                }
            }
            else
            {
                Debug.LogWarning("Reason key not found in purchase failure json: " + json);
            }

            return new PurchaseFailureDescription("Unknown ProductID", reason, BuildPurchaseFailureDescriptionMessage(dic));
        }

        static string BuildPurchaseFailureDescriptionMessage(Dictionary<string, object> dic)
        {
            var message = dic.TryGetString("message");
            var storeSpecificErrorCode = dic.TryGetString("storeSpecificErrorCode");

            if (message == null && storeSpecificErrorCode == null)
            {
                return null;
            }

            if (storeSpecificErrorCode != null)
            {
                storeSpecificErrorCode = " storeSpecificErrorCode: " + storeSpecificErrorCode;
            }

            return message + storeSpecificErrorCode;
        }

        private static Dictionary<string, object> EncodeProductDef(ProductDefinition product)
        {
            var prod = new Dictionary<string, object> { { "id", product.id }, { "storeSpecificId", product.storeSpecificId }, { "type", product.type.ToString() } };

            var enabled = true;
            var enabledProp = typeof(ProductDefinition).GetProperty("enabled");
            if (enabledProp != null)
            {
                try
                {
                    enabled = Convert.ToBoolean(enabledProp.GetValue(product, null));
                }
                catch
                {
                    enabled = true;
                }
            }

            prod.Add("enabled", enabled);

            var payoutsArray = new List<object>();
            var payoutsProp = typeof(ProductDefinition).GetProperty("payouts");
            if (payoutsProp != null)
            {
                var payoutsObject = payoutsProp.GetValue(product, null);
                var payouts = payoutsObject as Array;
                if (payouts != null)
                {
                    foreach (var payout in payouts)
                    {
                        var payoutDict = new Dictionary<string, object>();
                        var payoutType = payout.GetType();
                        payoutDict["t"] = payoutType.GetField("typeString").GetValue(payout);
                        payoutDict["st"] = payoutType.GetField("subtype").GetValue(payout);
                        payoutDict["q"] = payoutType.GetField("quantity").GetValue(payout);
                        payoutDict["d"] = payoutType.GetField("data").GetValue(payout);
                        payoutsArray.Add(payoutDict);
                    }
                }
            }

            prod.Add("payouts", payoutsArray);

            return prod;
        }

        private static Dictionary<string, object> EncodeProductDesc(ProductDescription product)
        {
            var prod = new Dictionary<string, object> { { "storeSpecificId", product.storeSpecificId } };

            // ProductDescription.type field available in Unity 5.4+. Access by reflection here.
            var pdClassType = typeof(ProductDescription);
            var pdClassFieldType = pdClassType.GetField("type");
            if (pdClassFieldType != null)
            {
                var typeValue = pdClassFieldType.GetValue(product);
                prod.Add("type", typeValue.ToString());
            }

            prod.Add("metadata", EncodeProductMeta(product.metadata));
            prod.Add("receipt", product.receipt);
            prod.Add("transactionId", product.transactionId);

            return prod;
        }

        private static Dictionary<string, object> EncodeProductMeta(ProductMetadata product)
        {
            var prod = new Dictionary<string, object>
            {
                { "localizedPriceString", product.localizedPriceString },
                { "localizedTitle", product.localizedTitle },
                { "localizedDescription", product.localizedDescription },
                { "isoCurrencyCode", product.isoCurrencyCode },
                { "localizedPrice", Convert.ToDouble(product.localizedPrice) }
            };

            return prod;
        }
    }
}
