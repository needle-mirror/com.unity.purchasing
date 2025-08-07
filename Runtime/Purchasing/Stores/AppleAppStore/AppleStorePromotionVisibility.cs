namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This enum is a C# representation of the Apple object `Product.PromotionInfo.Visibility`.
    /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility
    ///
    /// Converted to a string (ToString) to pass to Apple native code, so do not change these names.
    /// </summary>
    public enum AppleStorePromotionVisibility
    {
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.appStoreConnectDefault`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/appstoreconnectdefault
        /// </summary>
        AppStoreConnectDefault = 0,
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.appStoreConnectDefault`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/appstoreconnectdefault
        /// </summary>
        Default = 0,
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.visible`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/visible
        /// </summary>
        Visible = 1,
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.visible`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/visible
        /// </summary>
        Show = 1,
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.hidden`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/hidden
        /// </summary>
        Hidden = 2,
        /// <summary>
        /// C# representation of Apple's object `Product.PromotionInfo.Visibility.hidden`
        /// https://developer.apple.com/documentation/storekit/product/promotioninfo/visibility/hidden
        /// </summary>
        Hide = 2
    }
}
