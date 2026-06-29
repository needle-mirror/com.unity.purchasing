using System;

namespace UnityEditor.Purchasing.Editor.Authoring.Core.Model
{
    class ClientException : Exception
    {
        public ClientException(string message, Exception innerExcception) : base(message, innerExcception)
        {

        }
    }
}
