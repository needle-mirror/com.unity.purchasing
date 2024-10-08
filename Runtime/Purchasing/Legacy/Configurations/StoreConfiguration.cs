using System;
using System.Collections.Generic;
using UnityEngine.Purchasing;

namespace UnityEngine.Purchasing
{
    class StoreConfiguration
    {
        public AppStore androidStore { get; private set; }
        public StoreConfiguration(AppStore store)
        {
            androidStore = store;
        }

        public static string Serialize(StoreConfiguration store)
        {
            var dic = new Dictionary<string, object>() {
                { "androidStore", store.androidStore.ToString() }
            };

            return MiniJson.JsonEncode(dic);
        }

        /// <exception cref="ArgumentException">Thrown when parsing fails</exception>
        public static StoreConfiguration Deserialize(string json)
        {
            var dic = (Dictionary<string, object>)MiniJson.JsonDecode(json);

            AppStore store;
            var key = (string)dic["androidStore"];
            store = !Enum.IsDefined(typeof(AppStore), key)
                ? AppStore.GooglePlay
                : (AppStore)Enum.Parse(typeof(AppStore), (string)dic["androidStore"], true);

            return new StoreConfiguration(store);
        }
    }
}
