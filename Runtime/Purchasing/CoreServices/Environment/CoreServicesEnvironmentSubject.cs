using System;
using System.Collections.Generic;
using System.Linq;
using JetBrains.Annotations;

namespace UnityEngine.Purchasing
{
    class CoreServicesEnvironmentSubject
    {
        static CoreServicesEnvironmentSubject s_Instance;

        const string k_DefaultLiveEnvironment = "production";

        string m_LastKnownEnvironment;
        List<ICoreServicesEnvironmentObserver> m_Observers = new List<ICoreServicesEnvironmentObserver>();

        internal static CoreServicesEnvironmentSubject Instance()
        {
            if (s_Instance == null)
            {
                s_Instance = new CoreServicesEnvironmentSubject();
            }

            return s_Instance;
        }

        public void SubscribeToUpdatesAndGetCurrent(ICoreServicesEnvironmentObserver newObserver)
        {
            if (!m_Observers.Contains(newObserver))
            {
                m_Observers.Add(newObserver);
                newObserver.OnUpdatedCoreServicesEnvironment(m_LastKnownEnvironment);
            }
        }

        internal void UpdateCurrentEnvironment(string currentEnvironment)
        {
            m_LastKnownEnvironment = currentEnvironment;
            NotifyObservers();
        }

        void NotifyObservers()
        {
            foreach (var observer in m_Observers)
            {
                observer.OnUpdatedCoreServicesEnvironment(m_LastKnownEnvironment);
            }
        }

        internal bool IsDefaultLiveEnvironment(string environment)
        {
            return environment == k_DefaultLiveEnvironment;
        }
    }
}
