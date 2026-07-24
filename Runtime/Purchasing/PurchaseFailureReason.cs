namespace UnityEngine.Purchasing
{
    // Maintainer note (not shipped in API docs): values are transmitted as raw ints by the native
    // iOS SK2 bridge. Keep in sync with the Swift mirror at
    // Plugins/UnityPurchasing/iOS/SK2/Models/PurchaseFailureReason.swift. Do not renumber existing
    // members; append new members with the next unused integer.
    /// <summary>
    /// The various reasons a purchase can fail.
    /// </summary>
    public enum PurchaseFailureReason
    {
        /// <summary>
        /// Purchasing may be disabled in security settings.
        /// </summary>
        PurchasingUnavailable = 0,

        /// <summary>
        /// Another purchase is already in progress.
        /// </summary>
        ExistingPurchasePending = 1,

        /// <summary>
        /// The product was reported unavailable by the purchasing system.
        /// </summary>
        ProductUnavailable = 2,

        /// <summary>
        /// Signature validation of the purchase's receipt failed.
        /// </summary>
        SignatureInvalid = 3,

        /// <summary>
        /// The user opted to cancel rather than proceed with the purchase.
        /// This is not specified on platforms that do not distinguish
        /// cancellation from other failure.
        /// </summary>
        UserCancelled = 4,

        /// <summary>
        /// There was a problem with the payment.
        /// This is unique to Apple platforms.
        /// </summary>
        PaymentDeclined = 5,

        /// <summary>
        /// The transaction has already been completed successfully.
        /// The purchase has already been confirmed.
        /// </summary>
        DuplicateTransaction = 6,

        /// <summary>
        /// Transaction failed verification performed by the TX Verifier service.
        /// </summary>
        ValidationFailure = 7,

        /// <summary>
        /// The purchase couldn't be initiated because the store is not connected.
        /// Use IStoreService.Connect() to initialize the connection to the store.
        /// </summary>
        StoreNotConnected = 8,

        /// <summary>
        /// The billing client responded with OK without including the purchase.
        /// This is unique to the Google Play Store.
        /// </summary>
        PurchaseMissing = 9,

        /// <summary>
        /// A catch all for remaining purchase problems.
        /// Note: Use Enum.Parse to use this named constant if targeting Unity 5.3
        /// or 5.4. Its value differs for 5.5+ which introduced DuplicateTransaction.
        /// </summary>
        Unknown = 10,

        /// <summary>
        /// The user is not authenticated.
        /// </summary>
        UserNotAuthenticated = 11,

        /// <summary>
        /// The current store does not support this functionality.
        /// </summary>
        NotSupported = 12,

        /// <summary>
        /// The order was cancelled before purchase was completed.
        /// </summary>
        OrderCancelled = 13,

        /// <summary>
        /// The operation could not be completed due to an unexpected mismatch in order state.
        /// </summary>
        OrderStateChanged = 14
    }
}
