#nullable enable
using System;

namespace UnityEngine.Purchasing
{
    /// <summary>
    /// Abstraction over the global deep link entry points so the dispatcher can
    /// be unit-tested without driving <c>Application.deepLinkActivated</c>.
    /// </summary>
    interface IDeepLinkSource
    {
        event Action<string> DeepLinkActivated;
        string? LaunchUrl { get; }
    }

    sealed class ApplicationDeepLinkSource : IDeepLinkSource
    {
        public event Action<string> DeepLinkActivated
        {
            add => Application.deepLinkActivated += value;
            remove => Application.deepLinkActivated -= value;
        }

        public string? LaunchUrl =>
            string.IsNullOrEmpty(Application.absoluteURL) ? null : Application.absoluteURL;
    }

    sealed class DeepLinkService : IDeepLinkService, IDisposable
    {
        readonly IDeepLinkSource m_Source;
        Action<string>? m_Handlers;
        string? m_Pending;
        bool m_Disposed;

        internal DeepLinkService(IDeepLinkSource? source = null)
        {
            m_Source = source ?? new ApplicationDeepLinkSource();
            m_Source.DeepLinkActivated += HandleUrl;

            if (!string.IsNullOrEmpty(m_Source.LaunchUrl))
            {
                HandleUrl(m_Source.LaunchUrl!);
            }
        }

        public event Action<string>? OnDeepLinkActivated
        {
            add
            {
                m_Handlers += value;
                if (m_Pending != null && value != null)
                {
                    var pending = m_Pending;
                    m_Pending = null;
                    value(pending);
                }
            }
            remove => m_Handlers -= value;
        }

        public event Action? OnAuthenticationFailed;

        internal void RaiseAuthenticationFailed()
        {
            OnAuthenticationFailed?.Invoke();
        }

        void HandleUrl(string url)
        {
            if (string.IsNullOrEmpty(url))
            {
                return;
            }

            // No subscriber yet (e.g. cold start before game code attaches): keep the
            // latest link until the first handler subscribes, then deliver it once.
            if (m_Handlers == null)
            {
                m_Pending = url;
            }
            else
            {
                m_Handlers.Invoke(url!);
            }
        }

        public void Dispose()
        {
            if (m_Disposed)
            {
                return;
            }
            m_Disposed = true;
            m_Source.DeepLinkActivated -= HandleUrl;
            m_Handlers = null;
            m_Pending = null;
        }
    }
}
