using System;

namespace UnityEngine.Purchasing.Security {
	public class IAPSecurityException : System.Exception {
		public IAPSecurityException() { }
		public IAPSecurityException(string message) : base(message) {
		}
	}
	public class InvalidSignatureException : IAPSecurityException {}
}
