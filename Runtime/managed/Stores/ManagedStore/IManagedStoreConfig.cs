using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{


    // We may want to add an enum of different store test modes...
    //
    public enum StoreTestMode
    {
        // WIP, Currently Unused
        Normal,
        Sandbox,
        TestMode,
        ServerTest,
        Unknown,
    };


    /// <summary>
    /// IAP E-Commerce Managed Store Configuration
    /// </summary>
    public interface IManagedStoreConfig : IStoreConfiguration
    {
        bool disableStoreCatalog { get; set; }

        bool? trackingOptOut { get; set; }

        // Test Mode Options -- the following should all latch TestEnabled to true

        bool storeTestEnabled { get; set; }

        string baseIapUrl { get; set; }

        string baseEventUrl { get; set; }
    }

}
