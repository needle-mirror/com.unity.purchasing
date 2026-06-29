namespace UnityEngine.Purchasing.WebshopService
{
    /// <summary>
    /// External-token kinds carried on a generated webshop link. Mirrors the backend enum
    /// so the webshop service stays decoupled from store-specific token types. Callers
    /// are responsible for mapping their platform/store token onto one of these values.
    /// </summary>
    internal enum WebshopExternalTokenType
    {
        AppleAcquisition,
        AppleServices,
        AppleLinkOut,
        Google,
    }

    internal readonly struct WebshopExternalToken
    {
        public WebshopExternalTokenType Type { get; }
        public string Token { get; }

        public WebshopExternalToken(WebshopExternalTokenType type, string token)
        {
            Type = type;
            Token = token;
        }
    }
}
