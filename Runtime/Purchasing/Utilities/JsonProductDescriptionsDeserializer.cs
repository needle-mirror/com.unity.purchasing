using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace Stores.Util
{
    class JsonProductDescriptionsDeserializer
    {
        public List<ProductDescription> DeserializeProductDescriptions(string json)
        {
            var objects = (List<object>)MiniJson.JsonDecode(json);
            var result = new List<ProductDescription>();
            foreach (Dictionary<string, object> obj in objects)
            {
                var metadata = DeserializeMetadata((Dictionary<string, object>)obj["metadata"]);
                var product = new ProductDescription(
                    (string)obj["storeSpecificId"],
                    metadata,
                    obj.TryGetString("receipt"),
                    obj.TryGetString("transactionId"),
                    ProductType.NonConsumable);
                result.Add(product);
            }

            return result;
        }

        internal virtual ProductMetadata DeserializeMetadata(Dictionary<string, object> data)
        {
            // We are seeing an occasional exception when converting a string to a decimal here. It may be related to
            // a mono bug with certain cultures' number formatters: https://bugzilla.xamarin.com/show_bug.cgi?id=4814
            //
            // It's not a great idea to set the price to 0 when this happens, but it's probably better than throwing
            // an exception. The best solution is to pass a number for localizedPrice when possible, to avoid any string
            // parsing issues.
            decimal localizedPrice;
            try
            {
                localizedPrice = Convert.ToDecimal(data["localizedPrice"]);
            }
            catch
            {
                localizedPrice = 0.0m;
            }

            return new ProductMetadata(
                data.TryGetString("localizedPriceString"),
                data.TryGetString("localizedTitle"),
                data.TryGetString("localizedDescription"),
                data.TryGetString("isoCurrencyCode"),
                localizedPrice);
        }
    }
}
