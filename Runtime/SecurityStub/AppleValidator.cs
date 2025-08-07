using System;

namespace UnityEngine.Purchasing.Security
{
    /// <summary>
    /// This class will validate the Apple receipt is signed with the correct certificate.
    /// Note: when building for non-Apple platforms, this will not execute code.
    /// </summary>
    [Obsolete("AppleValidator is a stub and does not perform any validation. Do not use this class. Use UnityEngine.Purchasing.Security.CrossPlatformValidator with a valid Apple certificate for receipt validation.", false)]
    public class AppleValidator
    {
        /// <summary>
        /// Constructs an instance with Apple Certificate.
        /// </summary>
        /// <param name="appleRootCertificate">The apple certificate.</param>
        /// <exception cref="NotImplementedException">Not implemented for this platform.</exception>
        public AppleValidator(byte[] appleRootCertificate)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// Validate that the Apple receipt is signed correctly.
        /// </summary>
        /// <param name="receiptData">The Apple receipt to validate.</param>
        /// <returns>The parsed AppleReceipt</returns>
        /// <exception cref="InvalidSignatureException">The exception thrown if the receipt is incorrectly signed.</exception>
        /// <exception cref="NotImplementedException">Not implemented for this platform.</exception>
        public AppleReceipt Validate(byte[] receiptData)
        {
            throw new NotImplementedException();
        }
    }

    /// <summary>
    /// This class with parse the Apple receipt data received in byte[] into a AppleReceipt object
    /// Note: when building for non-Apple platforms, this will not execute code.
    /// </summary>
    public class AppleReceiptParser
    {
        /// <summary>
        /// Parse the Apple receipt data into a AppleReceipt object
        /// </summary>
        /// <param name="receiptData">Apple receipt data</param>
        /// <returns>The converted AppleReceipt object from the Apple receipt data</returns>
        /// <exception cref="NotImplementedException">Not implemented for this platform.</exception>
        public AppleReceipt Parse(byte[] receiptData)
        {
            throw new NotImplementedException();
        }
    }
}
