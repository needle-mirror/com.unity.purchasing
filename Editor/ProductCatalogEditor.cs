using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Editor;
using UnityEditor.Connect;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.Purchasing;

namespace UnityEditor.Purchasing
{
    /// <summary>
    /// Editor window for displaying and editing the contents of the default ProductCatalog.
    /// </summary>
    public class ProductCatalogEditor : EditorWindow
    {
        static readonly string[] kStoreKeys =
        {
            AppleAppStore.Name,
            GooglePlay.Name,
            AmazonApps.Name,
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

        static readonly GUIContent windowTitle = new GUIContent("IAP Catalog");
        static readonly List<ProductCatalogItemEditor> productEditors = new List<ProductCatalogItemEditor>();
        static readonly List<ProductCatalogItemEditor> toRemove = new List<ProductCatalogItemEditor>();

        DateTime lastChanged;
        bool dirty;
        readonly TimeSpan kSaveDelay = new TimeSpan(0, 0, 0, 0, 500); // 500 milliseconds

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

        void OnDisable()
        {
            if (dirty)
            {
                Save();
            }
        }

        void Update()
        {
            if (dirty && DateTime.Now.Subtract(lastChanged) > kSaveDelay)
            {
                Save();
            }
        }

        void SetDirtyFlag()
        {
            lastChanged = DateTime.Now;
            dirty = true;
        }

        void Save()
        {
            dirty = false;
            File.WriteAllText(ProductCatalog.kCatalogPath, ProductCatalog.Serialize(Catalog));

            AssetDatabase.ImportAsset(ProductCatalog.kCatalogPath);
        }

        Vector2 scrollPosition;

        void OnGUI()
        {
            scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition, false, false, GUI.skin.horizontalScrollbar,
                GUI.skin.verticalScrollbar, GUI.skin.box);

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

            if (EditorGUI.EndChangeCheck())
            {
                CheckForDuplicateIDs();
                SetDirtyFlag();
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

        void ShowAndProcessCodelessAutoInitToggleGuis()
        {
            EditorGUILayout.Space();

            ShowAndProcessIapAutoInitToggleGui();
            if (Catalog.enableCodelessAutoInitialization)
            {
                ShowAndProcessUgsAutoInitToggleGui();
            }

            EditorGUILayout.Space();
        }

        void ShowAndProcessIapAutoInitToggleGui()
        {
            var newValue = EditorGUILayout.Toggle(
                new GUIContent("Automatically initialize UnityPurchasing (recommended)",
                    "Automatically start Unity IAP if there are any products defined in this catalog. Uncheck this if you plan to initialize Unity IAP manually in your code."),
                Catalog.enableCodelessAutoInitialization);

            UpdateIapAutoInitValue(newValue);
        }

        void UpdateIapAutoInitValue(bool newValue)
        {
            if (newValue != Catalog.enableCodelessAutoInitialization)
            {
                Catalog.enableCodelessAutoInitialization = newValue;

                GenericEditorClickCheckboxEventSenderHelpers.SendCatalogAutoInitToggleEvent(newValue);
            }
        }

        void ShowAndProcessUgsAutoInitToggleGui()
        {
            var newValue = EditorGUILayout.Toggle(new GUIContent(
                    "Automatically initialize Unity Gaming Services",
                    "This initializes Unity Gaming Services with the default `production` environment.\n" +
                    "This way of initializing Unity Gaming Services might not be compatible with all other services as they might require special initialization options.\n" +
                    "If the use of initialization options is needed, Unity Gaming Services should be initialized with the coded API."),
                Catalog.enableUnityGamingServicesAutoInitialization);

            UpdateUgsAutoInitValue(newValue);
        }

        void UpdateUgsAutoInitValue(bool newValue)
        {
            if (newValue != Catalog.enableUnityGamingServicesAutoInitialization)
            {
                Catalog.enableUnityGamingServicesAutoInitialization = newValue;

                GenericEditorClickCheckboxEventSenderHelpers.SendCatalogUgsAutoInitToggleEvent(newValue);
            }
        }

        static bool IsNullOrWhiteSpace(string value)
        {
            return string.IsNullOrEmpty(value?.Trim());
        }

        void ValidateProductIds()
        {
            foreach (var productEditor in productEditors)
            {
                if (IsNullOrWhiteSpace(productEditor.Item.id))
                {
                    productEditor.SetIDInvalidError(true);
                }
            }
        }

        void AddNewProduct()
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

        void CheckForDuplicateIDs()
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

        static void ShowValidationResultsGUI(ExporterValidationResults results)
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

        static void BeginErrorBlock()
        {
            EditorGUI.BeginChangeCheck();
        }

        static void EndErrorBlock(ExporterValidationResults validation, string fieldName)
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
        /// Inner class for displaying and editing the contents of a single entry in the ProductCatalog.
        /// </summary>
        public class ProductCatalogItemEditor
        {
            const float k_DuplicateIDFieldWidth = 90f;

            /// <summary>
            /// Property which gets the <c>ProductCatalogItem</c> instance being edited.
            /// </summary>
            public ProductCatalogItem Item { get; private set; }

            ExporterValidationResults validation;

            readonly bool editorSupportsPayouts;

            bool advancedVisible = true;
            bool descriptionVisible = true;
            bool storeIDsVisible;
            bool payoutsVisible;

            bool idDuplicate;
            bool idInvalid;
            bool shouldBeMarked = true;

            readonly List<LocalizedProductDescription> descriptionsToRemove = new List<LocalizedProductDescription>();
            readonly List<ProductCatalogPayout> payoutsToRemove = new List<ProductCatalogPayout>();

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

                        ShowAndProcessGooglePrice();

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

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }

            void ShowAndProcessProductIDBlockGui(Rect idRect)
            {
                BeginErrorBlock();

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
                BeginErrorBlock();

                var oldType = Item.type;

                var typeRect = EditorGUILayout.GetControlRect(true);
                typeRect.width = width;
                Item.type = (ProductType)EditorGUI.EnumPopup(typeRect, "Type:", Item.type);

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

            void ShowAndProcessGooglePrice()
            {
                var fieldName = "googlePrice";
                BeginErrorBlock();
                var priceStr = ShowEditTextFieldGuiAndGetValue(fieldName, "Price:", Item.googlePrice == null || Item.googlePrice.value == 0 ? string.Empty : Item.googlePrice.value.ToString());

                Item.googlePrice.value = decimal.TryParse(priceStr, out var priceDecimal) ? priceDecimal : 0;

                EndErrorBlock(validation, fieldName);
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

            bool DescriptionEditorGUI(LocalizedProductDescription description, bool showRemoveButton, string fieldValidationPrefix)
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
                BeginErrorBlock();

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
                BeginErrorBlock();
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

            static string TruncateString(string s, int len)
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
