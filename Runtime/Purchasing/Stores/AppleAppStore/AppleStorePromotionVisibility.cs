namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This enum is a C# representation of the Apple object `SKProductStorePromotionVisibility`.
    /// https://developer.apple.com/documentation/storekit/skproductstorepromotionvisibility?changes=latest__7
    ///
    /// Converted to a string (ToString) to pass to Apple native code, so do not change these names.
    /// </summary>
    public enum AppleStorePromotionVisibility
    {
        /// <summary>
        /// C# representation of Apple's object `SKProductStorePromotionVisibility.default`
        /// https://developer.apple.com/documentation/storekit/skproductstorepromotionvisibility/default?changes=latest__7
        /// </summary>
        Default,
        /// <summary>
        /// C# representation of Apple's object `SKProductStorePromotionVisibility.hide`
        /// https://developer.apple.com/documentation/storekit/skproductstorepromotionvisibility/hide?changes=latest__7
        /// </summary>
        Hide,
        /// <summary>
        /// C# representation of Apple's object `SKProductStorePromotionVisibility.show`
        /// https://developer.apple.com/documentation/storekit/skproductstorepromotionvisibility/show?changes=latest__7
        /// </summary>
        Show
    }
}
