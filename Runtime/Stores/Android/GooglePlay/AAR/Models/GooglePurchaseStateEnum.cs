namespace UnityEngine.Purchasing.Models
{
    /// <summary>
    /// This is C# representation of the Java Class PurchaseState
    /// <a href="https://developer.android.com/reference/com/android/billingclient/api/Purchase.PurchaseState">See more</a>
    /// </summary>
    static class GooglePurchaseStateEnum
    {
        const string k_AndroidPurchaseStateClassName = "com.android.billingclient.api.Purchase$PurchaseState";

        static AndroidJavaObject GetPurchaseStateJavaObject()
        {
            return new AndroidJavaClass(k_AndroidPurchaseStateClassName);
        }

        static int? s_Purchased;
        internal static int Purchased()
        {
            if (s_Purchased == null)
            {
                using var obj = GetPurchaseStateJavaObject();
                s_Purchased = obj.GetStatic<int>("PURCHASED");
            }
            return s_Purchased.Value;
        }

        static int? s_Pending;
        internal static int Pending()
        {
            if (s_Pending == null)
            {
                using var obj = GetPurchaseStateJavaObject();
                s_Pending = obj.GetStatic<int>("PENDING");
            }
            return s_Pending.Value;
        }
    }
}
