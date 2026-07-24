// Mirror of C# enum UnityEngine.Purchasing.PurchaseFailureReason.
// KEEP IN SYNC with:
//   Packages/com.unity.purchasing/Runtime/Purchasing/PurchaseFailureReason.cs
// Values are transmitted to C# as raw Int via PurchaseDetails.reason and must
// match the C# enum exactly. Do not renumber existing cases; append new cases
// at the end with the next unused integer. A C# guard test locks these values.

import Foundation

public enum PurchaseFailureReason: Int {
    case PurchasingUnavailable = 0
    case ExistingPurchasePending = 1
    case ProductUnavailable = 2
    case SignatureInvalid = 3
    case UserCancelled = 4
    case PaymentDeclined = 5
    case DuplicateTransaction = 6
    case ValidationFailure = 7
    case StoreNotConnected = 8
    case PurchaseMissing = 9
    case Unknown = 10
    case UserNotAuthenticated = 11
    case NotSupported = 12
    case OrderCancelled = 13
    case OrderStateChanged = 14
}
