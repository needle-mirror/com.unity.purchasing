namespace UnityEngine.Purchasing
{
    interface IAndroidJavaStore : INativeStore
    {
        AndroidJavaObject GetStore();
    }
}
