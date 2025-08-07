// For backward compatibility with legacy code expecting UnityEngine.Purchasing.IDs
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Purchasing.Extension;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Maps store specific Product identifiers to one
    /// or more store identifiers.
    ///
    /// The name is deliberately terse for use as a collection initializer.
    /// </summary>
    [Obsolete(UnityUtil.ObsoleteUpgradeToIAPV5Message, false)]
    public class IDs : IEnumerable<KeyValuePair<string, string>>
    {
        private readonly Dictionary<string, string> m_Dic = new Dictionary<string, string>();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns> An IEnumerator object that can be used to iterate through the collection. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_Dic.GetEnumerator();
        }

        /// <summary>
        /// Add a product identifier to a list of store names with string.
        /// </summary>
        /// <param name="id"> Product identifier. </param>
        /// <param name="stores"> List of stores by string, to which we the id will be mapped to. </param>
        public void Add(string id, params string[] stores)
        {
            foreach (var store in stores)
            {
                m_Dic[store] = id;
            }
        }

        /// <summary>
        /// Add a product identifier to a list of store names with non strings such as Enums.
        /// </summary>
        /// <param name="id"> Product identifier. </param>
        /// <param name="stores"> List of stores by other object, to which we the id will be mapped to. </param>
        public void Add(string id, params object[] stores)
        {
            foreach (var store in stores)
            {
                m_Dic[store.ToString()] = id;
            }
        }

        internal string SpecificIDForStore(string store, string defaultValue)
        {
            if (m_Dic.ContainsKey(store))
            {
                return m_Dic[store];
            }

            return defaultValue;
        }

        /// <summary>
        /// Retrieve an Enumerator with which can be used to iterate through the internal map structure.
        /// </summary>
        /// <returns> Enumerator as a Key/Value pair. </returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return m_Dic.GetEnumerator();
        }
    }
}
