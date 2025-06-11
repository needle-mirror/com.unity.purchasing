#nullable enable

using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    class GoogleOrderInfo : OrderInfo, IGoogleOrderInfo
    {
        public string? ObfuscatedAccountId { get; set; }
        public string? ObfuscatedProfileId { get; set; }

        public GoogleOrderInfo(string receipt, string? transactionID, string storeName, string? obfuscatedAccountId, string? obfuscatedProfileId)
            : base(receipt, transactionID, storeName)
        {
            ObfuscatedAccountId = obfuscatedAccountId;
            ObfuscatedProfileId = obfuscatedProfileId;
        }
    }
}
