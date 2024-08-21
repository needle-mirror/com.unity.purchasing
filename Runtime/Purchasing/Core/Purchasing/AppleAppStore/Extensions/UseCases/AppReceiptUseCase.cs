#nullable enable

using System;
using UnityEngine.Purchasing.UseCases.Interfaces;
using UnityEngine.Scripting;

namespace UnityEngine.Purchasing.UseCases
{
    class AppReceiptUseCase : IAppReceiptUseCase
    {
        readonly IAppleAppReceiptViewer m_AppleAppReceiptViewer;

        [Preserve]
        internal AppReceiptUseCase(IAppleAppReceiptViewer appleAppReceiptViewer)
        {
            m_AppleAppReceiptViewer = appleAppReceiptViewer;
        }

        public string? AppReceipt()
        {
            return m_AppleAppReceiptViewer.AppReceipt();
        }
    }
}
