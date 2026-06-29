using System;
using System.Collections.Generic;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Validations
{
    static class MinimumPriceValidation
    {
        // Minimum price guidelines are taken from: https://docs.stripe.com/currencies#minimum-and-maximum-charge-amounts
        static readonly Dictionary<string, double> k_MinimumPrices = new (StringComparer.OrdinalIgnoreCase)
        {
            { "USD", 0.50 },
            { "AED", 2.00 },
            { "ARS", 0.50 },
            { "AUD", 0.50 },
            { "BRL", 0.50 },
            { "CAD", 0.50 },
            { "CHF", 0.50 },
            { "COP", 0.50 },
            { "CZK", 15.00 },
            { "DKK", 2.50 },
            { "EUR", 0.50 },
            { "GBP", 0.30 },
            { "HKD", 4.00 },
            { "HUF", 175.00 },
            { "IDR", 0.50 },
            { "ILS", 0.50 },
            { "INR", 0.50 },
            { "JPY", 50.00 },
            { "KRW", 50.00 },
            { "MXN", 10.0 },
            { "MYR", 2.00 },
            { "NOK", 3.00 },
            { "NZD", 0.50 },
            { "PHP", 0.50 },
            { "PLN", 2.00 },
            { "RON", 2.00 },
            { "RUB", 0.50 },
            { "SEK", 3.00 },
            { "SGD", 0.50 },
            { "THB", 10.0 },
            { "ZAR", 0.50 },
        };

        internal static bool IsPriceValid(string currencyCode, double price, out double minimumPrice)
        {
            if (string.IsNullOrWhiteSpace(currencyCode) ||
                !k_MinimumPrices.TryGetValue(currencyCode, out minimumPrice))
            {
                minimumPrice = 0;
            }

            return price >= minimumPrice;
        }
    }
}
