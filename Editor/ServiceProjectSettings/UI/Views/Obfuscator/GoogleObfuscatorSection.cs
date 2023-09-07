using UnityEngine.UIElements;

namespace UnityEditor.Purchasing
{
    class GoogleObfuscatorSection : AbstractObfuscatorSection
    {
        readonly GoogleConfigurationData m_GoogleConfigDataRef;

        const string k_GooglePlayKeyEntry = "GooglePlayKeyEntry";

        internal GoogleObfuscatorSection(GoogleConfigurationData googleData)
            : base()
        {
            m_GoogleConfigDataRef = googleData;
        }

        internal void RegisterGooglePlayKeyChangedCallback()
        {
            RegisterGooglePlayKeyChangedCallback(evt =>
            {
                m_GoogleConfigDataRef.googlePlayKey = evt.newValue;
            });
        }

        protected override void ObfuscateKeys()
        {
            m_ErrorMessage = ObfuscationGenerator.ObfuscateGoogleSecrets(GetGoogleKey());
        }

        string GetGoogleKey()
        {
            return m_GoogleConfigDataRef.googlePlayKey;
        }

        protected override bool DoesTangleFileExist()
        {
            return ObfuscationGenerator.DoesGooglePlayTangleClassExist();
        }

        internal void SetGooglePlayKeyText(string key)
        {
            m_ObfuscatorBlock.Q<TextField>(k_GooglePlayKeyEntry).SetValueWithoutNotify(key);
        }

        internal void RegisterGooglePlayKeyChangedCallback(EventCallback<ChangeEvent<string>> changeEvent)
        {
            m_ObfuscatorBlock.Q<TextField>(k_GooglePlayKeyEntry).RegisterValueChangedCallback(changeEvent);
        }
    }
}
