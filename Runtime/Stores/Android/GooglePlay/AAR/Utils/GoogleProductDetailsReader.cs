using UnityEngine.Purchasing.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.Utils
{
    class GoogleProductDetailsReader : IGoogleProductDetailsReader
    {
        [Preserve]
        internal GoogleProductDetailsReader()
        {
        }

        public string GetProductId(AndroidJavaObject productDetails)
        {
            return productDetails.Call<string>("getProductId");
        }
    }
}
