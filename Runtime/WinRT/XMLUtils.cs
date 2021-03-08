using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Linq;

namespace UnityEngine.Purchasing.Default {
    public class XMLUtils {
        public static IEnumerable<TransactionInfo> ParseProducts(string appReceipt)
        {
            if (null == appReceipt)
            {
                return new List<TransactionInfo>();
            }

            try
            {
                var xml = XElement.Parse(appReceipt);
                return from product in xml.Descendants("ProductReceipt")
                       select new TransactionInfo() {
                           productId = (string)product.Attribute("ProductId"),
                           transactionId = (string)product.Attribute("Id")
                       };
            }
            catch (XmlException)
            {
                return new List<TransactionInfo>();    
            }
        }

        public class TransactionInfo
        {
            public string productId;
            public string transactionId;
        }
    }
}
