namespace UnityEngine.Purchasing
{
    interface IGooglePlayStoreSetObfuscatedIdUseCase
    {
        void SetObfuscatedAccountId(string accountId);

        void SetObfuscatedProfileId(string profileId);
    }
}
