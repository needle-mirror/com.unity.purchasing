using System;
using System.Linq;
using UnityEditor.Build;
using UnityEditor.Purchasing.Editor.Authoring.Import.UI;
using UnityEditor.Purchasing.Editor.Authoring.PurchasingAdminApi;
using UnityEngine;
using UnityEngine.UIElements;

namespace UnityEditor.Purchasing.Editor.Authoring.Import.Google.ApiFetcher
{
internal class GoogleCatalogFetcherConfigDrawer : IConfigDrawer
{
    readonly GoogleCatalogApiFetcher m_Fetcher;

    public GoogleCatalogFetcherConfigDrawer(GoogleCatalogApiFetcher fetcher)
    {
        m_Fetcher = fetcher;
    }

    public VisualElement CreateConfigUI()
    {
        var container = new VisualElement();

        var packageName = PlayerSettings.GetApplicationIdentifier(NamedBuildTarget.Android);

        var packageRow = new VisualElement();
        packageRow.style.flexDirection = FlexDirection.Row;
        packageRow.style.alignItems = Align.Center;
        packageRow.style.marginBottom = 4;

        var packageLabel = new Label("Package Name");
        packageLabel.style.minWidth = 120;
        packageLabel.style.marginRight = 8;
        packageRow.Add(packageLabel);

        var packageValue = new Label(string.IsNullOrWhiteSpace(packageName) ? "<missing>" : packageName);
        packageRow.Add(packageValue);
        container.Add(packageRow);

        if (string.IsNullOrWhiteSpace(packageName))
        {
            var warning = new HelpBox(
                "Android Package Name is not configured. Set it in Project Settings > Player before importing.",
                HelpBoxMessageType.Warning);
            warning.style.marginBottom = 4;
            container.Add(warning);

            var openSettingsButton = new Button(() => SettingsService.OpenProjectSettings("Project/Player"));
            openSettingsButton.text = "Open Project Settings";
            openSettingsButton.style.marginBottom = 4;
            container.Add(openSettingsButton);
        }

        var secretKeyRow = new VisualElement();
        secretKeyRow.style.flexDirection = FlexDirection.Row;
        secretKeyRow.style.alignItems = Align.Center;
        secretKeyRow.style.marginBottom = 4;

        var secretKeyLabel = new Label("Secret Key");
        secretKeyLabel.style.minWidth = 120;
        secretKeyLabel.style.marginRight = 8;

        var secretKeyField = new TextField();
        secretKeyField.value = m_Fetcher.SecretKey;
        secretKeyField.RegisterValueChangedCallback(e => m_Fetcher.SecretKey = e.newValue);
        secretKeyField.style.flexGrow = 1;

        secretKeyRow.Add(secretKeyLabel);
        secretKeyRow.Add(secretKeyField);
        container.Add(secretKeyRow);

        var scopeRow = new VisualElement();
        scopeRow.style.flexDirection = FlexDirection.Row;
        scopeRow.style.alignItems = Align.Center;
        scopeRow.style.marginBottom = 4;

        var scopeLabel = new Label("Secret Scope");
        scopeLabel.style.minWidth = 120;
        scopeLabel.style.marginRight = 8;

        var scopeNames = Enum.GetNames(typeof(PlatformCatalogImportRequest.SecretScopeEnum)).ToList();
        var scopeField = new PopupField<string>(
            scopeNames,
            m_Fetcher.SecretScope.ToString());
        scopeField.RegisterValueChangedCallback(e =>
        {
            if (Enum.TryParse<PlatformCatalogImportRequest.SecretScopeEnum>(e.newValue, out var parsed))
                m_Fetcher.SecretScope = parsed;
        });
        scopeField.style.flexGrow = 1;

        scopeRow.Add(scopeLabel);
        scopeRow.Add(scopeField);
        container.Add(scopeRow);

        return container;
    }
}
}
