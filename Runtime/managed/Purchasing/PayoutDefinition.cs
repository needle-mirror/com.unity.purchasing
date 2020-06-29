using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityEngine.Purchasing
{
    [Serializable]
    public class PayoutDefinition
    {
        [SerializeField]
        PayoutType m_Type = PayoutType.Other;

        [SerializeField]
        string m_Subtype = string.Empty;

        [SerializeField]
        double m_Quantity;

        [SerializeField]
        string m_Data = string.Empty;

        public PayoutType type {
            get {
                return m_Type;
            }
            private set {
                m_Type = value;
            }
        }

        public string typeString {
            get {
                return m_Type.ToString ();
            }
        }

        public const int MaxSubtypeLength = 64;
        public string subtype {
            get {
                return m_Subtype;
            }
            private set {
                if (value.Length > MaxSubtypeLength)
                    throw new ArgumentException(string.Format("subtype cannot be longer than {0}", MaxSubtypeLength));
                m_Subtype = value;
            }
        }

        public double quantity {
            get {
                return m_Quantity;
            }
            private set {
                m_Quantity = value;
            }
        }

        public const int MaxDataLength = 1024;
        public string data {
            get {
                return m_Data;
            }
            private set {
                if (value.Length > MaxDataLength)
                    throw new ArgumentException (string.Format ("data cannot be longer than {0}", MaxDataLength));
                m_Data = value;
            }
        }

        public PayoutDefinition()
        {
        }

        public PayoutDefinition (string typeString, string subtype, double quantity) : this (typeString, subtype, quantity, string.Empty)
        {
        }

        public PayoutDefinition (string typeString, string subtype, double quantity, string data)
        {
            PayoutType t = PayoutType.Other;
            if (Enum.IsDefined(typeof(PayoutType), typeString))
                t = (PayoutType)Enum.Parse (typeof (PayoutType), typeString);

            this.type = t;
            this.subtype = subtype;
            this.quantity = quantity;
            this.data = data;
        }

        public PayoutDefinition (string subtype, double quantity) : this (PayoutType.Other, subtype, quantity, string.Empty)
        {
        }

        public PayoutDefinition (string subtype, double quantity, string data) : this (PayoutType.Other, subtype, quantity, data)
        {
        }

        public PayoutDefinition (PayoutType type, string subtype, double quantity) : this (type, subtype, quantity, string.Empty)
        {
        }

        public PayoutDefinition (PayoutType type, string subtype, double quantity, string data)
        {
            this.type = type;
            this.subtype = subtype;
            this.quantity = quantity;
            this.data = data;
        }
    }
}
