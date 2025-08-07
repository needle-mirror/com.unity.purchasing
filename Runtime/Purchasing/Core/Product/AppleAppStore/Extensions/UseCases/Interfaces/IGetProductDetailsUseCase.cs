#nullable enable

using System;
using System.Collections.Generic;

namespace UnityEngine.Purchasing.UseCases.Interfaces
{
    /// <summary>
    /// Interface for a class that acts out the use case of getting product details.
    /// </summary>
    interface IGetProductDetailsUseCase
    {
        /// <summary>
        /// Extracting product details.
        /// </summary>
        /// <returns>returns product details or an empty dictionary</returns>
        Dictionary<string, string> GetProductDetails();
    }
}
