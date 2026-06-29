using System.Collections.Generic;

namespace UnityEngine.Purchasing.LiveContentAdapterService
{
    internal class ConfigContentData
    {
        public string id;
        public string path;
        public string contentHash;
        public long contentSize;
        public string content;
        public List<string> schemas;
        public List<string> variantTag;
    }
}
