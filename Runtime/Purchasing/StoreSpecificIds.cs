using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Serialization;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Maps store specific Product identifiers to one
    /// or more store identifiers.
    ///
    /// The name is deliberately terse for use as a collection initializer.
    /// </summary>
    [Serializable]
    public class StoreSpecificIds : IEnumerable, ISerializationCallbackReceiver
    {
        Dictionary<string, string> m_productIdDictionary = new Dictionary<string, string>();

        [FormerlySerializedAs("m_keys")]
        [SerializeField]
        List<string> m_storeNames = new List<string>();
        [FormerlySerializedAs("m_values")]
        [SerializeField]
        List<string> m_productIds = new List<string>();

        /// <summary>
        /// Returns an enumerator that iterates through the collection.
        /// </summary>
        /// <returns> An IEnumerator object that can be used to iterate through the collection. </returns>
        IEnumerator IEnumerable.GetEnumerator()
        {
            return m_productIdDictionary.GetEnumerator();
        }

        /// <summary>
        /// Retrieve an Enumerator with which can be used to iterate through the internal map structure.
        /// </summary>
        /// <returns> Enumerator as a Key/Value pair. </returns>
        public IEnumerator<KeyValuePair<string, string>> GetEnumerator()
        {
            return m_productIdDictionary.GetEnumerator();
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
                m_productIdDictionary[store] = id;
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
                m_productIdDictionary[store.ToString()] = id;
            }
        }

        internal string SpecificIDForStore(string store, string defaultValue)
        {
            return m_productIdDictionary.ContainsKey(store) ? m_productIdDictionary[store] : defaultValue;
        }

        /// <summary>
        /// Method from ISerializationCallbackReceiver. Allow the serialization of the dictionary.
        /// </summary>
        public void OnBeforeSerialize()
        {
            m_storeNames.Clear();
            m_productIds.Clear();

            foreach (var kvp in m_productIdDictionary)
            {
                m_storeNames.Add(kvp.Key);
                m_productIds.Add(kvp.Value);
            }
        }

        /// <summary>
        /// Method from ISerializationCallbackReceiver. Allow the deserialization of the dictionary.
        /// </summary>
        public void OnAfterDeserialize()
        {
            m_productIdDictionary = new Dictionary<string, string>();

            for (int i = 0; i != m_productIdDictionary.Count; i++)
            {
                m_productIdDictionary.Add(m_storeNames[i], m_productIds[i]);
            }
        }
    }
}
