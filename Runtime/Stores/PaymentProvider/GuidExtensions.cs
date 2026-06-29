#nullable enable

using System;

namespace UnityEngine.Purchasing.PaymentProviders
{
    internal static class GuidExtensions
    {
        internal static string ToFormattedString(this Guid guid)
        {
            // A string representation of a Guid with hyphens, in lowercase.
            return guid.ToString("D");
        }
    }
}
