#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of getting the introductory price dictionary.
    /// </summary>
    interface IGetIntroductoryPriceDictionaryUseCase
    {
        /// <summary>
        /// Extracting Introductory Price subscription related product details.
        /// </summary>
        /// <returns>returns the Introductory Price subscription related product details or an empty dictionary</returns>
        Dictionary<string, string> GetIntroductoryPriceDictionary();
    }
}
