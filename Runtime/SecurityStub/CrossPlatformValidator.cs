using System;
using System.Linq;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.Security
{
	public class CrossPlatformValidator
	{
		public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, string appBundleId) {
			throw new NotImplementedException();
		}

		public CrossPlatformValidator(byte[] googlePublicKey, byte[] appleRootCert, string googleBundleId, string appleBundleId) {
			throw new NotImplementedException();
		}

		public IPurchaseReceipt[] Validate(string unityIAPReceipt) {
			throw new NotImplementedException();
		}
	}
}
