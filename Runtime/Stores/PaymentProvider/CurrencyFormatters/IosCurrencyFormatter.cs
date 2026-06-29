#nullable enable

using System;
#if (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR
using System.Runtime.InteropServices;
#endif
using UnityEngine.Purchasing.Utilities;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Stores.PaymentProviderCurrencyFormatters
{
    /// <summary>
    /// Apple (iOS, tvOS, visionOS) currency formatter backed by <c>Foundation.NumberFormatter</c>.
    /// </summary>
    internal sealed class IosCurrencyFormatter : ICurrencyFormatter
    {
        readonly ICurrencyFormatter m_Fallback;

        [Preserve]
        [Inject]
        public IosCurrencyFormatter() : this(new DotNetCurrencyFormatter()) { }

        // Test seam.
        public IosCurrencyFormatter(ICurrencyFormatter fallback)
        {
            m_Fallback = fallback;
        }

        public string Format(decimal price, string currencyCode, string localeTag)
        {
            return TryFormatNative(price, currencyCode, localeTag)
                   ?? m_Fallback.Format(price, currencyCode, localeTag);
        }

        static string? TryFormatNative(decimal price, string iso, string localeTag)
        {
#if (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR
            IntPtr ptr;
            try
            {
                ptr = NativeFormatCurrency(localeTag, iso, (double)price);
            }
            catch
            {
                return null;
            }

            if (ptr == IntPtr.Zero) return null;
            try
            {
                return Marshal.PtrToStringAuto(ptr);
            }
            finally
            {
                NativeDeallocateMemory(ptr);
            }
#else
            return null;
#endif
        }

#if (UNITY_IOS || UNITY_TVOS || UNITY_VISIONOS) && !UNITY_EDITOR
        [DllImport("__Internal", EntryPoint = "unityPurchasing_FormatCurrency")]
        static extern IntPtr NativeFormatCurrency(string locale, string currencyCode, double amount);

        [DllImport("__Internal", EntryPoint = "unityPurchasing_DeallocateMemory")]
        static extern void NativeDeallocateMemory(IntPtr pointer);
#endif
    }
}
