using System;
using System.Collections;
using System.Collections.ObjectModel;
using System.Collections.Generic;
using System.Linq;

using Uniject;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Purchasing;
using UnityEngine.Purchasing.Extension;
using UnityEngine.UI;

namespace UnityEngine.Purchasing
{
	/// <summary>
	/// User interface fake store.
	/// </summary>
	internal class UIFakeStore : FakeStore
	{
		/// <summary>
		/// Dialog request dumb container
		/// </summary>
		protected class DialogRequest
		{
			public string QueryText;
			public string OkayButtonText;
			public string CancelButtonText;
			public List<string> Options;
			public Action<bool,int> Callback;
		}

		/// <summary>
		/// Lifecycle notifier waits to be destroyed before calling a callback.
		/// Use to notify script of hierarchy destruction for avoiding dynamic 
		/// UI hierarchy collisions.
		/// </summary>
		protected class LifecycleNotifier : MonoBehaviour
		{
			public Action OnDestroyCallback;

			void OnDestroy()
			{
				if (OnDestroyCallback != null)
				{
					OnDestroyCallback();
				}
			}
		}

		const string EnvironmentDescriptionPostfix = "\n\n[Environment: FakeStore]";
		const string SuccessString = "Success";
		const int RetrieveProductsDescriptionCount = 2;

		DialogRequest m_CurrentDialog;
		int m_LastSelectedDropdownIndex;

		GameObject UIFakeStoreCanvasPrefab; // The loaded prefab
		Canvas m_Canvas; // Cloned canvas instance not the prefab itself. Auto-null'd by UI system.
		GameObject m_EventSystem; // Dynamically created. Auto-null'd by UI system.
		string m_ParentGameObjectPath;

		#pragma warning disable 0414
		IUtil m_Util;
		#pragma warning restore 0414

		public UIFakeStore() {
		}

		public UIFakeStore (IUtil util)
		{
			m_Util = util;
		}
		
		/// <summary>
		/// Creates and displays a modal dialog UI. Note pointer events can "drill through" the 
		/// UI activating underlying interface elements. Consider using techniques mentioned in
		/// http://forum.unity3d.com/threads/frequently-asked-ui-questions.264479/ in apps
		/// to mitigate this. Shows only one at a time.
		/// </summary>
		/// <returns><c>true</c>, if UI was started, <c>false</c> otherwise.</returns>
		/// <param name="model">Store model being shown; uses dialogType to decode.</param>
		/// <param name="dialogType">Dialog type.</param>
		/// <param name="callback">Callback called when dialog dismissed.</param>
		/// <typeparam name="T">The 1st type parameter.</typeparam>
		protected override bool StartUI<T>(object model, DialogType dialogType, Action<bool,T> callback)
		{
			List<string> options = new List<string>();
			// Add a default option for "Success" 
			options.Add(SuccessString);

			foreach (T code in Enum.GetValues(typeof(T))) 
			{
				options.Add(code.ToString());
			}

			Action<bool,int> callbackWrapper = new Action<bool,int> ((bool result, int codeValue) => {
				// TRICKY: Would prefer to use .NET 4+'s dynamic keyword over double-casting to what I know is an enum type.
				T value = (T)(object)codeValue;
				callback (result, value);
			});

			string title = null, okayButton = null, cancelButton = null;
			if (dialogType == DialogType.Purchase)
			{
				title = CreatePurchaseQuestion ((ProductDefinition)model);
				if (UIMode == FakeStoreUIMode.DeveloperUser) 
				{
					// Developer UIMode is one button, one option menu, so the button must support both pass and fail
					okayButton = "OK";
				} 
				else 
				{
					okayButton = "Buy";
				}
			} 
			else if (dialogType == DialogType.RetrieveProducts)
			{
				title = CreateRetrieveProductsQuestion ((ReadOnlyCollection<ProductDefinition>)model);
				okayButton = "OK";
			} 
			else
			{
				Debug.LogError ("Unrecognized DialogType " + dialogType);
			}
			cancelButton = "Cancel";

			return StartUI (title, okayButton, cancelButton, options, callbackWrapper);
		}

		/// <summary>
		/// Helper 
		/// </summary>
		bool StartUI(string queryText, string okayButtonText, string cancelButtonText, 
			List<string> options, Action<bool,int> callback)
		{
			// One dialog at a time please
			if (IsShowingDialog())
			{
				return false;
			}

			// Wrap this dialog request for later use
			DialogRequest dr = new DialogRequest ();
			dr.QueryText = queryText;
			dr.OkayButtonText = okayButtonText; 
			dr.CancelButtonText = cancelButtonText;
			dr.Options = options;
			dr.Callback = callback;

			m_CurrentDialog = dr;

			InstantiateDialog();

			return true;
		}

		/// <summary>
		/// Creates the UI from a prefab. Configures the UI. Shows the dialog. 
		/// </summary>
		private void InstantiateDialog()
		{
			if (m_CurrentDialog == null)
			{
				Debug.LogError(this + " requires m_CurrentDialog. Not showing dialog.");
				return;
			}

			// Load this once
			if (UIFakeStoreCanvasPrefab == null)
			{
				UIFakeStoreCanvasPrefab = Resources.Load("UIFakeStoreCanvas") as GameObject;
			}

			Canvas dialogCanvas = UIFakeStoreCanvasPrefab.GetComponent<Canvas>();

			// To show, and to configure UI, first realize it on screen
			m_Canvas = Object.Instantiate(dialogCanvas);

			// TRICKY: I support one dialog at a time but there's a delay between a request
			// to the UI system to destroy a UI element and the UI system completing the destruction.
			// To avoid conflicts with partially destroyed dialogs hanging around too long we add a 
			// custom behavior to the scene explicitly to notify me when the UI has been destroyed.

			LifecycleNotifier notifier = m_Canvas.gameObject.AddComponent<LifecycleNotifier>() as LifecycleNotifier;
			notifier.OnDestroyCallback = () =>
			{
				// Indicates we've completely closed our dialog
				m_CurrentDialog = null;
			};

			m_ParentGameObjectPath = m_Canvas.name + "/Panel/";

			// Ensure existence of EventSystem for use by UI
			if (Object.FindObjectOfType<EventSystem>() == null)
			{
				// No EventSystem found, create a new one and add to the Canvas
				m_EventSystem = new GameObject("EventSystem", typeof(EventSystem));
				m_EventSystem.AddComponent<StandaloneInputModule>();
				m_EventSystem.transform.parent = m_Canvas.transform;
			}

			// Configure the dialog
			var qt = GameObject.Find(m_ParentGameObjectPath + "HeaderText");
			Text queryTextComponent = qt.GetComponent<Text>();
			queryTextComponent.text = m_CurrentDialog.QueryText;

			Text allowText = GetOkayButtonText();
			allowText.text = m_CurrentDialog.OkayButtonText;

			Text denyText = GetCancelButtonText();
			denyText.text = m_CurrentDialog.CancelButtonText;
		
			// Populate the dropdown
			GetDropdown().options.Clear(); // Assume it has defaults prepopulated
			foreach (var item in m_CurrentDialog.Options)
			{
				GetDropdown().options.Add(new Dropdown.OptionData(item));
			}

			if (m_CurrentDialog.Options.Count > 0)
			{
				m_LastSelectedDropdownIndex = 0;
			}

			// Ensure the dropdown renders its default value
			GetDropdown().RefreshShownValue();

			// Wire up callbacks
			GetOkayButton().onClick.AddListener(() => { 
				this.OkayButtonClicked(); 
			});
			GetCancelButton().onClick.AddListener(() => { 
				this.CancelButtonClicked(); 
			});
			GetDropdown().onValueChanged.AddListener((int selectedItem) => {
				this.DropdownValueChanged(selectedItem);
			});

			// Honor FakeStoreUIMode 
			if (UIMode == FakeStoreUIMode.StandardUser)
			{
				GetDropdown ().onValueChanged.RemoveAllListeners ();
				GameObject.Destroy (GetDropdownContainerGameObject ());
			} 
			else if (UIMode == FakeStoreUIMode.DeveloperUser)
			{
				GetCancelButton().onClick.RemoveAllListeners();
				GameObject.Destroy (GetCancelButtonGameObject ());
			}
		}

		private string CreatePurchaseQuestion(ProductDefinition definition) 
		{
			return "Do you want to Purchase " + definition.id + "?" + EnvironmentDescriptionPostfix;
		}

		private string CreateRetrieveProductsQuestion(ReadOnlyCollection<ProductDefinition> definitions)
		{
			string title = "Do you want to initialize purchasing for products {";
			title += string.Join(", ", definitions.Take(RetrieveProductsDescriptionCount).Select(pid => pid.id).ToArray());
			if (definitions.Count > RetrieveProductsDescriptionCount)
			{
				title += ", ...";
			}
			title += "}?" + EnvironmentDescriptionPostfix;

			return title;
		}

		private Button GetOkayButton()
		{
			return GameObject.Find(m_ParentGameObjectPath + "Button1").GetComponent<Button>();
		}

		private Button GetCancelButton()
		{
			GameObject gameObject = GameObject.Find (m_ParentGameObjectPath + "Button2");

			if (gameObject != null)
			{
				return gameObject.GetComponent<Button>();
			} 
			else
			{
				return null;
			}
		}

		private GameObject GetCancelButtonGameObject()
		{
			return GameObject.Find (m_ParentGameObjectPath + "Button2");
		}

		private Text GetOkayButtonText()
		{
			return GameObject.Find (m_ParentGameObjectPath + "Button1/Text").GetComponent<Text> ();
		}

		private Text GetCancelButtonText()
		{
			return GameObject.Find (m_ParentGameObjectPath + "Button2/Text").GetComponent<Text> ();
		}

		private Dropdown GetDropdown()
		{
			var gameObject = GameObject.Find (m_ParentGameObjectPath + "Panel2/Panel3/Dropdown");
			if (gameObject != null)
			{
				return gameObject.GetComponent<Dropdown> ();
			}
			else
			{
				return null;
			}
		}

		private GameObject GetDropdownContainerGameObject()
		{
			return GameObject.Find(m_ParentGameObjectPath + "Panel2");
		}

		/// <summary>
		/// Positive button clicked. For yes/no dialog will send true message. For 
		/// multiselect (FakeStoreUIMode.DeveloperUser) dialog may send true or false
		/// message, along with chosen option.
		/// </summary>
		private void OkayButtonClicked()
		{
			bool result = false; 

			// Return false if the user chose something other than Success, and is in Development mode.
			// True if the "Success" option was chosen, or if this is non-Development mode.
			if (m_LastSelectedDropdownIndex == 0 || UIMode != FakeStoreUIMode.DeveloperUser)
			{
				// Ensure we return true
				result = true;
			}

			int codeValue = Math.Max(0, m_LastSelectedDropdownIndex - 1); // Pop SuccessString

			m_CurrentDialog.Callback(result, codeValue);
			CloseDialog();
		}

		/// <summary>
		/// Negative button clicked. Sends false message.
		/// </summary>
		private void CancelButtonClicked()
		{
			int codeValue = Math.Max(0, m_LastSelectedDropdownIndex - 1); // Pop SuccessString

			// ASSUME: This is FakeStoreUIMode.StandardUser
			m_CurrentDialog.Callback(false, codeValue);
			CloseDialog();
		}

		private void DropdownValueChanged(int selectedItem)
		{
			m_LastSelectedDropdownIndex = selectedItem;
		}

		private void CloseDialog()
		{
			m_CurrentDialog = null;

			GetOkayButton().onClick.RemoveAllListeners();
			if (GetCancelButton ()) 
			{
				GetCancelButton().onClick.RemoveAllListeners();
			}

			if (GetDropdown () != null) 
			{
				GetDropdown ().onValueChanged.RemoveAllListeners ();
			}

			GameObject.Destroy(m_Canvas.gameObject);
		}

		public bool IsShowingDialog()
		{
			return m_CurrentDialog != null;
		}
	}
}
