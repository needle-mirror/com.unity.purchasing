using System.Linq;
using Unity.Services.DeploymentApi.Editor;
using UnityEditor.Purchasing.Editor.Authoring.Deployment;
using UnityEditor.Purchasing.Editor.Authoring.Model;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.UI
{
    [CustomEditor(typeof(RoutingAsset))]
    class RoutingAssetInspector : UnityEditor.Editor
    {
        const string k_Uxml = "Packages/com.unity.purchasing/Editor/Authoring/UI/Assets/RoutingAssetInspector.uxml";

        const long k_ValidationDebounceMs = 400;

        VisualElement m_RootElement;
        VisualElement m_ValidationContainer;
        Button m_ApplyButton;
        Button m_RevertButton;
        bool m_HasValidationErrors;
        IVisualElementScheduledItem m_ValidationSchedule;

        RoutingAsset m_TargetRoutingAsset;
        RoutingInspectorConfig m_RoutingInspectorConfig;
        InspectorElement m_RoutingInspectorElement;
        SerializedObject m_SerializedObjectOriginal;
        SerializedObject m_SerializedObjectCurrent;

        public override VisualElement CreateInspectorGUI()
        {
            m_TargetRoutingAsset = target as RoutingAsset;

            m_RootElement = new VisualElement();

            var visualTree = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(k_Uxml);
            visualTree.CloneTree(m_RootElement);

            CreateInspector();
            BuildRoutingInspectorConfig();
            InitializeSerializedObjects();
            InitializeValidationContainer();
            UpdateValidationWarnings();
            InitializeApplyRevertButtons();

            var routingProvider = PurchasingAuthoringServiceProvider.GetService<RoutingDeploymentProvider>();
            CatalogInspectorHelper.AddDeploymentFooter(
                m_RootElement,
                AssetDatabase.GetAssetPath(target),
                m_TargetRoutingAsset.RoutingDeploymentItem,
                routingProvider,
                "Payment Provider Routing");

            return m_RootElement;
        }

        void CreateInspector()
        {
            var container = m_RootElement.Q("ContainerConfig");
            m_RoutingInspectorElement = new InspectorElement();
            container.Add(m_RoutingInspectorElement);
        }

        void BuildRoutingInspectorConfig()
        {
            m_RoutingInspectorConfig = CreateInstance<RoutingInspectorConfig>();
            m_RoutingInspectorConfig.Initialize(m_TargetRoutingAsset?.RoutingDeploymentItem?.RoutingConfig);
        }

        void InitializeSerializedObjects()
        {
            m_SerializedObjectOriginal = new SerializedObject(m_RoutingInspectorConfig);
            m_SerializedObjectCurrent = new SerializedObject(m_RoutingInspectorConfig);

            m_RoutingInspectorElement.Unbind();
            m_RoutingInspectorElement.Bind(m_SerializedObjectCurrent);
            m_RoutingInspectorElement.TrackSerializedObjectValue(
                m_SerializedObjectCurrent,
                SerializedObjectValueChanged);
        }

        void InitializeValidationContainer()
        {
            m_ValidationContainer = m_RootElement.Q<VisualElement>("ValidationMessages");
        }

        void UpdateValidationWarnings()
        {
            m_ValidationContainer.Clear();
            m_HasValidationErrors = false;

            if (m_SerializedObjectCurrent == null)
            {
                return;
            }

            var inspectorConfig = (RoutingInspectorConfig)m_SerializedObjectCurrent.targetObject;
            var states = inspectorConfig.Validate();
            foreach (var state in states)
            {
                if (state.Level == SeverityLevel.Error)
                {
                    m_HasValidationErrors = true;
                }

                var messageType = state.Level == SeverityLevel.Error
                    ? HelpBoxMessageType.Error
                    : HelpBoxMessageType.Warning;
                var message = string.IsNullOrEmpty(state.Detail)
                    ? state.Description
                    : state.Detail;
                m_ValidationContainer.Add(new HelpBox(message, messageType));
            }
        }

        void SerializedObjectValueChanged(SerializedObject serializedObject)
        {
            var areObjectsEqual = CatalogInspectorHelper.AreSerializedObjectsEqual(
                m_SerializedObjectOriginal, m_SerializedObjectCurrent);
            var hasChanges = !areObjectsEqual;

            m_RevertButton.SetEnabled(hasChanges);
            m_ApplyButton.SetEnabled(false);
            hasUnsavedChanges = hasChanges;

            m_ValidationSchedule?.Pause();
            m_ValidationSchedule = m_RootElement.schedule.Execute(() =>
            {
                UpdateValidationWarnings();
                m_ApplyButton.SetEnabled(hasChanges && !m_HasValidationErrors);
            }).StartingIn(k_ValidationDebounceMs);
        }

        void InitializeApplyRevertButtons()
        {
            m_ApplyButton = m_RootElement.Q<Button>("ApplyButton");
            m_ApplyButton.clicked += SaveChanges;
            m_RevertButton = m_RootElement.Q<Button>("RevertButton");
            m_RevertButton.clicked += DiscardChanges;

            UpdateApplyRevertButtons(false);
        }

        void UpdateApplyRevertButtons(bool enabled)
        {
            m_RevertButton.SetEnabled(enabled);
            m_ApplyButton.SetEnabled(enabled);
        }

        void SaveAssetChanges()
        {
            var inspectorConfig = (RoutingInspectorConfig)m_SerializedObjectCurrent.targetObject;
            var validationStates = inspectorConfig.Validate();
            if (validationStates.Any(s => s.Level == SeverityLevel.Error))
            {
                return;
            }

            m_TargetRoutingAsset.RoutingDeploymentItem.RoutingConfig = inspectorConfig.ToProviderRoutingConfig();
            m_TargetRoutingAsset.SaveToDisk();

            InitializeSerializedObjects();
        }

        public override void SaveChanges()
        {
            SaveAssetChanges();
            base.SaveChanges();
            UpdateApplyRevertButtons(false);
            AssetDatabase.Refresh();
        }

        public override void DiscardChanges()
        {
            RevertAssetChanges();
            base.DiscardChanges();
            UpdateApplyRevertButtons(false);
        }

        void RevertAssetChanges()
        {
            BuildRoutingInspectorConfig();
            InitializeSerializedObjects();
            UpdateApplyRevertButtons(false);
        }
    }
}
