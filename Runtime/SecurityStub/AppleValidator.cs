using System;
using System.Collections.Generic;
using System.Text;

namespace UnityEngine.Purchasing.Security
{
	public class AppleValidator
	{
		public AppleValidator (byte[] appleRootCertificate)
		{
			throw new NotImplementedException();
		}

		public AppleReceipt Validate (byte [] receiptData)
		{
			throw new NotImplementedException();
		}
	}

	public class AppleReceiptParser
	{
		public AppleReceipt Parse (byte [] receiptData)
		{
			throw new NotImplementedException();
		}
	}
}
