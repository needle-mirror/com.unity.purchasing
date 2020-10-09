#define UNITY_UNIFIED_IAP

using System;
using System.Reflection;

namespace Stores
{
#if !UNITY_UNIFIED_IAP
    public static class ReflectionUtils
    {
        internal static bool HasMethod(this object objectToCheck, string methodName)
        {
            return GetMethod(objectToCheck, methodName) != null;
        }

        static MethodInfo GetMethod(this object objectToCheck, string methodName)
        {
            var type = objectToCheck.GetType();
            return type.GetMethod(methodName);
        }

        internal static void InvokeMethod(this object objectToCheck, string methodName, object[] parameters)
        {
            GetMethod(objectToCheck, methodName)?.Invoke(objectToCheck, parameters);
        }
    }
#endif
}
