using System;

namespace UnityEngine.Purchasing.WebshopService
{
    internal class CloudProjectAuthenticationException : Exception
    {
        internal CloudProjectAuthenticationException()
            : base("Failed to authenticate the cloud project because the environment id and/or project id is null. Make sure the user is authenticated.")
        { }
    }
}
