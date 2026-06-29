namespace UnityEngine.Purchasing.WebshopService
{
    internal class WebshopLinkData
    {
        public string Url { get; }
        public bool Live { get; }

        public WebshopLinkData(string url, bool live)
        {
            Url = url;
            Live = live;
        }
    }
}
