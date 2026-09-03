namespace UnityEngine.Purchasing.Interfaces
{
    /// <summary>
    /// Reads fields off a Google Play `ProductDetails` <see cref="AndroidJavaObject"/>.
    /// Exists so callers can be unit tested: `AndroidJavaObject.Call` is non-virtual and needs a live JVM.
    /// Intended to grow into a full `ProductDetails` -> managed struct conversion.
    /// </summary>
    interface IGoogleProductDetailsReader
    {
        string GetProductId(AndroidJavaObject productDetails);
    }
}
