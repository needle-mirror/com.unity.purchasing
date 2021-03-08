using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing.Security
{
    public class StoreNotSupportedException : IAPSecurityException {
        public StoreNotSupportedException (string message) : base(message) {
        }
    }
    public class InvalidBundleIdException : IAPSecurityException { }
    public class InvalidReceiptDataException : IAPSecurityException { }
    public class MissingStoreSecretException : IAPSecurityException {
        public MissingStoreSecretException(string message) : base(message) {
        }
    }
    public class InvalidPublicKeyException : IAPSecurityException {
        public InvalidPublicKeyException(string message) : base(message) {
        }
    }
    public class GenericValidationException : IAPSecurityException {
        public GenericValidationException(string message) : base(message) {
        }
    }

	public class CrossPlatformValidator
	{
	    private GooglePlayValidator google;
		private AppleValidator apple;
		private string googleBundleId, appleBundleId;

	    public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert,
	        string appBundleId) : this(googlePublicKey, appleRootCert, null, appBundleId, appBundleId, null)
	    {
	    }

	    public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, byte[] unityChannelPublicKey_not_used,
		    string appBundleId)
		    : this(googlePublicKey, appleRootCert, null, appBundleId, appBundleId, appBundleId) {
		}

	    public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert,
	        string googleBundleId, string appleBundleId)
	        : this(googlePublicKey, appleRootCert, null, googleBundleId, appleBundleId, null)
	    {
	    }

		public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, byte[] unityChannelPublicKey_not_used,
		    string googleBundleId, string appleBundleId, string xiaomiBundleId_not_used) {
			try {
			    if (null != googlePublicKey) {
			        google = new GooglePlayValidator(googlePublicKey);
			    }

				if (null != appleRootCert) {
					apple = new AppleValidator(appleRootCert);
				}
			} catch (Exception ex) {
				throw new InvalidPublicKeyException ("Cannot instantiate self with an invalid public key. (" +
					ex.ToString () + ")");
			}

			this.googleBundleId = googleBundleId;
			this.appleBundleId = appleBundleId;
		}

		public IPurchaseReceipt[] Validate(string unityIAPReceipt) {
			try {
				var wrapper = (Dictionary<string, object>) MiniJson.JsonDecode (unityIAPReceipt);
				if (null == wrapper) {
					throw new InvalidReceiptDataException ();
				}

				var store = (string)wrapper ["Store"];
				var payload = (string)wrapper ["Payload"];
				switch (store) {
				    case "GooglePlay":
				    {
				        if (null == google)
				        {
				            throw new MissingStoreSecretException(
				                "Cannot validate a Google Play receipt without a Google Play public key.");
				        }
				        var details = (Dictionary<string, object>) MiniJson.JsonDecode(payload);
				        var json = (string) details["json"];
				        var sig = (string) details["signature"];
				        var result = google.Validate(json, sig);

				        // [IAP-1696] Check googleBundleId if packageName is present inside the signed receipt.
				        // packageName can be missing when the GPB v1 getPurchaseHistory API is used to fetch.
				        if (!string.IsNullOrEmpty(result.packageName) &&
				            !googleBundleId.Equals(result.packageName))
				        {
				            throw new InvalidBundleIdException();
				        }

				        return new IPurchaseReceipt[] {result};
				    }
				    case "AppleAppStore":
				    case "MacAppStore":
				    {
				        if (null == apple)
				        {
				            throw new MissingStoreSecretException(
				                "Cannot validate an Apple receipt without supplying an Apple root certificate");
				        }
				        var r = apple.Validate(Convert.FromBase64String(payload));
				        if (!appleBundleId.Equals(r.bundleID))
				        {
				            throw new InvalidBundleIdException();
				        }
				        return r.inAppPurchaseReceipts.ToArray();
				    }
				    default:
				    {
				        throw new StoreNotSupportedException ("Store not supported: " + store);
				    }
				}
			} catch (IAPSecurityException ex) {
				throw ex;
			} catch (Exception ex) {
				throw new GenericValidationException ("Cannot validate due to unhandled exception. ("+ex+")");
			}
		}
	}
}
