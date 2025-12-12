namespace UnityEngine.Purchasing
{
    // Based on Apple's App Store Service API Documentation: https://developer.apple.com/documentation/appstoreserverapi/offertype
    internal enum OfferType
    {
        Introductory = 1,
        Promotional = 2,
        Code = 3,
        WinBack  = 4,
        Unknown = -1
    }
}
