using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using Unity.Services.DeploymentApi.Editor;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    abstract class DeploymentItemBase : IDeploymentItem, ITypedItem
    {
        float m_Progress;
        DeploymentStatus m_Status;
        string m_Path;
        string m_Name;

        protected DeploymentItemBase(string path)
        {
            Name = System.IO.Path.GetFileName(path);
            Path = path;
        }

        public abstract string Type { get; }

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

        public DeploymentStatus Status
        {
            get => m_Status;
            set => SetField(ref m_Status, value);
        }

        public ObservableCollection<AssetState> States { get; } = new();

        internal void ClearTypedStates(string ownedType)
        {
            var index = 0;
            while (index < States.Count)
            {
                if (States[index].Type == ownedType)
                {
                    States.RemoveAt(index);
                }
                else
                {
                    index++;
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected void SetField<T>(
            ref T field,
            T value,
            Action<T> onFieldChanged = null,
            [CallerMemberName] string propertyName = null)
        {
            if (EqualityComparer<T>.Default.Equals(field, value))
            {
                return;
            }
            field = value;
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
            onFieldChanged?.Invoke(field);
        }
    }
}
