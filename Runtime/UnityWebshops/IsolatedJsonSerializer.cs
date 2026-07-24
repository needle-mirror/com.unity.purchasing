#nullable enable
using System.IO;
using Newtonsoft.Json;

namespace UnityEngine.Purchasing.WebshopService
{
    // Serializes with a fresh JsonSerializer per call so JsonConvert.DefaultSettings
    // mutations in the host app can't perturb the wire format we send to the webshop
    // service. Equivalent in effect to the generated IsolatedJsonConvert in each
    // client, but scoped to code we own so we're not depending on generated files.
    static class IsolatedJsonSerializer
    {
        public static string Serialize(object value)
        {
            var serializer = JsonSerializer.Create();
            using var sw = new StringWriter();
            using var writer = new JsonTextWriter(sw);
            serializer.Serialize(writer, value);
            return sw.ToString();
        }
    }
}
