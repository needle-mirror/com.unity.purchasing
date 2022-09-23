using System;
using System.Collections.Generic;
using Stores.Util;

namespace UnityEngine.Purchasing
{
    class AppleJsonProductDescriptionsDeserializer : JsonProductDescriptionsDeserializer
    {
        internal override ProductMetadata DeserializeMetadata(Dictionary<string, object> data)
        {
            return new AppleProductMetadata(
                baseProductMetadata: base.DeserializeMetadata(data),
                isFamilyShareable: data.TryGetString("isFamilyShareable"));
        }
    }
}
