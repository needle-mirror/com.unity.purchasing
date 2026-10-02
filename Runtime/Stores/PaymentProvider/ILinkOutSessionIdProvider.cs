#nullable enable

using System;
using System.Threading.Tasks;

namespace UnityEngine.Purchasing.Stores
{
    internal interface ILinkOutSessionIdProvider
    {
        // Returns a link-out session id for the current player, or null when none is available.
        Task<string?> GetLinkOutSessionId(
            IPlayerData playerData,
            Func<PaymentProviderService.Models.DeviceInfo?> deviceInfoFactory);
    }
}
