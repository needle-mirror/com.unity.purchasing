using System;

namespace UnityEngine.Purchasing
{
    static class ProductTypeExtensions
    {
        internal static ProductType ToProductType(this string productType)
        {
            switch (productType)
            {
                case "Auto-Renewable Subscription":
                case "Non-Renewing Subscription":
                    return ProductType.Subscription;
                case "Non-Consumable":
                    return ProductType.NonConsumable;
                case "Consumable":
                    return ProductType.Consumable;
            }

            throw new ArgumentException($"Unrecognized productType {productType}");
        }
    }
}
