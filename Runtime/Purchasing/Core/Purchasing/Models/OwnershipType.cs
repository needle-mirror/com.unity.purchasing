using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// This enum is a C# representation of the Apple object `OwnershipType`.
    /// https://developer.apple.com/documentation/storekit/transaction/ownershiptype
    /// </summary>
    public enum OwnershipType
    {
        /// <summary>
        /// Represents an undefined or unknown ownership type.
        /// </summary>
        Undefined = -1,

        /// <summary>
        /// The product was purchased by the user.
        /// </summary>
        Purchased = 0,

        /// <summary>
        /// The product was purchased by a family member of the user.
        /// </summary>
        FamilyShared = 1,
    }
}
