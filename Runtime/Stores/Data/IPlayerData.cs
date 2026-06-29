#nullable enable
using System.Threading.Tasks;
using UnityEngine.Purchasing.PaymentProviderService.Models;

namespace UnityEngine.Purchasing.Stores
{
    internal interface IPlayerData
    {
        string DisplayName { get; set; }
        string? Locale { get; }
        string? RegionCode { get; }
        string? CurrencyCode { get; }
        Task<PlayerIdentity> CreatePlayerIdentityAsync();
    }
}
