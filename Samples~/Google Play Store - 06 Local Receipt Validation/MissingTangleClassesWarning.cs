#if UNITY_EDITOR
using System;
using UnityEngine;
using UnityEditor;

namespace Samples.Purchasing.Core.LocalReceiptValidation
{
    /// <summary>
    /// Shows an editor warning then the AppleTangle or GooglePlayTangle classes are missing.
    /// This class is for informational purposes only, see LocalReceiptValidation.cs for the relevant script accomplishing the local receipt validation.
    /// </summary>
    [InitializeOnLoad]
    class MissingTangleClassesWarning
    {
        const string k_TangleClassesNamespace = "UnityEngine.Purchasing.Security";

        const string k_GooglePlayTangleClassName = "GooglePlayTangle";

        static readonly string k_GooglePlayTangleClassPath = $"{k_TangleClassesNamespace}.{k_GooglePlayTangleClassName}";

        const string k_MissingTangleClassesWarningMsg = "You appear to be missing the GooglePlayTangle class. CLICK ME for instructions on how to generate them or see the README for more information.\n" +
            "To generate these classes in your project, do the following:\n" +
            "\t1. Get your license key from the Google Play Developer Console.\n" +
            "\t\ta. Select your app from the list.\n" +
            "\t\tb. Go to \"Monetization setup\" under \"Monetize\".\n" +
            "\t\tc. Copy the key from the \"Licensing\" section.\n" +
            "\t2. Open the obfuscation window from Services > In-App Purchasing > Receipt Validation Obfuscator.\n" +
            "\t3. Paste your Google Play key.\n" +
            "\t4. Obfuscate the key. (Creates GooglePlayTangle class in your project.)\n" +
            "\t5. (Optional) To ensure correct revenue data, enter your key in the Analytics dashboard.\n" +
            "\n" +
            "See README for more information.\n";

        static MissingTangleClassesWarning()
        {
            WarnMissingTangleClasses();
            Application.logMessageReceived += OnLogMsgReceived;
        }

        static void OnLogMsgReceived(string condition, string stacktrace, LogType type)
        {
            if (LogMsgRelatedToTangleClasses(condition, type))
            {
                WarnMissingTangleClasses();
            }
        }

        static void WarnMissingTangleClasses()
        {
            if (!HasTangleClasses())
            {
                Debug.LogWarning(k_MissingTangleClassesWarningMsg);
            }
        }

        static bool HasTangleClasses()
        {
            var googlePlayTangle = Type.GetType(k_GooglePlayTangleClassPath);
            return googlePlayTangle != null;
        }

        static bool LogMsgRelatedToTangleClasses(string condition, LogType type)
        {
            return type == LogType.Error && (
                condition.Contains(k_GooglePlayTangleClassName)
            );
        }
    }
}
#endif
