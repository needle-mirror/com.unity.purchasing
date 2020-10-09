#define UNITY_UNIFIED_IAP

using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
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

        private static string[] kStoreKeys = {
            AppleAppStore.Name,
            GooglePlay.Name,
            SamsungApps.Name,
            AmazonApps.Name,
            MacAppStore.Name,
            WindowsStore.Name,
            UDP.Name
        };

        public const string ProductCatalogEditorMenuPath = "Window/Unity IAP/IAP Catalog";
        [MenuItem(ProductCatalogEditorMenuPath, false, 5)]
        public static void ShowWindow()
        {
            EditorWindow.GetWindow(typeof(ProductCatalogEditor));
        }

        private static GUIContent windowTitle = new GUIContent("IAP Catalog");
        private static List<ProductCatalogItemEditor> productEditors = new List<ProductCatalogItemEditor>();
        private static List<ProductCatalogItemEditor> toRemove = new List<ProductCatalogItemEditor>();
        private ProductCatalog catalog;
        private Rect exportButtonRect;
        private ExporterValidationResults validation;
        private bool enableCodelessAutoInitialization;

        private DateTime lastChanged;
        private bool dirty;
        private readonly TimeSpan kSaveDelay = new TimeSpan (0, 0, 0, 0, 500); // 500 milliseconds


        #region UDP Related Fields

        private static bool kValidLogin = true; // User needs to login to Unity first.
        private static bool kValidConfig = true; // User needs to have clientID for the game.

        private static readonly Queue<ReqStruct> requestQueue = new Queue<ReqStruct>();

        private static bool kIsPreparing = true;
        private static TokenInfo kTokenInfo  = new TokenInfo();
        private static string kOrgId;
        private static object kAppStoreSettings; //UDP AppStoreSettings via Reflection
        private static IDictionary<string, IapItem> kIapItems = new Dictionary<string, IapItem>();
        private static readonly bool s_udpAvailable = UdpSynchronizationApi.CheckUdpAvailability();
        private static string kUdpErrorMsg = "";

        #endregion

        /// <summary>
        /// Since we are changing the product catalog's location, it may be necessary to migrate existing product
        /// catalog to the new product catalog location.
        /// </summary>
        [InitializeOnLoadMethod]
        internal static void MigrateProductCatalog()
        {
            try
            {
                FileInfo file = new FileInfo(ProductCatalog.kCatalogPath);
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
                Debug.LogException(ex);
            }
        }

        internal static bool DoesPrevCatalogPathExist()
        {
            return File.Exists(ProductCatalog.kPrevCatalogPath);
        }

        public ProductCatalog Catalog {
            get {
                return catalog;
            }
        }

        public void SetCatalogValidationResults(ExporterValidationResults catalogResults,
            List<ExporterValidationResults> itemResults)
        {
            validation = catalogResults;

            if (productEditors.Count == itemResults.Count)
            {
                for (int i = 0; i < productEditors.Count; ++i)
                {
                    productEditors[i].SetValidationResults(itemResults[i]);
                }
            }
        }

        void Awake()
        {
            catalog = ProductCatalog.LoadDefaultCatalog();
            if (catalog.allProducts.Count == 0)
            {
                AddNewProduct(); // Start the catalog with one item
            }
        }

        void OnEnable()
        {
            titleContent = windowTitle;

            productEditors.Clear();
            foreach (var product in catalog.allProducts)
            {
                productEditors.Add(new ProductCatalogItemEditor(product));
            }

            enableCodelessAutoInitialization = catalog.enableCodelessAutoInitialization;

            if (s_udpAvailable && IsUdpInstalled())
            {
        	    kUdpErrorMsg = "";
            	kTokenInfo = new TokenInfo();
                kValidLogin = true;
	            kValidConfig = true;
    	        kIsPreparing = true;
        	    kOrgId = null;
            	PrepareDeveloperInfo();
            }
        }

		private static bool IsUdpInstalled()
		{
			return UnityPurchasingEditor.IsUdpUmpPackageInstalled() || UnityPurchasingEditor.IsUdpAssetStorePackageInstalled();
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

            CheckApiUpdate();
        }

        private void SetDirtyFlag()
        {
            lastChanged = DateTime.Now;
            dirty = true;
        }

        private void Save()
        {
            dirty = false;
            File.WriteAllText(ProductCatalog.kCatalogPath, ProductCatalog.Serialize(catalog));
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
            }

            EditorGUILayout.EndScrollView();

            EditorGUILayout.BeginVertical();
            float defaultLabelWidth = EditorGUIUtility.labelWidth;
            EditorGUIUtility.labelWidth = 310;

            bool catalogHasProducts = !catalog.IsEmpty();
            if (catalogHasProducts)
            {
                EditorGUILayout.Space();
                enableCodelessAutoInitialization = EditorGUILayout.Toggle(
                    new GUIContent("Automatically initialize UnityPurchasing (recommended)",
                        "Automatically start Unity IAP if there are any products defined in this catalog. Uncheck this if you plan to initialize Unity IAP manually in your code."),
                    enableCodelessAutoInitialization);
                catalog.enableCodelessAutoInitialization = enableCodelessAutoInitialization;
                EditorGUILayout.Space();
            }

            EditorGUILayout.EndVertical();

            var exportBox = EditorGUILayout.BeginVertical();
            EditorGUIUtility.labelWidth = defaultLabelWidth;

            EditorGUILayout.LabelField("Catalog Export");
            BeginErrorBlock(validation, "appleSKU");
            catalog.appleSKU = EditorGUILayout.TextField("Apple SKU:", catalog.appleSKU);
            EndErrorBlock(validation, "appleSKU");

            BeginErrorBlock(validation, "appleTeamID");
            catalog.appleTeamID = EditorGUILayout.TextField("Apple Team ID:", catalog.appleTeamID);
            EndErrorBlock(validation, "appleTeamID");

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
                new GUIContent("App Store Export", "Export products for bulk import into app store tools")))
            {
                PopupWindow.Show(exportButtonRect, new ProductCatalogExportWindow(this));
            }

            //Upload to dashboard depends on extension code that is only available in version 5.6 and later
            //if (VersionCheck.GreaterThanOrEqual(Application.unityVersion, "5.6.0")) {
            //	if (GUILayout.Button(new GUIContent("Upload to Unity Cloud", "Upload this product catalog to the Unity Cloud dashboard"))
            //	    && EditorUtility.DisplayDialog("Upload to Unity Cloud?",
            //	                                   "Are you sure you want to upload the catalog? This upload will replace any product catalog already uploaded to the Unity Cloud.",
            //	                                   "Upload",
            //	                                   "Cancel")) {
            //
            //		try {
            //			UploadToCloud();
            //		} catch (FileNotFoundException) {
            //			Debug.LogError("Upload to Unity Cloud is only available in version 5.6 and later");
            //		}
            //	}
            //}

            EditorGUILayout.EndVertical();

            if (toRemove.Count > 0)
            {
                productEditors.RemoveAll(x => toRemove.Contains(x));
                foreach (var editor in toRemove)
                {
                    catalog.Remove(editor.Item);
                }

                toRemove.Clear();
                SetDirtyFlag();
            }
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

            if (invalidIdsExist) return;

            var newEditor = new ProductCatalogItemEditor();
            newEditor.SetShouldBeMarked(false);
            productEditors.Add(newEditor);
            catalog.Add(newEditor.Item);
        }

        private void CheckForDuplicateIDs()
        {
            var ids = new HashSet<string>();
            var duplicates = new HashSet<string>();
            foreach (var product in catalog.allProducts)
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

        private void UploadToCloud()
        {
            var exporter = new CloudJSONProductCatalogExporter();
            bool valid = true;

            var catalogValidation = exporter.Validate(catalog);
            valid = valid && catalogValidation.Valid;

            var itemValidation = new List<ExporterValidationResults>();
            foreach (var item in catalog.allProducts)
            {
                var v = exporter.Validate(item);
                valid = valid && v.Valid;
                itemValidation.Add(v);
            }

            SetCatalogValidationResults(catalogValidation, itemValidation);

            if (valid)
            {
                UnityEditor.Purchasing.CloudCatalogUploader.Upload(exporter.Export(catalog),
                    (completeEvents) =>
                    {
                        if (completeEvents.Error != null)
                        {
                            throw completeEvents.Error;
                        }
                        else
                        {
                            Debug.Log("Catalog upload complete");
                        }
                    },
                    (progressEvents) =>
                    {
                        Debug.Log(string.Format("Catalog upload progress: {0}% complete",
                            progressEvents.ProgressPercentage.ToString()));
                    },
                    "https://cloud-staging.uca.cloud.unity3d.com");
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
            if (EditorGUI.EndChangeCheck() && validation != null) {
                validation.fieldErrors.Remove(fieldName);
            }

            if (validation != null && validation.fieldErrors.ContainsKey(fieldName)) {
                var style = new GUIStyle();
                style.normal.textColor = Color.red;
                EditorGUILayout.LabelField(validation.fieldErrors[fieldName], style);
            }
        }

        public static bool Export(string storeName, string folder, bool eraseExport)
        {
            var editor = ScriptableObject.CreateInstance(typeof(ProductCatalogEditor)) as ProductCatalogEditor;
            return new ProductCatalogExportWindow(editor).Export(storeName, folder, eraseExport);
        }

        #region UDP Related Functions

        // This method is used in Update() to check the UnityWebRequest each frame
        private void CheckApiUpdate()
        {
            if (requestQueue.Count == 0)
            {
                return;
            }

            ReqStruct reqStruct = requestQueue.Dequeue();
            object request = reqStruct.request;
            GeneralResponse resp = reqStruct.resp;

            if (request != null && UdpSynchronizationApi.IsUnityWebRequestDone(request)) // Check what the request is and parse the response
            {
                // Deal with errors
                if (UdpSynchronizationApi.UnityWebRequestError(request) != null || UdpSynchronizationApi.UnityWebRequestResponseCode(request)/100 != 2)
                {

                    ErrorResponse response = JsonUtility.FromJson<ErrorResponse>(UdpSynchronizationApi.UnityWebRequestResultString(request));
                    if (response?.message != null && response.details != null && response.details.Length != 0)
                    {
                        kUdpErrorMsg = string.Format("{0} : {1}", response.details[0].field, response.message);
                    }
                    else if (response?.message != null)
                    {
                        kUdpErrorMsg = response.message;
                    }
                    else
                    {
                        kUdpErrorMsg = "Unknown Error, Please try again";
                    }

                    if (reqStruct.itemEditor != null)
                    {
                        kIsPreparing = false;
                        reqStruct.itemEditor.udpItemSyncing = false;
                        reqStruct.itemEditor.udpSyncErrorMsg = kUdpErrorMsg;
                        kUdpErrorMsg = "";
                    }
                }
                // No error.
                else
                {
                    if (resp.GetType() == typeof(TokenInfo))
                    {
                        resp = JsonUtility.FromJson<TokenInfo>(UdpSynchronizationApi.UnityWebRequestResultString(request));
                        kTokenInfo.access_token = ((TokenInfo) resp).access_token;
                        kTokenInfo.refresh_token = ((TokenInfo) resp).refresh_token;

                        var newRequest =
                            UdpSynchronizationApi.GetOrgId(kTokenInfo.access_token, Application.cloudProjectId);
                        ReqStruct newReqStruct = new ReqStruct {request = newRequest, resp = new OrgIdResponse()};

                        requestQueue.Enqueue(newReqStruct);
                    }
                    // Get orgId request
                    else if (resp.GetType() == typeof(OrgIdResponse))
                    {
                        resp = JsonUtility.FromJson<OrgIdResponse>(UdpSynchronizationApi.UnityWebRequestResultString(request));
                        kOrgId = ((OrgIdResponse) resp).org_foreign_key;

            			if (kAppStoreSettings != null)
					    {
                        	var appSlug = AppStoreSettingsInterface.GetAppSlugField();

                        	// Then, get all iap items
                        	requestQueue.Enqueue(new ReqStruct
                        	{
                        	    request = UdpSynchronizationApi.SearchStoreItem(kTokenInfo.access_token, kOrgId, (string)appSlug.GetValue(kAppStoreSettings)),
                        	    resp = new IapItemSearchResponse()
                        	});
						}
                    }

                    else if (resp.GetType() == typeof(IapItemSearchResponse))
                    {
                        if (UdpSynchronizationApi.UnityWebRequestResultString(request) != null)
                        {
                            resp = JsonUtility.FromJson<IapItemSearchResponse>(UdpSynchronizationApi.UnityWebRequestResultString(request));
                            foreach (var item in ((IapItemSearchResponse) resp).results)
                            {
                                kIapItems[item.slug] = item;
                            }
                        }

                        kIsPreparing = false;
                    }
                    // Creating/Updating IAP item succeeds
                    else if (resp.GetType() == typeof(IapItemResponse))
                    {
                        resp = JsonUtility.FromJson<IapItemResponse>(UdpSynchronizationApi.UnityWebRequestResultString(request));

                        if (reqStruct.iapItem != null) // this should always be true
                        {
                            reqStruct.itemEditor.udpItemSyncing = false;
                            kIapItems[reqStruct.iapItem.slug] = reqStruct.iapItem;
                            kIapItems[reqStruct.iapItem.slug].id = ((IapItemResponse) resp).id;
                        }
                    }
                }
                Repaint();
            }
            else
            {
                requestQueue.Enqueue(reqStruct);
            }
        }



        /// <summary>
        /// Get userId, orgId of the developer. Make prepare for syncing
        /// </summary>
        void PrepareDeveloperInfo()
        {
            // Get Client ID
            Type udpAppStoreSettings = AppStoreSettingsInterface.GetClassType();
            if (udpAppStoreSettings != null)
            {
                var assetPathProp = AppStoreSettingsInterface.GetAssetPathField();
              	var clientIDProp = AppStoreSettingsInterface.GetClientIDField();

                kAppStoreSettings = AssetDatabase.LoadAssetAtPath((string)assetPathProp.GetValue(null), udpAppStoreSettings);

                if (kAppStoreSettings == null || clientIDProp.GetValue(kAppStoreSettings) == null)
                {
                    kUdpErrorMsg = "Please create and sync GameSettings.asset first.";
                    kValidConfig = false;
                    return;
                }
            }

            // Using reflection to get AuthCode to avoid
            Type unityOAuthType = UdpSynchronizationApi.GetUnityOAuthType();
            Type authCodeResponseType = unityOAuthType.GetNestedType("AuthCodeResponse", BindingFlags.Public);
            var performMethodInfo =
                typeof(ProductCatalogEditor).GetMethod("GetAuthCode").MakeGenericMethod(authCodeResponseType);
            var actionT =
                typeof(Action<>).MakeGenericType(authCodeResponseType); // Action<UnityOAuth.AuthCodeResponse>
            var getAuthorizationCodeAsyncMethodInfo = unityOAuthType.GetMethod("GetAuthorizationCodeAsync");
            var performDelegate = Delegate.CreateDelegate(actionT, this, performMethodInfo);
            try
            {
                getAuthorizationCodeAsyncMethodInfo.Invoke(null,
                    new object[] {UdpSynchronizationApi.kOAuthClientId, performDelegate});
            }
            catch (TargetInvocationException ex)
            {
                if (ex.InnerException is InvalidOperationException)
                {
                    kUdpErrorMsg = "To sync UDP catalog, you must login with Unity ID first.";
                    kValidLogin = false;
                    kIsPreparing = false;
                }
            }
        }

        public void GetAuthCode<T>(T response)
        {
            var authCodePropertyInfo = response.GetType().GetProperty("AuthCode");
            var exceptionPropertyInfo = response.GetType().GetProperty("Exception");
            string authCode = (string) authCodePropertyInfo.GetValue(response, null);
            Exception exception = (Exception) exceptionPropertyInfo.GetValue(response, null);

            if (authCode != null)
            {
                object request = UdpSynchronizationApi.GetAccessToken(authCode);
                TokenInfo tokenInfoResp = new TokenInfo();
                ReqStruct reqStruct = new ReqStruct {request = request, resp = tokenInfoResp};
                requestQueue.Enqueue(reqStruct);
            }
            else
            {
                kUdpErrorMsg = exception.ToString();
                kIsPreparing = false;
            }
        }

        #endregion

        /// <summary>
        /// Inner class for displaying and editing the contents of a single entry in the ProductCatalog.
        /// </summary>
        public class ProductCatalogItemEditor
        {
            public ProductCatalogItem Item { get; private set; }

            private ExporterValidationResults validation;

            private bool editorSupportsPayouts = false;

            private bool advancedVisible = true;
            private bool descriptionVisible = true;
            private bool storeIDsVisible = false;
            private bool payoutsVisible = false;
            private bool googleVisible = false;
            private bool appleVisible = false;
            private bool udpVisible = false;

            private bool idDuplicate = false;
            private bool idInvalid = false;
            private bool shouldBeMarked = true;
            public Boolean udpItemSyncing = false;

            public string udpSyncErrorMsg = "";

            private List<LocalizedProductDescription> descriptionsToRemove = new List<LocalizedProductDescription>();
            private List<ProductCatalogPayout> payoutsToRemove = new List<ProductCatalogPayout>();

            public ProductCatalogItemEditor()
            {
                this.Item = new ProductCatalogItem();

                editorSupportsPayouts = (null != typeof(ProductDefinition).GetProperty("payouts"));
            }

            public ProductCatalogItemEditor(ProductCatalogItem description)
            {
                this.Item = description;
                editorSupportsPayouts = (null != typeof(ProductDefinition).GetProperty("payouts"));
            }

            public void OnGUI()
            {
                GUIStyle s = new GUIStyle(EditorStyles.foldout);
                var box = EditorGUILayout.BeginVertical();

                Rect rect = new Rect(box.xMax - EditorGUIUtility.singleLineHeight - 2, box.yMin, EditorGUIUtility.singleLineHeight + 2, EditorGUIUtility.singleLineHeight);
                if (GUI.Button(rect, "x") && EditorUtility.DisplayDialog("Delete Product?",
                                                                         "Are you sure you want to delete this product?",
                                                                         "Delete",
                                                                         "Do Not Delete")) {
                    toRemove.Add(this);
                }

                ShowValidationResultsGUI(validation);

                var productLabel = Item.id + (string.IsNullOrEmpty(Item.defaultDescription.Title)
                                          ? string.Empty
                                          : " - " + Item.defaultDescription.Title);

                if (string.IsNullOrEmpty(productLabel) || Item.id.Trim().Length == 0)
                {
                    productLabel = "Product ID is Empty";
                } else {
                    idInvalid = false;
                }

                EditorGUILayout.LabelField(productLabel);
                EditorGUILayout.Space();

                BeginErrorBlock(validation, "id");
                var duplicateIDFieldWidth = 90;
                var idRect = EditorGUILayout.GetControlRect(true);
                idRect.width -= duplicateIDFieldWidth;
                Item.id = EditorGUI.TextField(idRect, "ID:", Item.id);
                var style = new GUIStyle();
                style.normal.textColor = Color.red;

                var duplicateIDLabelRect = new Rect(idRect.xMax + 5, idRect.yMin, duplicateIDFieldWidth,
                    EditorGUIUtility.singleLineHeight);
                EditorGUI.LabelField(duplicateIDLabelRect, idDuplicate ? "ID is a duplicate" : string.Empty, style);
                EditorGUI.LabelField(duplicateIDLabelRect,
                    !idDuplicate && idInvalid && shouldBeMarked ? "ID is empty" : string.Empty, style);
                EndErrorBlock(validation, "id");

                BeginErrorBlock(validation, "type");
                var typeRect = EditorGUILayout.GetControlRect(true);
                typeRect.width = idRect.width;
                Item.type = (ProductType) EditorGUI.EnumPopup(typeRect, "Type:", Item.type);
                EndErrorBlock(validation, "type");

                advancedVisible = CompatibleGUI.Foldout(advancedVisible, "Advanced", true, s);
                if (advancedVisible)
                {
                    EditorGUI.indentLevel++;

                    descriptionVisible = CompatibleGUI.Foldout(descriptionVisible, "Descriptions", true, s);
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
                        }

                        foreach (var desc in Item.translatedDescriptions)
                        {
                            if (DescriptionEditorGUI(desc, true, "translatedDescriptions." + desc.googleLocale))
                            {
                                descriptionsToRemove.Add(desc);
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
                        payoutsVisible = EditorGUILayout.Foldout(payoutsVisible, "Payouts", s);
                        if (payoutsVisible)
                        {
                            EditorGUI.indentLevel++;

                            int payoutIndex = 1;
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
                                }

                                EditorGUI.indentLevel++;
                                payout.type =
                                    (ProductCatalogPayout.ProductCatalogPayoutType) EditorGUILayout.EnumPopup("Type",
                                        payout.type);
                                payout.subtype = TruncateString(EditorGUILayout.TextField("Subtype", payout.subtype),
                                    ProductCatalogPayout.MaxSubtypeLength);
                                payout.quantity = EditorGUILayout.DoubleField("Quantity", payout.quantity);
                                payout.data = TruncateString(EditorGUILayout.TextField("Data", payout.data),
                                    ProductCatalogPayout.MaxDataLength);
                                EditorGUI.indentLevel--;

                                EditorGUILayout.EndVertical();

                                payoutIndex++;
                            }

                            payoutsToRemove.ForEach((p) => Item.RemovePayout(p));
                            payoutsToRemove.Clear();

                            if (GUILayout.Button("Add Payout"))
                            {
                                Item.AddPayout();
                            }

                            EditorGUI.indentLevel--;
                        }
                    }

                    EditorGUILayout.Separator();

                    storeIDsVisible = CompatibleGUI.Foldout(storeIDsVisible, "Store ID Overrides", true, s);
                    if (storeIDsVisible) {
                        EditorGUI.indentLevel++;
                        foreach (string storeKey in kStoreKeys) {
                            BeginErrorBlock(validation, "storeID." + storeKey);
                            Item.SetStoreID(storeKey, EditorGUILayout.TextField(storeKey, Item.GetStoreID(storeKey)));
                            EndErrorBlock(validation, "storeID." + storeKey);
                        }
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    googleVisible = CompatibleGUI.Foldout(googleVisible, "Google Configuration", true, s);
                    if (googleVisible) {
                        EditorGUI.indentLevel++;

                        EditorGUILayout.LabelField("Provide either a price or an ID for a pricing template created in Google Play");

                        BeginErrorBlock(validation, "googlePrice");
                        var priceStr = EditorGUILayout.TextField("Price:", Item.googlePrice == null || Item.googlePrice.value == 0 ? string.Empty : Item.googlePrice.value.ToString());
                        decimal priceDecimal;
                        if (decimal.TryParse(priceStr, out priceDecimal)) {
                            Item.googlePrice.value = priceDecimal;
                        } else {
                            Item.googlePrice.value = 0;
                        }
                        Item.pricingTemplateID = EditorGUILayout.TextField("Pricing Template:", Item.pricingTemplateID);
                        EndErrorBlock(validation, "googlePrice");

                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    appleVisible = CompatibleGUI.Foldout(appleVisible, "Apple Configuration", true, s);
                    if (appleVisible) {
                        EditorGUI.indentLevel++;
                        BeginErrorBlock(validation, "applePriceTier");
                        Item.applePriceTier = EditorGUILayout.Popup("Price Tier:", Item.applePriceTier, ApplePriceTiers.Strings);
                        EndErrorBlock(validation, "applePriceTier");

                        // Screenshot
                        BeginErrorBlock(validation, "screenshotPath");
                        EditorGUILayout.LabelField("Screenshot path:", Item.screenshotPath);
                        EndErrorBlock(validation, "screenshotPath");
                        var screenshotButtonBox = EditorGUILayout.BeginVertical();

                        var screenshotButtonRect = new Rect(screenshotButtonBox.xMax - ProductCatalogExportWindow.kWidth,
                                                            screenshotButtonBox.yMin,
                                                            ProductCatalogExportWindow.kWidth,
                                                            EditorGUIUtility.singleLineHeight);
                        if (GUI.Button(screenshotButtonRect, new GUIContent("Select a screenshot", "Required for Apple XML Delivery"))) {
                            string selectedPath = EditorUtility.OpenFilePanel("Select a screenshot", "", "");
                            if (selectedPath != null) {
                                Item.screenshotPath = selectedPath;
                            }
                        }

                        EditorGUILayout.EndVertical();
                        EditorGUI.indentLevel--;
                    }

                    EditorGUILayout.Separator();

                    #region UDP Catalog

                    if (s_udpAvailable && IsUdpInstalled())
                    {
                        EditorGUILayout.Separator();
                        udpVisible = CompatibleGUI.Foldout(udpVisible, "Unity Distribution Portal Configuration",
                            true, s);

                        if (udpVisible)
                        {
                            EditorGUI.indentLevel++;

                            if (!string.IsNullOrEmpty(udpSyncErrorMsg)){
                                var errStyle = new GUIStyle();
                                errStyle.normal.textColor = Color.red;
                                EditorGUILayout.LabelField(udpSyncErrorMsg, errStyle);
                            }

							var udpFieldsDisabled = kIsPreparing || udpItemSyncing || !kValidLogin || !kValidConfig;

							//If everything appears ok, check UDP compatibility and warn user if there's a problem
							//This should not stop the user from doing some UDP sync work, as there is no current blocker for those features.
							if (!udpFieldsDisabled && string.IsNullOrEmpty(kUdpErrorMsg) && !UdpSynchronizationApi.CheckUdpCompatibility())
							{
        	        			kUdpErrorMsg = "Please update your UDP package. Transaction features will no longer work at runtime with your current UDP version";
							}

                            if (!string.IsNullOrEmpty(kUdpErrorMsg)){
                                var errStyle = new GUIStyle();
                                errStyle.normal.textColor = Color.red;
                                EditorGUILayout.LabelField(kUdpErrorMsg, errStyle);
                            }

                            EditorGUI.BeginDisabledGroup(udpFieldsDisabled);

                            BeginErrorBlock(validation, "udpPrice");
                            EditorGUILayout.LabelField(
                                "Please provide a price in USD, other currencies can be edited on UDP portal.");

                            var priceStr = EditorGUILayout.TextField("Price:",
                                Item.udpPrice == null || Item.udpPrice.value == 0
                                    ? string.Empty
                                    : Item.udpPrice.value.ToString());

                            decimal priceDecimal;
                            if (decimal.TryParse(priceStr, out priceDecimal))
                            {
                                Item.udpPrice.value = priceDecimal;
                            }
                            else
                            {
                                Item.udpPrice.value = 0;
                            }

                            EndErrorBlock(validation, "udpPrice");

                            if (GUILayout.Button("Sync to UDP"))
                            {
                                udpSyncErrorMsg = "";
                                IapItem iapItem = new IapItem();
                                iapItem.consumable = Item.type == ProductType.Consumable;
                                iapItem.slug = Item.GetStoreID(UDP.Name) ?? Item.id;
                                iapItem.name = Item.defaultDescription.Title;
                                iapItem.properties.description = Item.defaultDescription.Description;
                                iapItem.priceSets.PurchaseFee.priceMap.DEFAULT.Add(new PriceDetail
                                {
                                    price = Item.udpPrice.value.ToString()
                                });

            					if (kAppStoreSettings != null)
					            {
                    	            var appSlug = AppStoreSettingsInterface.GetAppSlugField();
                                	iapItem.masterItemSlug = (string)appSlug.GetValue(kAppStoreSettings);
								}

                                iapItem.ownerId = kOrgId;

                                if (iapItem.ValidationCheck() == "")
                                {
                                    if (kIapItems.ContainsKey(iapItem.slug)) // Update
                                    {
                                        iapItem.id = kIapItems[iapItem.slug].id;
                                        requestQueue.Enqueue(new ReqStruct
                                        {
                                            resp = new IapItemResponse(),
                                            itemEditor = this,
                                            request = UdpSynchronizationApi.UpdateStoreItem(kTokenInfo.access_token,
                                                iapItem),
                                            iapItem = iapItem,
                                        });
                                    }
                                    else // Create
                                    {
                                        requestQueue.Enqueue(new ReqStruct
                                        {
                                            request = UdpSynchronizationApi.CreateStoreItem(kTokenInfo.access_token,
                                                kOrgId, iapItem),
                                            resp = new IapItemResponse(),
                                            itemEditor = this,
                                            iapItem = iapItem,
                                        });
                                    }

                                    udpItemSyncing = true;
                                }
                                else
                                {
                                    EditorUtility.DisplayDialog("Sync Error", iapItem.ValidationCheck(), "OK");
                                }
                            }

                            EditorGUI.EndDisabledGroup();

                            EditorGUI.indentLevel--;
                        }
                    }
                    #endregion

                    EditorGUI.indentLevel--;
                }

                EditorGUILayout.EndVertical();
            }



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
                    udpVisible = true;
                }
            }

            public void SetIDDuplicateError(bool isDuplicate)
            {
                idDuplicate = isDuplicate;
            }

            public void SetIDInvalidError(bool isValid)
            {
                idInvalid = isValid;
            }

            public void SetShouldBeMarked(bool marked)
            {
                shouldBeMarked = marked;
            }

            private bool DescriptionEditorGUI(LocalizedProductDescription description, bool showRemoveButton, string fieldValidationPrefix)
            {
                var box = EditorGUILayout.BeginVertical();
                var removeButtonWidth = EditorGUIUtility.singleLineHeight + 2;

                var rect = EditorGUILayout.GetControlRect(true);
                if (showRemoveButton) {
                    rect.width -= removeButtonWidth;
                }

                BeginErrorBlock(validation, fieldValidationPrefix + ".googleLocale");
                description.googleLocale = (TranslationLocale)EditorGUI.Popup(rect, "Locale:", (int)description.googleLocale,
                    LocaleExtensions.GetLabelsWithSupportedPlatforms());
                EndErrorBlock(validation, fieldValidationPrefix + ".googleLocale");

                BeginErrorBlock(validation, fieldValidationPrefix + ".Title");
                description.Title = EditorGUILayout.TextField("Title:", description.Title);
                EndErrorBlock(validation, fieldValidationPrefix + ".Title");

                BeginErrorBlock(validation, fieldValidationPrefix + ".Description");
                description.Description = EditorGUILayout.TextField("Description:", description.Description);
                EndErrorBlock(validation, fieldValidationPrefix + ".Description");

                var removeButtonRect = new Rect(box.xMax - removeButtonWidth, box.yMin, removeButtonWidth, EditorGUIUtility.singleLineHeight);
                var remove = (showRemoveButton
                              && GUI.Button(removeButtonRect, "x")
                              && EditorUtility.DisplayDialog("Delete Translation?",
                                                             "Are you sure you want to delete this translation?",
                                                             "Delete",
                                                             "Do Not Delete"));
                EditorGUILayout.EndVertical();
                return remove;
            }

            private static string TruncateString (string s, int len)
            {
                if (string.IsNullOrEmpty (s)) return s;
                if (len < 0) return string.Empty;
                return s.Substring (0, Math.Min (s.Length, len));
            }
        }

        /// <summary>
        /// A popup window that shows a list of exporters and kicks off an export from the ProductCatalogEditor.
        /// </summary>
        public class ProductCatalogExportWindow : PopupWindowContent
        {
            public const float kWidth = 200f;

            private ProductCatalogEditor editor;
            private List<IProductCatalogExporter> exporters = new List<IProductCatalogExporter>();

            public ProductCatalogExportWindow(ProductCatalogEditor editor_)
            {
                editor = editor_;

                exporters.Add(new AppleXMLProductCatalogExporter());
                exporters.Add(new GooglePlayProductCatalogExporter());
                exporters.Add(new CloudJSONProductCatalogExporter());
            }

            public override Vector2 GetWindowSize()
            {
                return new Vector2(kWidth, EditorGUIUtility.singleLineHeight * (exporters.Count + 1));
            }

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
                    }
                }

                EditorGUILayout.EndVertical();
            }

            private bool Validate(IProductCatalogExporter exporter, out ExporterValidationResults catalogValidation,
                out List<ExporterValidationResults> itemValidation, bool debug = false)
            {
                bool valid = true;
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
                    Action<string, ExporterValidationResults> DebugResults =
                        (string name, ExporterValidationResults r) =>
                        {
                            if (!r.Valid || r.warnings.Count != 0) Debug.LogWarning(name + ", Valid = " + r.Valid);
                            foreach (var m in r.errors)
                            {
                                Debug.LogWarning("errors " + m);
                            }

                            foreach (var m in r.fieldErrors)
                            {
                                Debug.LogWarning("fieldErrors " + m);
                            }

                            foreach (var m in r.warnings)
                            {
                                Debug.LogWarning("warnings " + m);
                            }
                        };

                    if (!valid) Debug.LogWarning("Product Catalog Export Overall Result: valid " + valid);
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
                ExporterValidationResults catalogValidation;
                List<ExporterValidationResults> itemValidation;

                var valid = Validate(exporter, out catalogValidation, out itemValidation, kValidateDebugLog);
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
                        string targetPath = Path.Combine(Path.GetDirectoryName(path), Path.GetFileName(fileToCopy));
                        FileInfo fileInfo = new FileInfo(fileToCopy);
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

                IProductCatalogExporter exporter = exporters.Single(e => e.StoreName == storeName);
                if (exporter == null)
                {
                    Debug.LogErrorFormat("Unable to export {0} Product Catalog. Export is unsupported for this store.",
                        storeName);
                    return false;
                }

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

                ExporterValidationResults catalogValidation;
                List<ExporterValidationResults> itemValidation;
                var valid = Validate(exporter, out catalogValidation, out itemValidation, kValidateDebugLog);

                if (!valid)
                {
                    Debug.LogWarningFormat(
                        "{0} Product Catalog is invalid. Automatically fixing for export. Manually fix Catalog errors by opening IAP Catalog editor window with {1} menu, performing App Store Export for this store, and resolving reported issues.",
                        storeName, ProductCatalogEditorMenuPath);
                    catalog = exporter.NormalizeToType(catalog);
                }

                bool wrote = false;

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
                    Debug.LogErrorFormat("Unable to export {0} Product Catalog. Path {1} is invalid.", storeName, path);
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
            public bool Valid
            {
                get { return (errors.Count == 0 && fieldErrors.Count == 0); }
            }

            public List<string> errors = new List<string>();
            public List<string> warnings = new List<string>();

            public Dictionary<string, string> fieldErrors = new Dictionary<string, string>();
        }

        /// <summary>
        /// Product catalog exporters implement this interface to provide validation and export of a ProductCatalog.
        /// </summary>
        public interface IProductCatalogExporter
        {
            string DisplayName { get; }
            string DefaultFileName { get; }
            string FileExtension { get; }
            string StoreName { get; }

            /// <summary>
            /// Required specific path for output file. Is optional whether user will be permitted to save a copy
            /// to a separate path in addition to this required path.
            /// </summary>
            string MandatoryExportFolder { get; }

            string Export(ProductCatalog catalog);
            ExporterValidationResults Validate(ProductCatalog catalog);
            ExporterValidationResults Validate(ProductCatalogItem item);
            ProductCatalog NormalizeToType(ProductCatalog catalog); // Fixes issues targeting this exporter's impl

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
            public static bool parsedVersion = false;
            public static bool isRunningUnity55 = false;

            public static bool Foldout(bool foldout, string text, bool toggleOnLabelClick, GUIStyle style)
            {
                if (!parsedVersion)
                {
                    parsedVersion = true;
#if UNITY_UNIFIED_IAP
                    //The unified package only works on Unity 2018.4 and up. We can assume it's always higher than 5.5.
					isRunningUnity55 = true;
#else
                    bool parseError = false;
                    string[] v = Application.unityVersion.Split('.');
                    int major = 0;
                    int minor = 0;

                    if (v.Length > 0) parseError |= !int.TryParse(v[0], out major);
                    if (v.Length > 1) parseError |= !int.TryParse(v[1], out minor);

                    if (!parseError)
                    {
                        isRunningUnity55 = major > 5 || (major == 5 && minor >= 5);
                    }
#endif
                }

                if (isRunningUnity55)
                {
                    // Helper is required to be a separate scope to avoid Unity linker from failing to bind it
                    // and propagating the fatal error upwards.
                    return FoldoutHelper(foldout, text, toggleOnLabelClick, style);
                }
                else
                {
                    return EditorGUILayout.Foldout(foldout, text, style);
                }
            }

            public static bool FoldoutHelper(bool foldout, string text, bool toggleOnLabelClick,
                GUIStyle style)
            {
                return EditorGUILayout.Foldout(foldout, text, toggleOnLabelClick, style);
            }
        }
    }
}