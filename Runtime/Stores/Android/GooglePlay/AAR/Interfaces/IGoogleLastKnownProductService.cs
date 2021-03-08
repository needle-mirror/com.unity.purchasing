namespace UnityEngine.Purchasing.Interfaces
{
    interface IGoogleLastKnownProductService
    {
        string GetLastKnownProductId();

        void SetLastKnownProductId(string lastKnownProductId);
    }
}
