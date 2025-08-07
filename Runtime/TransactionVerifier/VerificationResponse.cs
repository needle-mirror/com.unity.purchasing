#nullable enable

using System;
using UnityEngine;
using UnityEngine.Purchasing.TransactionVerifier;
using UnityEngine.Purchasing.TransactionVerifier.Models;

namespace Purchasing.TransactionVerifier
{
    /// <summary>
    /// Represents the response from a transaction verification operation.
    /// Contains the results and metadata from verifying an in-app purchase transaction
    /// against the appropriate app store platform (Apple App Store or Google Play Store).
    /// </summary>
    public class VerificationResponse
    {
        Store m_Store;
        string m_Hash;
        readonly string m_PlayerId;
        readonly string m_ProjectId;
        readonly bool m_Fulfilled;
        readonly DateTime? m_FulfilledAt;
        readonly string? m_TransactionId; // Apple specific identifier
        readonly string? m_OrderId; // Google specific identifier
        readonly GooglePurchaseState? m_PurchaseState; // Google specific purchase state
        readonly string m_ProductId;
        readonly int? m_Quantity;
        readonly string m_TransactionType;
        readonly DateTime m_ValidatedAt;
        readonly DateTime m_UpdatedAt;
        readonly DateTime? m_RevokedAt;

        internal VerificationResponse(AppleTransactionDto transaction)
        {
            m_Store = Store.Apple;
            m_Hash = transaction.Hash;
            m_PlayerId = transaction.PlayerId;
            m_ProjectId = transaction.ProjectId;
            m_Fulfilled = transaction.Fulfilled;
            m_FulfilledAt = transaction.FulfilledAt;
            m_TransactionId = transaction.TransactionId;
            m_OrderId = null;
            m_ProductId = transaction.ProductId;
            m_Quantity = transaction.Quantity;
            m_TransactionType = transaction.TransactionType;
            m_ValidatedAt = transaction.ValidatedAt;
            m_UpdatedAt = transaction.UpdatedAt;
            m_RevokedAt = transaction.RevokedAt;
            m_PurchaseState = null;
        }

        internal VerificationResponse(GoogleTransactionDto transaction)
        {
            m_Store = Store.Google;
            m_Hash = transaction.Hash;
            m_PlayerId = transaction.PlayerId;
            m_ProjectId = transaction.ProjectId;
            m_Fulfilled = transaction.Fulfilled;
            m_FulfilledAt = transaction.FulfilledAt;
            m_TransactionId = null;
            m_OrderId = transaction.OrderId;
            m_ProductId = transaction.ProductId;
            m_Quantity = transaction.Quantity;
            m_TransactionType = transaction.TransactionType;
            m_ValidatedAt = transaction.ValidatedAt;
            m_UpdatedAt = transaction.UpdatedAt;
            m_RevokedAt = null; // Google does not have a revokedAt field for now, no refunds are processed.
            m_PurchaseState = transaction.PurchaseState;
        }

        /// <summary>
        /// Gets the unique identifier associated with the verified transaction.
        /// </summary>
        /// <returns>
        /// For Apple App Store transactions, returns the transaction ID.
        /// For Google Play Store transactions, returns the order ID.
        /// Returns null if the store type is unknown or unsupported.
        /// </returns>
        public string? GetIdentifier()
        {
            switch (m_Store)
            {
                case Store.Apple:
                    return m_TransactionId;
                case Store.Google:
                    return m_OrderId;
                default:
                    Debug.unityLogger.LogException(new Exception("Unknown store type"));
                    return null;
            }
        }
    }
}
