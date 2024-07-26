#nullable enable

using UnityEngine.Purchasing.Security;

namespace UnityEngine.Purchasing
{
    interface IAppleReceiptConverter
    {
        AppleReceipt? ConvertFromBase64String(string? receipt);
    }
}
