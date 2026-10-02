using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// A variant's structured config body, held transport-agnostically as JSON. The transport
    /// stays schema-agnostic and hands back this wrapper; each client turns it into its own DTO
    /// at the single point where it knows the schema via <see cref="TryGetContentAs{T}"/>.
    /// </summary>
    internal sealed class LiveContentConfigBody
    {
        JObject Values { get; }

        internal LiveContentConfigBody(JObject values)
        {
            Values = values ?? new JObject();
        }

        /// <summary>
        /// Deserializes the whole body into <typeparamref name="T"/>. Throws on malformed content;
        /// use <see cref="TryGetContentAs{T}"/> when the shape is not guaranteed.
        /// </summary>
        T GetContentAs<T>(JsonSerializerSettings settings = null)
        {
            var serializer = settings == null
                ? JsonSerializer.CreateDefault()
                : JsonSerializer.CreateDefault(settings);
            return Values.ToObject<T>(serializer);
        }

        /// <summary>
        /// Attempts to deserialize the body into <typeparamref name="T"/>. Returns <c>false</c>
        /// (and <c>default</c>) instead of throwing when the content cannot be mapped.
        /// </summary>
        public bool TryGetContentAs<T>(out T value, JsonSerializerSettings settings = null)
        {
            try
            {
                value = GetContentAs<T>(settings);
                return value != null;
            }
            catch (JsonException)
            {
                value = default;
                return false;
            }
        }
    }
}
