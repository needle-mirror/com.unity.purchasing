using System;
using System.Collections.Generic;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    /// <summary>
    /// A single variant of a Live Content config.
    /// </summary>
    internal sealed class LiveContentConfig
    {
        public string Id { get; }
        public string Path { get; }
        public string ContentType { get; }
        public long SortIndex { get; }
        public IReadOnlyList<string> Schemas { get; }
        public IReadOnlyList<string> VariantTags { get; }
        public string ContentHash { get; }
        public long ContentSize { get; }
        public bool Complete { get; }
        public DateTime CreatedAt { get; }
        public DateTime UpdatedAt { get; }
        public IReadOnlyDictionary<string, object> Metadata { get; }

        /// <summary>
        /// Gets the config's inline content, if available.
        /// This may be <c>null</c> when the config was fetched as metadata only.
        /// </summary>
        public LiveContentConfigBody Body { get; }

        internal LiveContentConfig(
            string id = "",
            string path = "",
            string contentType = "",
            long sortIndex = 0,
            IReadOnlyList<string> schemas = null,
            IReadOnlyList<string> variantTag = null,
            string contentHash = "",
            long contentSize = 0,
            bool complete = false,
            DateTime createdAt = default,
            DateTime updatedAt = default,
            IReadOnlyDictionary<string, object> metadata = null,
            LiveContentConfigBody body = null)
        {
            Id = id;
            Path = path;
            ContentType = contentType;
            SortIndex = sortIndex;
            Schemas = schemas ?? Array.Empty<string>();
            VariantTags = variantTag ?? Array.Empty<string>();
            ContentHash = contentHash;
            ContentSize = contentSize;
            Complete = complete;
            CreatedAt = createdAt;
            UpdatedAt = updatedAt;
            Metadata = metadata ?? new Dictionary<string, object>();
            Body = body;
        }
    }
}
