using System;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.IO;
using Uniject;
using UnityEngine;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
	public class AmazonAppStoreStoreExtensions : IAmazonExtensions, IAmazonConfiguration
	{
		private AndroidJavaObject android;
		public AmazonAppStoreStoreExtensions(AndroidJavaObject a) {
			this.android = a;
		}
		
		public void WriteSandboxJSON(HashSet<ProductDefinition> products)
		{
			android.Call("writeSandboxJSON", JSONSerializer.SerializeProductDefs(products));
		}

		public void NotifyUnableToFulfillUnavailableProduct(string transactionID) {
			android.Call("notifyUnableToFulfillUnavailableProduct", transactionID);
		}

		public string amazonUserId
		{
			get {
				return android.Call<string> ("getAmazonUserId");
			}
		}
	}
}
