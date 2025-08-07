using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing.Security
{
    /// <summary>
    /// Security exception for when the store is not supported by the <c>CrossPlatformValidator</c>.
    /// </summary>
    public class StoreNotSupportedException : IAPSecurityException
    {
        /// <summary>
        /// Constructs an instance with a message.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        public StoreNotSupportedException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Security exception for an invalid App bundle ID.
    /// </summary>
    public class InvalidBundleIdException : IAPSecurityException { }

    /// <summary>
    /// Security exception for invalid receipt Data.
    /// </summary>
    public class InvalidReceiptDataException : IAPSecurityException { }

    /// <summary>
    /// Security exception for a missing store secret.
    /// </summary>
    public class MissingStoreSecretException : IAPSecurityException
    {
        /// <summary>
        /// Constructs an instance with a message.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        public MissingStoreSecretException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Security exception for an invalid public key.
    /// </summary>
    public class InvalidPublicKeyException : IAPSecurityException
    {
        /// <summary>
        /// Constructs an instance with a message.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        public InvalidPublicKeyException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// A generic exception for <c>CrossPlatformValidator</c> issues.
    /// </summary>
    public class GenericValidationException : IAPSecurityException
    {
        /// <summary>
        /// Constructs an instance with a message.
        /// </summary>
        /// <param name="message"> The message that describes the error. </param>
        public GenericValidationException(string message) : base(message)
        {
        }
    }

    /// <summary>
    /// Class that validates receipts on multiple platforms that support the Security module.
    /// Note that this currently only supports GooglePlay and Apple platforms.
    /// </summary>
    public class CrossPlatformValidator
    {
        private GooglePlayValidator google;
        private string googleBundleId;

        /// <summary>
        /// Constructs an instance and checks the validity of the certification keys for GooglePlay.
        /// </summary>
        /// <param name="googlePublicKey"> The GooglePlay public key. </param>
        /// <param name="googleBundleId"> The GooglePlay bundle ID. </param>
        public CrossPlatformValidator(byte[] googlePublicKey, string googleBundleId)
        {
            try
            {
                if (googlePublicKey != null)
                {
                    google = new GooglePlayValidator(googlePublicKey);
                }
            }
            catch (Exception ex)
            {
                throw new InvalidPublicKeyException("Cannot instantiate self with an invalid public key. (" +
                    ex.ToString() + ")");
            }

            this.googleBundleId = googleBundleId;
        }

        /// <summary>
        /// Constructs an instance and checks the validity of the certification keys
        /// which only takes input parameters for the supported platforms and uses a common bundle ID for Apple and GooglePlay.
        /// </summary>
        /// <param name="googlePublicKey"> The GooglePlay public key. </param>
        /// <param name="appleRootCert"> The Apple certification key. </param>
        /// <param name="appBundleId"> The bundle ID for all platforms. </param>
        [Obsolete("Use the CrossPlatformValidator for Google Play Store only.")]
        public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert,
            string appBundleId) : this(googlePublicKey, appBundleId)
        {
        }

        /// <summary>
        /// Constructs an instance and checks the validity of the certification keys
        /// which uses a common bundle ID for Apple and GooglePlay.
        /// </summary>
        /// <param name="googlePublicKey"> The GooglePlay public key. </param>
        /// <param name="appleRootCert"> The Apple certification key. </param>
        /// <param name="unityChannelPublicKey_not_used"> The Unity Channel public key. Not used because Unity Channel is no longer supported. </param>
        /// <param name="appBundleId"> The bundle ID for all platforms. </param>
        [Obsolete("Use the CrossPlatformValidator for Google Play Store only.")]
        public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, byte[] unityChannelPublicKey_not_used,
            string appBundleId)
            : this(googlePublicKey, appBundleId)
        {
        }

        /// <summary>
        /// Constructs an instance and checks the validity of the certification keys
        /// which only takes input parameters for the supported platforms.
        /// </summary>
        /// <param name="googlePublicKey"> The GooglePlay public key. </param>
        /// <param name="appleRootCert"> The Apple certification key. </param>
        /// <param name="googleBundleId"> The GooglePlay bundle ID. </param>
        /// <param name="appleBundleId"> The Apple bundle ID. </param>
        [Obsolete("Use the CrossPlatformValidator for Google Play Store only.")]
        public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert,
            string googleBundleId, string appleBundleId)
            : this(googlePublicKey, googleBundleId)
        {
        }

        /// <summary>
        /// Constructs an instance and checks the validity of the certification keys.
        /// </summary>
        /// <param name="googlePublicKey"> The GooglePlay public key. </param>
        /// <param name="appleRootCert"> The Apple certification key. </param>
        /// <param name="unityChannelPublicKey_not_used"> The Unity Channel public key. Not used because Unity Channel is no longer supported. </param>
        /// <param name="googleBundleId"> The GooglePlay bundle ID. </param>
        /// <param name="appleBundleId"> The Apple bundle ID. </param>
        /// <param name="xiaomiBundleId_not_used"> The Xiaomi bundle ID. Not used because Xiaomi is no longer supported. </param>
        [Obsolete("Use the CrossPlatformValidator for Google Play Store only.")]
        public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, byte[] unityChannelPublicKey_not_used,
            string googleBundleId, string appleBundleId, string xiaomiBundleId_not_used) : this(googlePublicKey, googleBundleId)
        {
        }

        /// <summary>
        /// Validates a receipt.
        /// </summary>
        /// <param name="unityIAPReceipt"> The receipt to be validated. </param>
        /// <exception cref="IAPSecurityException"> The exception thrown if unityIAPReceipt is deemed invalid. </exception>
        /// <returns> An array of receipts parsed from the validation process </returns>
        public IPurchaseReceipt[] Validate(string unityIAPReceipt)
        {
            try
            {
                var wrapper = (Dictionary<string, object>)MiniJson.JsonDecode(unityIAPReceipt);
                if (null == wrapper)
                {
                    throw new InvalidReceiptDataException();
                }

                var store = (string)wrapper["Store"];
                var payload = (string)wrapper["Payload"];

                switch (store)
                {
                    case "GooglePlay":
                    {
                        if (null == google)
                        {
                            throw new MissingStoreSecretException(
                                "Cannot validate a Google Play receipt without a Google Play public key.");
                        }
                        var details = (Dictionary<string, object>)MiniJson.JsonDecode(payload);
                        var json = (string)details["json"];
                        var sig = (string)details["signature"];
                        var result = google.Validate(json, sig);

                        // [IAP-1696] Check googleBundleId if packageName is present inside the signed receipt.
                        // packageName can be missing when the GPB v1 getPurchaseHistory API is used to fetch.
                        if (!string.IsNullOrEmpty(result.packageName) &&
                            !googleBundleId.Equals(result.packageName))
                        {
                            throw new InvalidBundleIdException();
                        }

                        return new IPurchaseReceipt[] { result };
                    }
                    case "AppleAppStore":
                    case "MacAppStore":
                    {
                        return new IPurchaseReceipt[] { };
                    }
                    default:
                    {
                        throw new StoreNotSupportedException("Store not supported: " + store);
                    }
                }
            }
            catch (IAPSecurityException ex)
            {
                throw ex;
            }
            catch (Exception ex)
            {
                throw new GenericValidationException("Cannot validate due to unhandled exception. (" + ex + ")");
            }
        }
    }
}
