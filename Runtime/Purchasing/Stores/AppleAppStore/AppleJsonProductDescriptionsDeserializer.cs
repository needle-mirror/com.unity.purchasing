using System;
using System.Collections.Generic;
using Stores.Util;

namespace UnityEngine.Purchasing
{
    class AppleJsonProductDescriptionsDeserializer : JsonProductDescriptionsDeserializer
    {
        internal override ProductMetadata DeserializeMetadata(Dictionary<string, object> data)
        {
            var isFamilyShareable = Convert.ToBoolean(data.TryGetString("isFamilyShareable"));
            return new AppleProductMetadata(
                base.DeserializeMetadata(data),
                isFamilyShareable);
        }
    }
}
