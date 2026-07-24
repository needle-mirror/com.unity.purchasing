using System;
using System.Collections.Generic;
using System.Runtime.Serialization;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    [DataContract]
    public class WebshopCategories
    {
        [DataMember(Name = "categories")]
        public List<WebshopCategory> Categories { get; set; }
    }

    [Serializable, DataContract]
    public class WebshopCategory
    {
        [DataMember(Name = "id")]
        public string Id;

        [DataMember(Name = "name")]
        // Editor-only DataContract type deserialized from JSON, not by Unity's serializer.
#pragma warning disable UAC1009, UAC1015
        public Dictionary<string, string> Name;
#pragma warning restore UAC1009, UAC1015
    }
}
