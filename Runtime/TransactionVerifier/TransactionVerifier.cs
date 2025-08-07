#nullable enable

using System;
using System.Threading.Tasks;
using Purchasing.TransactionVerifier;
using UnityEngine.Scripting;
using VerifyAppleTransactionRequestBody = UnityEngine.Purchasing.TransactionVerifier.Models.VerifyAppleTransactionRequest;
using VerifyAppleTransactionRequest = UnityEngine.Purchasing.TransactionVerifier.Apple.VerifyAppleTransactionRequest;
using VerifyGoogleTransactionRequestBody = UnityEngine.Purchasing.TransactionVerifier.Models.VerifyGoogleTransactionRequest;
using VerifyGoogleTransactionRequest = UnityEngine.Purchasing.TransactionVerifier.Google.VerifyGoogleTransactionRequest;

#if IAP_UNITY_AUTH_ENABLED
#else
#error Unity Authentication (com.unity.services.authentication) is required when validating transactions (when IAP_TX_VERIFIER_ENABLED is defined).
#endif

namespace UnityEngine.Purchasing.TransactionVerifier
{
    /// <summary>
    /// Enumeration of supported app stores for transaction verification.
    /// Used to identify which store platform a transaction originated from.
    /// </summary>
    public enum Store
    {
        /// <summary>
        /// Apple App Store platform.
        /// Used for transactions from iOS devices including iPhone, iPad, and Mac App Store.
        /// </summary>
        Apple,

        /// <summary>
        /// Google Play Store platform.
        /// Used for transactions from Android devices using Google Play services.
        /// </summary>
        Google,

        /// <summary>
        /// Unknown or unsupported store platform.
        /// Used when the store platform cannot be determined or is not supported for verification.
        /// </summary>
        Unknown
    }

    /// <summary>
    /// Interface for verifying transactions with the Unity Transaction Verifier service.
    /// </summary>
    [Preserve]
    public class TransactionVerifier : ITransactionVerifier
    {
        readonly Store m_Store;
        readonly string? m_ProjectId;
        readonly string? m_EnvironmentId;
        readonly ITransactionVerifierService m_Service;

        /// <summary>
        /// Initializes a new instance of the <see cref="TransactionVerifier"/> class.
        /// </summary>
        /// <param name="storeName">The name of the store.</param>
        /// <param name="projectId">The project ID for the Unity project.</param>
        /// <param name="environmentId">The environment ID for the Unity project.</param>
        public TransactionVerifier(string storeName, string? projectId, string? environmentId)
        {
            m_Store = storeName switch
            {
                "GooglePlay" => Store.Google,
                "AppleAppStore" => Store.Apple,
                _ => Store.Unknown
            };

            m_ProjectId = projectId;
            m_EnvironmentId = environmentId;

            if (m_ProjectId == null || m_EnvironmentId == null)
            {
                Debug.unityLogger.LogError("TransactionVerification", "Unknown project or environment id. Ensure core (UnityServices.InitializeAsync) and authentication (AnalyticsService.Instance.SignIn) are initialized.");
            }

            m_Service = TransactionVerifierService.Instance;
        }

        /// <summary>
        /// Verifies a pending order by sending the transaction representation to the appropriate store's verification endpoint.
        /// </summary>
        /// <param name="transactionRepresentation">The transaction representation to verify, typically a receipt or token.</param>
        /// <returns>The verification response containing the result of the verification.</returns>
        /// <exception cref="Exception">Thrown if the transaction verification fails or if the store type is unknown.</exception>
        public async Task<VerificationResponse> VerifyPendingOrder(string transactionRepresentation)
        {
            switch (m_Store)
            {
                case Store.Apple:
                    // Create Apple request
                    var appleBody = new VerifyAppleTransactionRequestBody(transactionRepresentation);
                    var appleRequest = new VerifyAppleTransactionRequest(m_ProjectId, m_EnvironmentId, appleBody);

                    // Send Request
                    var appleResponse = await m_Service.AppleApi.VerifyAppleTransactionAsync(appleRequest);

                    // Convert apple response to generic
                    if (appleResponse.Status == 200)
                    {
                        return new VerificationResponse(appleResponse.Result);
                    }

                    throw new Exception($"Transaction verification failed with status {appleResponse.Status}");


                case Store.Google:
                    // Create Google request
                    var googleBody = new VerifyGoogleTransactionRequestBody(transactionRepresentation);
                    var googleRequest = new VerifyGoogleTransactionRequest(m_ProjectId, m_EnvironmentId, googleBody);

                    // Send request
                    var googleResponse = await m_Service.GoogleApi.VerifyGoogleTransactionAsync(googleRequest);

                    // Convert google response to generic
                    if (googleResponse.Status == 200)
                    {
                        return new VerificationResponse(googleResponse.Result);
                    }

                    throw new Exception($"Transaction verification failed with status {googleResponse.Status}");

                default:
                    throw new Exception("Transaction verification failed due to unknown store type.");
            }
        }
    }
}
