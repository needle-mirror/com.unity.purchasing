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

    internal class ConfigContentPage
    {
        public List<ConfigContentData> configs = new List<ConfigContentData>();

        /// <summary>
        /// The <c>X-Next-Cursor</c> response header: the <c>after</c> value for the next page. Null on the last page.
        /// </summary>
        public string nextCursor;
    }
}
