using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    [DataContract]
    class CatalogEntryDeploymentItem : IDeploymentItem, ITypedItem
    {
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;
        string m_Name;

        public CatalogEntryDeploymentItem(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
        }

        /// <summary>
        /// Name of the item as shown for user feedback, normally file_name.ext
        /// </summary>
        public virtual string Type => "Catalog Item";

        public virtual string Name
        {
            get => m_Name;
            set => SetField(ref m_Name, value);
        }

        public string Path
        {
            get => m_Path;
            set => SetField(ref m_Path, value);
        }

        public float Progress
        {
            get => m_Progress;
            set => SetField(ref m_Progress, value);
        }

        public CatalogItem CatalogItem { get; set; }

        public DeploymentStatus Status
        {
            get => m_Status;
            set => SetField(ref m_Status, value);
        }

        public ObservableCollection<AssetState> States { get; } = new();

        public bool Validate(CatalogItem previousItem)
        {
            ClearTypedStates(CatalogItem.ValidationStateType);

            var states = CatalogItem.Validate();
            foreach (var state in states)
                States.Add(state);

            if (previousItem != null && CatalogItem.ProductType != previousItem.ProductType)
            {
                States.Add(new AssetState(
                    "Product Type change",
                    "The product type has changed. This can lead to unintended consequences in the future of the catalog.",
                    SeverityLevel.Warning,
                    CatalogItem.ValidationStateType));
            }

            return !states.Any(s => s.Level == SeverityLevel.Error);
        }

        internal void ClearTypedStates(string ownedType)
        {
            var states = States;
            var i = 0;
            while (i < states.Count)
            {
                if (states[i].Type == ownedType)
                    states.RemoveAt(i);
                else
                    i++;
            }
        }

        public override string ToString()
        {
            if (Path == "Remote")
                return CatalogItem.uSku;
            return $"'{Path}'";
        }

        /// <summary>
        /// Event will be raised when a property of the instance is changed
        /// </summary>
        public event PropertyChangedEventHandler PropertyChanged;

        /// <summary>
        /// Sets the field and raises an OnPropertyChanged event.
        /// </summary>
        /// <param name="field">The field to set.</param>
        /// <param name="value">The value to set.</param>
        /// <param name="onFieldChanged">The callback.</param>
        /// <param name="propertyName">Name of the property to set.</param>
        /// <typeparam name="T">Type of the parameter.</typeparam>
        protected void SetField<T>(
            ref T field,
            T value,
            Action<T> onFieldChanged = null,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
                return;
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            onFieldChanged?.Invoke(field);
        }
    }
}
