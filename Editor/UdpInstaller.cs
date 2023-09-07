using System;
using UnityEngine;
using UnityEditor.PackageManager.UI;

namespace UnityEditor.Purchasing
{
    [Obsolete("UDP support will be removed in the next major update of In-App Purchasing. Right now, the UDP SDK will still function normally in tandem with IAP.")]
    /// <summary>
    /// This class directs the developer to install UDP if it is not already installed through Package Manager.
    /// </summary>
    public class UdpInstaller
    {
        private const string k_PackManWindowTitle = "Get UDP via Package Manager";
        private const string k_NoPackManWindowTitle = "UDP is no longer available in the Package Manager";
        private static readonly Vector2 k_WindowDims = new Vector2(480, 120);

        internal static void PromptUdpInstallation()
        {
            OpenUdpInstallationInstructionsWindow();
        }

        static void OpenUdpInstallationInstructionsWindow()
        {
            OpenUdpWindow<UdpInstallInstructionsWindow>(k_PackManWindowTitle);
        }

        static void OpenUdpWindow<TEditorWindow>(string title) where TEditorWindow : RichEditorWindow
        {
            var window = (TEditorWindow)EditorWindow.GetWindow(typeof(TEditorWindow));
            window.titleContent.text = title;
            window.minSize = k_WindowDims;
            window.Show();
        }

        internal static void PromptUdpUnavailability()
        {
            OpenUdpDeprecatedDisclaimerWindow();
        }

        static void OpenUdpDeprecatedDisclaimerWindow()
        {
            OpenUdpWindow<UdpDeprecatedDisclaimerWindow>(k_NoPackManWindowTitle);
        }
    }

    internal class UdpInstallInstructionsWindow : RichEditorWindow
    {
        private const string k_InfoText = "In order to use this functionality, you must install or update the Unity Distribution Portal Package. Would you like to begin?";

        private const string k_NotNowButtonText = "Not Now";
        private const string k_GoButtonText = "Go";

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            OnTextGui();
            OnButtonGui();

            EditorGUILayout.EndVertical();
        }

        void OnTextGui()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            GUILayout.Label(k_InfoText, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void OnButtonGui()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(k_NotNowButtonText))
            {
                Close();
            }

            if (GUILayout.Button(k_GoButtonText))
            {
                GoToInstaller();
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }

        static void GoToInstaller()
        {
            try
            {
                Window.Open(UnityPurchasingEditor.UdpPackageName);
            }
            catch (Exception exception)
            {
                Debug.LogWarning("Could not locate the Unity Distribution Portal package in package manager. It is now deprecated and you will need to install a local archived copy if you need these features.\nThe Package Manager sent this exception: " + exception.Message);
            }
        }
    }


    class UdpDeprecatedDisclaimerWindow : RichEditorWindow
    {
        const string k_InfoText = "In order to use this functionality, you must install or update the Unity Distribution Portal Package.\nUnfortunately, the package is now deprecated and is no longer hosted in the Unity Registry. You will need to obtain a local copy of it and install it manually.";

        const string k_CloseButtonText = "Close";

        private void OnGUI()
        {
            EditorGUILayout.BeginVertical(GUILayout.ExpandHeight(true));

            OnTextGui();
            OnButtonGui();

            EditorGUILayout.EndVertical();
        }

        void OnTextGui()
        {
            EditorGUILayout.BeginHorizontal();
            EditorGUILayout.BeginVertical();

            GUILayout.Label(k_InfoText, EditorStyles.wordWrappedLabel);

            EditorGUILayout.EndVertical();
            EditorGUILayout.EndHorizontal();
        }

        void OnButtonGui()
        {
            EditorGUILayout.BeginHorizontal();

            GUILayout.FlexibleSpace();

            if (GUILayout.Button(k_CloseButtonText))
            {
                Close();
            }

            EditorGUILayout.EndHorizontal();
        }
    }
}
