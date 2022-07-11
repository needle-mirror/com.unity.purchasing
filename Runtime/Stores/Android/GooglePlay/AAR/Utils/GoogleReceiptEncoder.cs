using System.Collections.Generic;

namespace UnityEngine.Purchasing.Utils
{
    static class GoogleReceiptEncoder
    {
        internal static string EncodeReceipt(string purchaseOriginalJson, string purchaseSignature, List<string> skuDetailsJson)
        {
            var dic = new Dictionary<string, object>
            {
                ["json"] = purchaseOriginalJson,
                ["signature"] = purchaseSignature,
                ["skuDetails"] = skuDetailsJson,
            };
            return MiniJson.JsonEncode(dic);
        }
    }
}
