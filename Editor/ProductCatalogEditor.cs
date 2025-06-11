using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Editor window for displaying and editing the contents of the default ProductCatalog.
    /// </summary>
    public class ProductCatalogEditor : EditorWindow
    {
        private const bool kValidateDebugLog = false;

        private static readonly string[] kStoreKeys =
        {
            AppleAppStore.Name,
            GooglePlay.Name,
            MacAppStore.Name
        };

        /// <summary>
        /// The path in the Menu bar where the <c>ProductCatalogEditor</c> item is located.
        /// </summary>
        public const string ProductCatalogEditorMenuPath = IapMenuConsts.MenuItemRoot + "/IAP Catalog...";

        /// <summary>
        /// Opens the <c>ProductCatalogEditor</c> Window or moves it to the front of the draw order.
        /// </summary>
        [MenuItem(ProductCatalogEditorMenuPath, false, 200)]
        public static void ShowWindow()
        {
            GetWindow(typeof(ProductCatalogEditor));

            GenericEditorMenuItemClickEventSenderHelpers.SendIapMenuOpenCatalogEvent();
            GameServicesEventSenderHelpers.SendTopMenuIapCatalogEvent();
        }

        private static readonly GUIContent windowTitle = new GUIContent("IAP Catalog");
        private static readonly List<ProductCatalogItemEditor> productEditors = new List<ProductCatalogItemEditor>();
        private static readonly List<ProductCatalogItemEditor> toRemove = new List<ProductCatalogItemEditor>();
        private Rect exportButtonRect;
        private ExporterValidationResults validation;

        private DateTime lastChanged;
        private bool dirty;
        private readonly TimeSpan kSaveDelay = new TimeSpan(0, 0, 0, 0, 500); // 500 milliseconds

        /// <summary>
        /// Since we are changing the product catalog's location, it may be necessary to migrate existing product
        /// catalog to the new product catalog location.
        /// </summary>
        [InitializeOnLoadMethod]
        internal static void MigrateProductCatalog()
        {
            try
            {
                var file = new FileInfo(ProductCatalog.kCatalogPath);

                // This will create the new product catalog file location, if it already exists,
                // this will not do anything.
                file.Directory.Create();

                // See if the product catalog already exists in the new location.
                if (File.Exists(ProductCatalog.kCatalogPath))
                {
                    return;
                }

                // check if catalog exists before moving it
                if (DoesPrevCatalogPathExist())
                {
                    AssetDatabase.MoveAsset(ProductCatalog.kPrevCatalogPath, ProductCatalog.kCatalogPath);
                }
            }
            catch (Exception ex)
            {
                Debug.unityLogger.LogIAPException(ex);
            }
        }

        internal static bool DoesPrevCatalogPathExist()
        {
            return File.Exists(ProductCatalog.kPrevCatalogPath);
        }

        /// <summary>
        /// Property which gets the <c>ProductCatalog</c> instance which is being edited.
        /// </summary>
        public ProductCatalog Catalog { get; private set; }

        /// <summary>
        /// Sets the results of the validation of catalog items upon export.
        /// </summary>
        /// <param name="catalogResults"> Validation results of the exported catalog </param>
        /// <param name="itemResults"> List of validation results of the exported items </param>
        public void SetCatalogValidationResults(ExporterValidationResults catalogResults,
            List<ExporterValidationResults> itemResults)
        {
            validation = catalogResults;

            if (productEditors.Count == itemResults.Count)
            {
                for (var i = 0; i < productEditors.Count; ++i)
                {
                    productEditors[i].SetValidationResults(itemResults[i]);
                }
            }
        }

        void Awake()
        {
            Catalog = ProductCatalog.LoadDefaultCatalog();
            if (Catalog.allProducts.Count == 0)
            {
                AddNewProduct(); // Start the catalog with one item
            }
        }

        void OnEnable()
        {
            titleContent = windowTitle;

            productEditors.Clear();
            foreach (var product in Catalog.allProducts)
            {
                productEditors.Add(new ProductCatalogItemEditor(product));
            }
        }

        private void OnDisable()
        {
            if (dirty)
            {
                Save();
            }
        }

        private void Update()
        {
            if (dirty && DateTime.Now.Subtract(lastChanged) > kSaveDelay)
            {
                Save();
            }
        }

        private void SetDirtyFlag()
        {
            lastChanged = DateTime.Now;
            dirty = true;
        }

        private void Save()
        {
            dirty = false;
            File.WriteAllText(ProductCatalog.kCatalogPath, ProductCatalog.Serialize(Catalog));

            AssetDatabase.ImportAsset(ProductCatalog.kCatalogPath);
        }

        private Vector2 scrollPosition = new Vector2();

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar,
                GUI.skin.verticalScrollbar, GUI.skin.box);

            ShowValidationResultsGUI(validation);
            ValidateProductIds();

            EditorGUI.BeginChangeCheck();

            EditorGUILayout.LabelField("Products:");

            foreach (var editor in productEditors)
            {
                EditorGUILayout.BeginVertical(GUI.skin.box);
                editor.OnGUI();
                EditorGUILayout.EndVertical();
            }

            if (GUILayout.Button("Add Product"))
            {
                AddNewProduct();
                GenericEditorButtonClickEventSenderHelpers.SendCatalogAddProductEvent();
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical();
            var defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 315;

            var catalogHasProducts = !Catalog.IsEmpty();
            if (catalogHasProducts)
            {
                ShowAndProcessCodelessAutoInitToggleGuis();
            }

            EditorGUILayout.EndVertical();

            var exportBox = EditorGUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = defaultLabelWidth;

            EditorGUILayout.LabelField("Catalog Export");

            Catalog.appleSKU = ShowEditTextFieldGuiAndGetValue("appleSKU", "Apple SKU:", Catalog.appleSKU);
            Catalog.appleTeamID = ShowEditTextFieldGuiAndGetValue("appleTeamID", "Apple Team ID:", Catalog.appleTeamID);

            if (EditorGUI.EndChangeCheck())
            {
                CheckForDuplicateIDs();
                SetDirtyFlag();
            }

            exportButtonRect = new Rect(exportBox.xMax - ProductCatalogExportWindow.kWidth,
                exportBox.yMin,
                ProductCatalogExportWindow.kWidth,
                EditorGUIUtility.singleLineHeight);
            if (GUI.Button(exportButtonRect,
                    new GUIContent("App Store Export", "Export products for bulk import into app store tools.")))
            {
                PopupWindow.Show(exportButtonRect, new ProductCatalogExportWindow(this));
            }

            EditorGUILayout.EndVertical();

            if (toRemove.Count > 0)
            {
                productEditors.RemoveAll(x => toRemove.Contains(x));
                foreach (var editor in toRemove)
                {
                    Catalog.Remove(editor.Item);
                }

                toRemove.Clear();
                SetDirtyFlag();
            }
        }

        private void ShowAndProcessCodelessAutoInitToggleGuis()
        {
            EditorGUILayout.Space();

            ShowAndProcessIapAutoInitToggleGui();
            if (Catalog.enableCodelessAutoInitialization)
            {
                ShowAndProcessUgsAutoInitToggleGui();
            }

            EditorGUILayout.Space();
        }

        private void ShowAndProcessIapAutoInitToggleGui()
        {
            var newValue = EditorGUILayout.Toggle(
                new GUIContent("Automatically initialize UnityIAPServices (recommended)",
                    "Automatically start Unity IAP if there are any products defined in this catalog. Uncheck this if you plan to initialize Unity IAP manually in your code."),
                Catalog.enableCodelessAutoInitialization);

            UpdateIapAutoInitValue(newValue);
        }

        private void UpdateIapAutoInitValue(bool newValue)
        {
            if (newValue != Catalog.enableCodelessAutoInitialization)
            {
                Catalog.enableCodelessAutoInitialization = newValue;

                GenericEditorClickCheckboxEventSenderHelpers.SendCatalogAutoInitToggleEvent(newValue);
            }
        }

        private void ShowAndProcessUgsAutoInitToggleGui()
        {
            var newValue = EditorGUILayout.Toggle(new GUIContent(
                    "Automatically initialize Unity Gaming Services",
                    "This initializes Unity Gaming Services with the default `production` environment.\n" +
                    "This way of initializing Unity Gaming Services might not be compatible with all other services as they might require special initialization options.\n" +
                    "If the use of initialization options is needed, Unity Gaming Services should be initialized with the coded API."),
                Catalog.enableUnityGamingServicesAutoInitialization);

            UpdateUgsAutoInitValue(newValue);
        }

        private void UpdateUgsAutoInitValue(bool newValue)
        {
            if (newValue != Catalog.enableUnityGamingServicesAutoInitialization)
            {
                Catalog.enableUnityGamingServicesAutoInitialization = newValue;

                GenericEditorClickCheckboxEventSenderHelpers.SendCatalogUgsAutoInitToggleEvent(newValue);
            }
        }

        string ShowEditTextFieldGuiAndGetValue(string fieldName, string label, string oldText)
        {
            BeginErrorBlock(validation, fieldName);
            var newText = EditorGUILayout.TextField(label, oldText);
            EndErrorBlock(validation, fieldName);

            if (newText != oldText)
            {
                GenericEditorFieldEditEventSenderHelpers.SendCatalogEditEvent(fieldName);
            }

            return newText;
        }

        private static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrEmpty(value?.Trim());
        }

        private void ValidateProductIds()
        {
            foreach (var productEditor in productEditors)
            {
                if (IsNullOrWhiteSpace(productEditor.Item.id))
                {
                    productEditor.SetIDInvalidError(true);
                }
            }
        }

        private void AddNewProduct()
        {
            // go through the previously created products and check if any of them has an empty id, thus we prevent the
            // creation of an empty product if the id is not filled.
            // check is the previous  product item added to the list has a valid id
            var invalidIdsExist = false;
            foreach (var product in productEditors)
            {
                if (IsNullOrWhiteSpace(product.Item.id))
                {
                    product.SetIDInvalidError(true);
                    product.SetShouldBeMarked(true);
                    invalidIdsExist = true;
                }
            }

            if (invalidIdsExist)
            {
                return;
            }

            var newEditor = new ProductCatalogItemEditor();
            newEditor.SetShouldBeMarked(false);
            productEditors.Add(newEditor);
            Catalog.Add(newEditor.Item);
        }

        private void CheckForDuplicateIDs()
        {
            var ids = new HashSet<string>();
            var duplicates = new HashSet<string>();
            foreach (var product in Catalog.allProducts)
            {
                if (!string.IsNullOrEmpty(product.id) && ids.Contains(product.id))
                {
                    duplicates.Add(product.id);
                }

                ids.Add(product.id);
            }

            foreach (var editor in productEditors)
            {
                editor.SetIDDuplicateError(editor.Item != null && duplicates.Contains(editor.Item.id));
            }
        }

        private static void ShowValidationResultsGUI(ExporterValidationResults results)
        {
            if (results != null)
            {
                var style = new GUIStyle();
                if (results.errors.Count > 0)
                {
                    style.normal.textColor = Color.red;
                    foreach (var error in results.errors)
                    {
                        GUILayout.Box(error, style);
                    }
                }

                if (results.warnings.Count > 0)
                {
                    style.normal.textColor = Color.black;
                    foreach (var warning in results.warnings)
                    {
                        GUILayout.Box(warning, style);
                    }
                }
            }
        }

        private static void BeginErrorBlock(ExporterValidationResults validation, string fieldName)
        {
            EditorGUI.BeginChangeCheck();
        }

        private static void EndErrorBlock(ExporterValidationResults validation, string fieldName)
        {
            if (EditorGUI.EndChangeCheck() && validation != null)
            {
                validation.fieldErrors.Remove(fieldName);
            }

            if (validation != null && validation.fieldErrors.ContainsKey(fieldName))
            {
                var style = new GUIStyle();
                style.normal.textColor = Color.red;
                EditorGUILayout.LabelField(validation.fieldErrors[fieldName], style);
            }
        }

        /// <summary>
        /// Exports the Catalog to a file for a particular store, or erases an existing exported file.
        /// </summary>
        /// <param name="storeName"> The name of the store to be exported.</param>
        /// <param name="folder"> The full path of the export file, including the file name.</param>
        /// <param name="eraseExport"> If true, it will just erase the export file and do nothing else.</param>
        /// <returns>Whether or not the export was succesful. Always returns false if eraseExport is true.</returns>
        public static bool Export(string storeName, string folder, bool eraseExport)
        {
            var editor = CreateInstance(typeof(ProductCatalogEditor)) as ProductCatalogEditor;
            return new ProductCatalogExportWindow(editor).Export(storeName, folder, eraseExport);
        }

        /// <summary>
        /// Inner class for displaying and editing the contents of a single entry in the ProductCatalog.
        /// </summary>
        public class ProductCatalogItemEditor
        {
            private const float k_DuplicateIDFieldWidth = 90f;

            /// <summary>
            /// Property which gets the <c>ProductCatalogItem</c> instance being edited.
            /// </summary>
            public ProductCatalogItem Item { get; private set; }

            private ExporterValidationResults validation;

            private readonly bool editorSupportsPayouts = false;

            private bool advancedVisible = true;
            private bool descriptionVisible = true;
            private bool storeIDsVisible = false;
            private bool payoutsVisible = false;
            private bool googleVisible = false;
            private bool appleVisible = false;

            private bool idDuplicate = false;
            private bool idInvalid = false;
            private bool shouldBeMarked = true;

            private readonly List<LocalizedProductDescription> descriptionsToRemove = new List<LocalizedProductDescription>();
            private readonly List<ProductCatalogPayout> payoutsToRemove = new List<ProductCatalogPayout>();

            /// <summary>
            /// Default constructor. Creates a new <c>ProductCatalogItem</c> to edit.
            /// </summary>
            public ProductCatalogItemEditor()
            {
                Item = new ProductCatalogItem();

                editorSupportsPayouts = null != typeof(ProductDefinition).GetProperty("payouts");
            }

            /// <summary>
            /// Constructor taking an instance of <c>ProductCatalogItem</c> to edit.
            /// </summary>
            /// <param name="description"> The description of the item being created. </param>
            public ProductCatalogItemEditor(ProductCatalogItem description)
            {
                Item = description;
                editorSupportsPayouts = null != typeof(ProductDefinition).GetProperty("payouts");
            }

            /// <summary>
            /// Function called when the GUI updates.
            /// </summary>
            public void OnGUI()
            {
                var style = new GUIStyle(EditorStyles.foldout);
                var box = EditorGUILayout.BeginVertical();

                var rect = new Rect(box.xMax - EditorGUIUtility.singleLineHeight - 2, box.yMin, EditorGUIUtility.singleLineHeight + 2, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rect, "x") && EditorUtility.DisplayDialog("Delete Product?", "Are you sure you want to delete this product?", "Delete", "Do Not Delete"))
                {
                    toRemove.Add(this);
                    GenericEditorButtonClickEventSenderHelpers.SendCatalogRemoveProductEvent();
                }

                ShowValidationResultsGUI(validation);

                var productLabel = Item.id + (string.IsNullOrEmpty(Item.defaultDescription.Title)
                    ? string.Empty
                    : " - " + Item.defaultDescription.Title);

                if (string.IsNullOrEmpty(productLabel) || Item.id.Trim().Length == 0)
                {
                    productLabel = "Product ID is Empty";
                }
                else
                {
                    idInvalid = false;
                }

                EditorGUILayout.LabelField(productLabel);
                EditorGUILayout.Space();

                var idRect = EditorGUILayout.GetControlRect(true);
                idRect.width -= k_DuplicateIDFieldWidth;

                ShowAndProcessProductIDBlockGui(idRect);

                ShowAndProcessProductTypeBlockGui(idRect.width);

                advancedVisible = CompatibleGUI.Foldout(advancedVisible, "Advanced", true, style);
                if (advancedVisible)
                {
                    EditorGUI.indentLevel++;

                    descriptionVisible = CompatibleGUI.Foldout(descriptionVisible, "Descriptions", true, style);
                    if (descriptionVisible)
                    {
                        EditorGUI.indentLevel++;

                        DescriptionEditorGUI(Item.defaultDescription, false, "defaultDescription");

                        var translationBox = EditorGUILayout.BeginVertical();
                        EditorGUILayout.LabelField("Translations");
                        EditorGUI.indentLevel++;

                        var plusButtonWidth = EditorGUIUtility.singleLineHeight + 2;
                        var plusButtonRect = new Rect(translationBox.xMax - plusButtonWidth,
                            translationBox.yMin,
                            plusButtonWidth,
                            EditorGUIUtility.singleLineHeight);
                        if (Item.HasAvailableLocale && GUI.Button(plusButtonRect, "+"))
                        {
                            Item.AddDescription(Item.NextAvailableLocale);

                            GenericEditorButtonClickEventSenderHelpers.SendCatalogAddTranslationEvent();
                        }

                        foreach (var desc in Item.translatedDescriptions)
                        {
                            if (DescriptionEditorGUI(desc, true, "translatedDescriptions." + desc.googleLocale))
                            {
                                descriptionsToRemove.Add(desc);

                                GenericEditorButtonClickEventSenderHelpers.SendCatalogRemoveTranslationEvent();
                            }
                        }

                        EditorGUI.indentLevel--;
                        EditorGUILayout.EndVertical();

                        if (descriptionsToRemove.Count > 0)
                        {
                            foreach (var desc in descriptionsToRemove)
                            {
                                Item.RemoveDescription(desc.googleLocale);
                            }

                            descriptionsToRemove.Clear();
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    if (editorSupportsPayouts)
                    {
                        payoutsVisible = EditorGUILayout.Foldout(payoutsVisible, "Payouts", style);
                        if (payoutsVisible)
                        {
                            EditorGUI.indentLevel++;

                            var payoutIndex = 1;
                            foreach (var payout in Item.Payouts)
                            {
                                var payoutBox = EditorGUILayout.BeginVertical();
                                var removeButtonWidth = EditorGUIUtility.singleLineHeight + 2;
                                var payoutRemoveRect = new Rect(payoutBox.xMax - removeButtonWidth, payoutBox.yMin,
                                    removeButtonWidth, EditorGUIUtility.singleLineHeight);

                                EditorGUILayout.LabelField(payoutIndex.ToString() + ".");
                                if (GUI.Button(payoutRemoveRect, "x")
                                    && EditorUtility.DisplayDialog("Delete Payout?",
                                        "Are you sure you want to delete this payout?",
                                        "Delete",
                                        "Do Not Delete"))
                                {
                                    payoutsToRemove.Add(payout);
                                    GenericEditorButtonClickEventSenderHelpers.SendCatalogRemovePayoutEvent();
                                }

                                EditorGUI.indentLevel++;
                                ShowAndProcessPayoutBlockGui(payout);
                                EditorGUI.indentLevel--;

                                EditorGUILayout.EndVertical();

                                payoutIndex++;
                            }

                            payoutsToRemove.ForEach((p) => Item.RemovePayout(p));
                            payoutsToRemove.Clear();

                            if (GUILayout.Button("Add Payout"))
                            {
                                Item.AddPayout();
                                GenericEditorButtonClickEventSenderHelpers.SendCatalogAddPayoutEvent();
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUILayout.Separator();

                    storeIDsVisible = CompatibleGUI.Foldout(storeIDsVisible, "Store ID Overrides", true, style);

                    if (storeIDsVisible)
                    {
                        EditorGUI.indentLevel++;
                        foreach (var storeKey in kStoreKeys)
                        {
                            var newStoreID = ShowEditTextFieldGuiWithValidationErrorBlockAndGetValue("storeID." + storeKey, storeKey, Item.GetStoreID(storeKey));
                            Item.SetStoreID(storeKey, newStoreID);
                        }

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    googleVisible = CompatibleGUI.Foldout(googleVisible, "Google Configuration", true, style);
                    if (googleVisible)
                    {
                        EditorGUI.indentLevel++;

                        ShowAndProcessGoogleConfigGui();

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    appleVisible = CompatibleGUI.Foldout(appleVisible, "Apple Configuration", true, style);
                    if (appleVisible)
                    {
                        EditorGUI.indentLevel++;

                        ShowAndProcessAppleConfigGui();

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            void ShowAndProcessProductIDBlockGui(Rect idRect)
            {
                BeginErrorBlock(validation, "id");

                var oldID = Item.id;

                Item.id = EditorGUI.TextField(idRect, "ID:", Item.id);

                if (oldID != Item.id)
                {
                    GenericEditorFieldEditEventSenderHelpers.SendCatalogEditEvent("productId");
                }

                var style = new GUIStyle();
                style.normal.textColor = Color.red;
                var duplicateIDLabelRect = new Rect(idRect.xMax + 5, idRect.yMin, k_DuplicateIDFieldWidth, EditorGUIUtility.singleLineHeight);

                EditorGUI.LabelField(duplicateIDLabelRect, idDuplicate ? "ID is a duplicate" : string.Empty, style);
                EditorGUI.LabelField(duplicateIDLabelRect, idDuplicate && idInvalid && shouldBeMarked ? "ID is empty" : string.Empty, style);

                EndErrorBlock(validation, "id");
            }

            void ShowAndProcessProductTypeBlockGui(float width)
            {
                BeginErrorBlock(validation, "type");

                var oldType = Item.type;

                var typeRect = EditorGUILayout.GetControlRect(true);
                typeRect.width = width;
                Item.type = (ProductType)EditorGUI.EnumPopup(typeRect, "Type:", (CatalogPopupProductType)Item.type);

                if (oldType != Item.type)
                {
                    var typeName = Enum.GetName(typeof(ProductType), Item.type);
                    GenericEditorDropdownSelectEventSenderHelpers.SendCatalogSetProductTypeEvent(typeName);
                }

                EndErrorBlock(validation, "type");
            }

            void ShowAndProcessPayoutBlockGui(ProductCatalogPayout payout)
            {
                var oldType = payout.type;
                payout.type = (ProductCatalogPayout.ProductCatalogPayoutType)EditorGUILayout.EnumPopup("Type", payout.type);
                if (oldType != payout.type)
                {
                    var typeName = Enum.GetName(typeof(ProductCatalogPayout.ProductCatalogPayoutType), payout.type);
                    GenericEditorDropdownSelectEventSenderHelpers.SendCatalogSetPayoutTypeEvent(typeName);
                }

                payout.subtype = TruncateString(ShowEditTextFieldGuiAndGetValue("payoutSubtype", "Subtype", payout.subtype), ProductCatalogPayout.MaxSubtypeLength);
                payout.quantity = ShowEditDoubleFieldGuiAndGetValue("payoutQuantity", "Quantity", payout.quantity);
                payout.data = TruncateString(ShowEditTextFieldGuiAndGetValue("payoutData", "Data", payout.data), ProductCatalogPayout.MaxDataLength);
            }

            void ShowAndProcessGoogleConfigGui()
            {
                EditorGUILayout.LabelField("Provide either a price or an ID for a pricing template created in Google Play");

                var fieldName = "googlePrice";
                BeginErrorBlock(validation, fieldName);
                var priceStr = ShowEditTextFieldGuiAndGetValue(fieldName, "Price:", Item.googlePrice == null || Item.googlePrice.value == 0 ? string.Empty : Item.googlePrice.value.ToString());

                Item.googlePrice.value = decimal.TryParse(priceStr, out var priceDecimal) ? priceDecimal : 0;

                Item.pricingTemplateID = ShowEditTextFieldGuiAndGetValue("googlePriceTemplate", "Pricing Template:", Item.pricingTemplateID);
                EndErrorBlock(validation, fieldName);
            }

            void ShowAndProcessAppleConfigGui()
            {
                BeginErrorBlock(validation, "applePriceTier");

                var oldTier = Item.applePriceTier;

                Item.applePriceTier = EditorGUILayout.Popup("Price Tier:", Item.applePriceTier, ApplePriceTiers.Strings);
                EndErrorBlock(validation, "applePriceTier");

                if ((oldTier != Item.applePriceTier) && (Item.applePriceTier < ApplePriceTiers.Strings.Length))
                {
                    GenericEditorDropdownSelectEventSenderHelpers.SendCatalogSetApplePriceTierEvent(ApplePriceTiers.Strings[Item.applePriceTier]);
                }

                BeginErrorBlock(validation, "screenshotPath");
                EditorGUILayout.LabelField("Screenshot path:", Item.screenshotPath);
                EndErrorBlock(validation, "screenshotPath");
                var screenshotButtonBox = EditorGUILayout.BeginVertical();

                var screenshotButtonRect = new Rect(screenshotButtonBox.xMax - ProductCatalogExportWindow.kWidth,
                    screenshotButtonBox.yMin,
                    ProductCatalogExportWindow.kWidth,
                    EditorGUIUtility.singleLineHeight);
                if (GUI.Button(screenshotButtonRect, new GUIContent("Select a screenshot", "Required for Apple XML Delivery.")))
                {
                    var selectedPath = EditorUtility.OpenFilePanel("Select a screenshot", "", "");
                    if (selectedPath != null)
                    {
                        Item.screenshotPath = selectedPath;
                    }

                    GenericEditorButtonClickEventSenderHelpers.SendCatalogSelectAppleScreenshotEvent();
                }

                EditorGUILayout.EndVertical();
            }

            /// <summary>
            /// Sets the validation results upon export of this item.
            /// </summary>
            /// <param name="results"> The validation results of the export. </param>
            public void SetValidationResults(ExporterValidationResults results)
            {
                validation = results;
                if (!validation.Valid)
                {
                    advancedVisible = true;
                    descriptionVisible = true;
                    storeIDsVisible = true;
                    googleVisible = true;
                    appleVisible = true;
                }
            }

            /// <summary>
            /// Sets an error flag if the item's ID is a duplicate of another item's.
            /// </summary>
            /// <param name="isDuplicate"> Whether or not the ID is a duplicate of another item. </param>
            public void SetIDDuplicateError(bool isDuplicate)
            {
                idDuplicate = isDuplicate;
            }

            /// <summary>
            /// Sets an error flag if the item's ID is valid or not.
            /// </summary>
            /// <param name="isValid"> Whether or not the ID is valid. </param>
            public void SetIDInvalidError(bool isValid)
            {
                idInvalid = isValid;
            }

            /// <summary>
            /// Sets a flag if the item should be marked.
            /// </summary>
            /// <param name="marked"> Whether or not the item should be marked. </param>
            public void SetShouldBeMarked(bool marked)
            {
                shouldBeMarked = marked;
            }

            private bool DescriptionEditorGUI(LocalizedProductDescription description, bool showRemoveButton, string fieldValidationPrefix)
            {
                var box = EditorGUILayout.BeginVertical();
                var removeButtonWidth = EditorGUIUtility.singleLineHeight + 2;

                var rect = EditorGUILayout.GetControlRect(true);
                if (showRemoveButton)
                {
                    rect.width -= removeButtonWidth;
                }

                ShowAndProcessLocaleBlockGui(description, fieldValidationPrefix, rect);

                description.Title = ShowEditTextFieldGuiWithValidationErrorBlockAndGetValue(fieldValidationPrefix + ".Title", "Title:", description.Title);
                description.Description = ShowEditTextFieldGuiWithValidationErrorBlockAndGetValue(fieldValidationPrefix + ".Description", "Description:", description.Description);

                var removeButtonRect = new Rect(box.xMax - removeButtonWidth, box.yMin, removeButtonWidth, EditorGUIUtility.singleLineHeight);
                var remove = showRemoveButton
                    && GUI.Button(removeButtonRect, "x")
                    && EditorUtility.DisplayDialog("Delete Translation?",
                        "Are you sure you want to delete this translation?",
                        "Delete",
                        "Do Not Delete");
                EditorGUILayout.EndVertical();
                return remove;
            }

            void ShowAndProcessLocaleBlockGui(LocalizedProductDescription description, string fieldValidationPrefix, Rect rect)
            {
                BeginErrorBlock(validation, fieldValidationPrefix + ".googleLocale");

                var oldLocale = description.googleLocale;
                description.googleLocale = (TranslationLocale)EditorGUI.Popup(rect, "Locale:", (int)description.googleLocale, LocaleExtensions.GetLabelsWithSupportedPlatforms());
                if (oldLocale != description.googleLocale)
                {
                    var localeName = Enum.GetName(typeof(TranslationLocale), description.googleLocale);
                    GenericEditorDropdownSelectEventSenderHelpers.SendCatalogSetTranslationLocaleEvent(localeName);
                }

                EndErrorBlock(validation, fieldValidationPrefix + ".googleLocale");
            }

            string ShowEditTextFieldGuiWithValidationErrorBlockAndGetValue(string fieldName, string label, string oldText)
            {
                BeginErrorBlock(validation, fieldName);
                var newText = ShowEditTextFieldGuiAndGetValue(fieldName, label, oldText);
                EndErrorBlock(validation, fieldName);

                return newText;
            }

            string ShowEditTextFieldGuiAndGetValue(string fieldName, string label, string oldText)
            {
                var newText = EditorGUILayout.TextField(label, oldText);

                if (newText != oldText)
                {
                    GenericEditorFieldEditEventSenderHelpers.SendCatalogEditEvent(fieldName);
                }

                return newText;
            }

            double ShowEditDoubleFieldGuiAndGetValue(string fieldName, string label, double oldAmount)
            {
                var newAmount = EditorGUILayout.DoubleField(label, oldAmount);

                if (newAmount != oldAmount)
                {
                    GenericEditorFieldEditEventSenderHelpers.SendCatalogEditEvent(fieldName);
                }

                return newAmount;
            }

            private static string TruncateString(string s, int len)
            {
                if (string.IsNullOrEmpty(s))
                {
                    return s;
                }

                if (len < 0)
                {
                    return string.Empty;
                }

                return s.Substring(0, Math.Min(s.Length, len));
            }
        }

        /// <summary>
        /// A popup window that shows a list of exporters and kicks off an export from the ProductCatalogEditor.
        /// </summary>
        public class ProductCatalogExportWindow : PopupWindowContent
        {
            /// <summary>
            /// The default width of the export window.
            /// </summary>
            public const float kWidth = 200f;

            private readonly ProductCatalogEditor editor;
            private readonly List<IProductCatalogExporter> exporters = new List<IProductCatalogExporter>();

            /// <summary>
            /// Constructor taking an instance of <c>ProductCatalogEditor</c> to export contents from.
            /// </summary>
            /// <param name="editor_"> The product catalog editor from which the catalog will be exported. </param>
            public ProductCatalogExportWindow(ProductCatalogEditor editor_)
            {
                editor = editor_;

                exporters.Add(new AppleXMLProductCatalogExporter());
                exporters.Add(new GooglePlayProductCatalogExporter());
            }

            /// <summary>
            /// Gets the dimensions of the window.
            /// </summary>
            /// <returns>The size of the window as a 2D vector.</returns>
            public override Vector2 GetWindowSize()
            {
                return new Vector2(kWidth, EditorGUIUtility.singleLineHeight * (exporters.Count + 1));
            }

            /// <summary>
            /// Function called when the GUI updates.
            /// </summary>
            /// <param name="rect">The current draw rectangle of the Window's GUI.</param>
            public override void OnGUI(Rect rect)
            {
                if (editor == null)
                {
                    editorWindow.Close();
                    return;
                }

                EditorGUILayout.BeginVertical();
                foreach (var exporter in exporters)
                {
                    if (GUILayout.Button(exporter.DisplayName))
                    {
                        editorWindow.Close();
                        Export(exporter);
                        GenericEditorButtonClickEventSenderHelpers.SendCatalogAppStoreExportEvent(exporter.DisplayName);
                    }
                }

                EditorGUILayout.EndVertical();
            }

            private bool Validate(IProductCatalogExporter exporter, out ExporterValidationResults catalogValidation,
                out List<ExporterValidationResults> itemValidation, bool debug = false)
            {
                var valid = true;
                catalogValidation = exporter.Validate(editor.Catalog);
                valid = valid && catalogValidation.Valid;
                itemValidation = new List<ExporterValidationResults>();

                foreach (var item in editor.Catalog.allProducts)
                {
                    var v = exporter.Validate(item);
                    valid = valid && v.Valid;
                    itemValidation.Add(v);
                }

                if (debug)
                {
                    void DebugResults(string name, ExporterValidationResults r)
                    {
                        if (!r.Valid || r.warnings.Count != 0)
                        {
                            Debug.unityLogger.LogIAPWarning(name + ", Valid = " + r.Valid);
                        }

                        foreach (var m in r.errors)
                        {
                            Debug.unityLogger.LogIAPWarning("errors " + m);
                        }

                        foreach (var m in r.fieldErrors)
                        {
                            Debug.unityLogger.LogIAPWarning("fieldErrors " + m);
                        }

                        foreach (var m in r.warnings)
                        {
                            Debug.unityLogger.LogIAPWarning("warnings " + m);
                        }
                    }

                    if (!valid)
                    {
                        Debug.unityLogger.LogIAPWarning("Product Catalog Export Overall Result: invalid");
                    }

                    DebugResults("CatalogValidation", catalogValidation);
                    foreach (var r in itemValidation)
                    {
                        DebugResults("ItemValidation", r);
                    }
                }

                return valid;
            }

            private void Export(IProductCatalogExporter exporter)
            {

                var valid = Validate(exporter, out var catalogValidation, out var itemValidation, kValidateDebugLog);
                editor.SetCatalogValidationResults(catalogValidation, itemValidation);

                if (valid)
                {
                    string nonInteractivePath = null;

                    // Special case for exporters that need to export an entire package with a given name, not just a file.
                    if (exporter.SaveCompletePackage && !string.IsNullOrEmpty(exporter.MandatoryExportFolder))
                    {
                        // Choose the location of the final directory
                        var directoryPath = EditorUtility.SaveFolderPanel("Export to folder", "", "");
                        directoryPath = Path.Combine(directoryPath, exporter.MandatoryExportFolder);

                        // Replace any existing directory
                        if (Directory.Exists(directoryPath))
                        {
                            Directory.Delete(directoryPath, true);
                        }

                        Directory.CreateDirectory(directoryPath);

                        // ExportHelper needs a single file, let it create the main file and save the auxilliary files.
                        var mainFilePath = Path.Combine(directoryPath,
                            string.Format("{0}.{1}", exporter.DefaultFileName, exporter.FileExtension));
                        ExportHelper(exporter, mainFilePath);
                        EditorUtility.DisplayDialog(
                            "Exported Successfully",
                            string.Format("Exported {0} to \"{1}\".",
                                exporter.MandatoryExportFolder, directoryPath),
                            "OK");
                    }
                    else
                    {
                        // Export manually
                        var path = EditorUtility.SaveFilePanel("Export Product Catalog", "", exporter.DefaultFileName,
                            exporter.FileExtension);
                        ExportHelper(exporter, path);

                        // Export automatically, conditionally
                        if (!string.IsNullOrEmpty(exporter.MandatoryExportFolder))
                        {
                            // Always save a copy to the mandatory folder
                            if (!Directory.Exists(exporter.MandatoryExportFolder))
                            {
                                Directory.CreateDirectory(exporter.MandatoryExportFolder);
                            }

                            nonInteractivePath = Path.Combine(exporter.MandatoryExportFolder,
                                string.Format("{0}.{1}", exporter.DefaultFileName, exporter.FileExtension));
                            ExportHelper(exporter, nonInteractivePath);
                        }

                        if (nonInteractivePath != null)
                        {
                            EditorUtility.DisplayDialog(
                                "Exported Successfully",
                                string.Format("Exported {0} to \"{2}\".\n\n" +
                                    "Also saved copy into project at \"{1}\".",
                                    exporter.DisplayName, nonInteractivePath, path),
                                "OK");
                        }
                    }
                }
            }

            private void ExportHelper(IProductCatalogExporter exporter, string path)
            {
                if (string.IsNullOrEmpty(path))
                {
                    return;
                }

                File.WriteAllText(path, exporter.Export(editor.Catalog));

                if (exporter.FilesToCopy != null)
                {
                    foreach (var fileToCopy in exporter.FilesToCopy)
                    {
                        var targetPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(fileToCopy));
                        var fileInfo = new FileInfo(fileToCopy);
                        fileInfo.CopyTo(targetPath, true);
                    }
                }
            }

            /// <summary>
            /// Not user-facing. Use to generate a catalog without asking the user. Make a best-effort
            /// to fix issues.
            /// </summary>
            /// <param name="storeName"></param>
            /// <param name="folder"></param>
            /// <param name="justEraseExport"></param>
            /// <returns></returns>
            internal bool Export(string storeName, string folder, bool justEraseExport)
            {
                var catalog = editor.Catalog; // This may be normalized before export

                var exporter = exporters.Single(e => e.StoreName == storeName);

                if (!Directory.Exists(folder))
                {
                    Directory.CreateDirectory(folder);
                }

                var path = Path.Combine(folder,
                    string.Format("{0}.{1}", exporter.DefaultFileName, exporter.FileExtension));

                if (justEraseExport)
                {
                    File.Delete(path);
                    return false;
                }

                var valid = Validate(exporter, out var catalogValidation, out var itemValidation, kValidateDebugLog);

                if (!valid)
                {
                    Debug.unityLogger.LogIAPWarning($"{storeName} Product Catalog is invalid. Automatically " +
                        "fixing for export. Manually fix Catalog errors by opening IAP Catalog editor window with " +
                        $"{ProductCatalogEditorMenuPath} menu, performing App Store Export for this store, and " +
                        "resolving reported issues.");
                    catalog = exporter.NormalizeToType(catalog);
                }

                var wrote = false;

                if (!string.IsNullOrEmpty(path))
                {
                    var cat = exporter.Export(catalog);

                    // Write the path
                    File.WriteAllText(path, cat);
                    AssetDatabase.ImportAsset(path);

                    wrote = true;
                }
                else
                {
                    Debug.unityLogger.LogIAPError($"Unable to export {storeName} Product Catalog. Path " +
                        $"{path} is invalid.");
                }

                return wrote;
            }
        }

        /// <summary>
        /// Exporters return an instance of ExporterValidationResults to indicate whether a ProductCatalog or
        /// ProductCatalogItem can be correctly exported.
        /// </summary>
        public class ExporterValidationResults
        {
            /// <summary>
            /// Property that checks if the export results are valid.
            /// </summary>
            public bool Valid => errors.Count == 0 && fieldErrors.Count == 0;

            /// <summary>
            /// The list of errors.
            /// </summary>
            public List<string> errors = new List<string>();

            /// <summary>
            /// The list of warnings.
            /// </summary>
            public List<string> warnings = new List<string>();

            /// <summary>
            /// The dictionary of field errors.
            /// </summary>
            public Dictionary<string, string> fieldErrors = new Dictionary<string, string>();
        }

        /// <summary>
        /// Product catalog exporters implement this interface to provide validation and export of a ProductCatalog.
        /// </summary>
        public interface IProductCatalogExporter
        {
            /// <summary>
            /// The display name of the catalog.
            /// </summary>
            string DisplayName { get; }

            /// <summary>
            /// The default file name of the catalog export.
            /// </summary>
            string DefaultFileName { get; }

            /// <summary>
            /// The file extension of the catalog export.
            /// </summary>
            string FileExtension { get; }

            /// <summary>
            /// The name of the store to be exported.
            /// </summary>
            string StoreName { get; }

            /// <summary>
            /// Required specific path for output file. Is optional whether user will be permitted to save a copy
            /// to a separate path in addition to this required path.
            /// </summary>
            string MandatoryExportFolder { get; }

            /// <summary>
            /// Exports the product catalog.
            /// </summary>
            /// <param name="catalog"> The <c>ProductCatalog</c> to be exported. </param>
            /// <returns> The exported catalog as raw text. </returns>
            string Export(ProductCatalog catalog);

            /// <summary>
            /// Validates the product catalog for export.
            /// </summary>
            /// <param name="catalog"> The <c>ProductCatalog</c> to be exported. </param>
            /// <returns> The results of the validation. </returns>
            ExporterValidationResults Validate(ProductCatalog catalog);

            /// <summary>
            /// Validates the product catalog item for export.
            /// </summary>
            /// <param name="item"> The <c>ProductCataloItemg</c> to be exported. </param>
            /// <returns> The results of the validation. </returns>
            ExporterValidationResults Validate(ProductCatalogItem item);

            /// <summary>
            /// Normalizes the product catalog for export to the base type.
            /// Fixes issues targeting this exporter's implempentation.
            /// </summary>
            /// <param name="catalog"> The <c>ProductCatalog</c> to be normalized. </param>
            /// <returns> The normalized <c>ProductCatalog</c>. </returns>
            ProductCatalog NormalizeToType(ProductCatalog catalog);

            /// <summary>
            /// Files to copy to the final directory, ex. screenshots on iOS
            /// </summary>
            List<string> FilesToCopy { get; }

            /// <summary>
            /// True if the exporter should save an entire package/folder (specified by MandatoryExportFolder and FilesToCopy,
            /// not just a single file. This will present a Directory picker, not a File picker. The DefaultFileName will be
            /// used for the main file in the MandatoryExportFolder, and any FilesToCopy will be placed in that folder as well.
            /// </summary>
            bool SaveCompletePackage { get; }
        }

        /// <summary>
        /// Workaround toggleOnLabelClick not being supported correctly until Unity 5.5
        /// See https://ono.unity3d.com/unity/unity/changeset/9f5bb2308eb90fb8276f49033a5b31f66cd4faa3 (5.5.0b2)
        /// </summary>
        protected static class CompatibleGUI
        {
            /// <summary>
            /// Whether or not the GUI item has been folded out.
            /// </summary>
            public static bool parsedVersion = false;

            /// <summary>
            /// Folds out the GUI item.
            /// </summary>
            /// <param name="foldout"> The shown foldout state. </param>
            /// <param name="text"> The label to show. </param>
            /// <param name="toggleOnLabelClick"> Optional GUIStyle. </param>
            /// <param name="style"> Specifies whether clicking the label toggles the foldout state. The default value is false. Set to true to include the label in the clickable area. </param>
            /// <returns> The foldout state selected by the user. If true, you should render sub-objects. </returns>
            public static bool Foldout(bool foldout, string text, bool toggleOnLabelClick, GUIStyle style)
            {
                if (!parsedVersion)
                {
                    parsedVersion = true;
                }

                // Helper is required to be a separate scope to avoid Unity linker from failing to bind it
                // and propagating the fatal error upwards.
                return FoldoutHelper(foldout, text, toggleOnLabelClick, style);
            }

            /// <summary>
            /// Helper that folds out the GUI item.
            /// Exists to fix a linker binding error with Foldout.
            /// </summary>
            /// <param name="foldout"> The shown foldout state. </param>
            /// <param name="text"> The label to show. </param>
            /// <param name="toggleOnLabelClick"> Optional GUIStyle. </param>
            /// <param name="style"> Specifies whether clicking the label toggles the foldout state. The default value is false. Set to true to include the label in the clickable area. </param>
            /// <returns> The foldout state selected by the user. If true, you should render sub-objects. </returns>
            public static bool FoldoutHelper(bool foldout, string text, bool toggleOnLabelClick,
                GUIStyle style)
            {
                return EditorGUILayout.Foldout(foldout, text, toggleOnLabelClick, style);
            }
        }
    }
}
