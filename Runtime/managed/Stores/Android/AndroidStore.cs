namespace UnityEngine.Purchasing
{
    public enum AndroidStore
    {
        GooglePlay,     //<= Map to AppStore
        AmazonAppStore, //
        SamsungApps,    //
        UDP,            //
        NotSpecified
    }

    // Is distinct from AndroidStore to avoid non-unique Enum.Parse and Enum.ToString lookup conflicts.
    // Note these must be synchronized with constants in the AppStore enum.
    public enum AndroidStoreMeta
    {
        AndroidStoreStart = AndroidStore.GooglePlay,
        AndroidStoreEnd = AndroidStore.UDP
    }
}
