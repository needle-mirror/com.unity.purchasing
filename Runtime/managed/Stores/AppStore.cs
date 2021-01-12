namespace UnityEngine.Purchasing
{
	public enum AppStore
	{
	    NotSpecified,
		GooglePlay,     //<= Map to AndroidStore
		AmazonAppStore, //
		SamsungApps,    //
	    UDP,            // Last Android Store
	    MacAppStore,
	    AppleAppStore,
	    WinRT,
	    fake
	}

    // Is distinct from AppStore to avoid non-unique Enum.Parse and Enum.ToString lookup conflicts.
    // Note these must be synchronized with constants in the AndroidStore enum.
    public enum AppStoreMeta
    {
        AndroidStoreStart = AppStore.GooglePlay,
        AndroidStoreEnd = AppStore.UDP
    }
}
