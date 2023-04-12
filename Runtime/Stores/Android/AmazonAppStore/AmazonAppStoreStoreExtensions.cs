using System;
using System.Collections.Generic;
using UnityEngine;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Access Amazon store specific functionality.
    /// </summary>
    public class AmazonAppStoreStoreExtensions : IAmazonExtensions, IAmazonConfiguration
    {
        private readonly AndroidJavaObject android;
        /// <summary>
        /// Build the AmazonAppStoreExtensions with the instance of the AmazonAppStore java object
        /// </summary>
        /// <param name="a">AmazonAppStore java object</param>
        public AmazonAppStoreStoreExtensions(AndroidJavaObject a)
        {
            android = a;
        }

        /// <summary>
        /// To use for Amazon’s local Sandbox testing app, generate a JSON description of your product catalog on the device’s SD card.
        /// </summary>
        /// <param name="products">Products to add to the testing app JSON.</param>
        public void WriteSandboxJSON(HashSet<ProductDefinition> products)
        {
            android.Call("writeSandboxJSON", JSONSerializer.SerializeProductDefs(products));
        }

        /// <summary>
        /// Amazon makes it possible to notify them of a product that cannot be fulfilled.
        ///
        /// This method calls Amazon's notifyFulfillment(transactionID, FulfillmentResult.UNAVAILABLE);
        /// https://developer.amazon.com/public/apis/earn/in-app-purchasing/docs-v2/implementing-iap-2.0
        /// </summary>
        /// <param name="transactionID">Products transaction id</param>
        public void NotifyUnableToFulfillUnavailableProduct(string transactionID)
        {
            android.Call("notifyUnableToFulfillUnavailableProduct", transactionID);
        }

        /// <summary>
        /// Gets the current Amazon user ID (for other Amazon services).
        /// </summary>
        public string amazonUserId => android.Call<string>("getAmazonUserId");
    }
}
