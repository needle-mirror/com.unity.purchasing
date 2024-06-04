using System.Collections.Generic;

namespace UnityEngine.Purchasing.Utils
{
    static class GoogleReceiptEncoder
    {
        internal static string EncodeReceipt(string purchaseOriginalJson, string purchaseSignature, List<string> productDetailsJson)
        {
            var dic = new Dictionary<string, object>
            {
                ["json"] = purchaseOriginalJson,
                ["signature"] = purchaseSignature,
                ["skuDetails"] = productDetailsJson,
            };
            return MiniJson.JsonEncode(dic);
        }
    }
}
