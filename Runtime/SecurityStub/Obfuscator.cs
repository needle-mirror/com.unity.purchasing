using System;

namespace UnityEngine.Purchasing.Security
{
    /// <summary>
    /// This class will deobfuscate the tangled signature used for client-side receipt validation obfuscation.
    /// Note: when building for platforms that do not support this feature, this will not execute code.
    /// </summary>
    public static class Obfuscator
    {
        /// <summary>
        /// Deobfucscates tangle data.
        /// </summary>
        /// <param name="data"> The Apple or GooglePlay public key data to be deobfuscated. </param>
        /// <param name="order"> The array of the order of the data slices used to obfuscate the data when the tangle files were originally generated. </param>
        /// <param name="key"> The encryption key to deobfuscate the tangled data at runtime, previously generated with the tangle file. </param>
        /// <returns>The deobfucated public key</returns>
        /// <exception cref="NotImplementedException">Not implemented for this platform.</exception>
        public static byte[] DeObfuscate(byte[] data, int[] order, int key)
        {
            throw new NotImplementedException();
        }
    }
}
