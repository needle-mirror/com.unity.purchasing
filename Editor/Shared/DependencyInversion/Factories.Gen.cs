// WARNING: Auto generated code. Modifications will be lost!
// Original source 'com.unity.services.shared' @0.0.12.

using System;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace Unity.Purchasing.Editor.Shared.DependencyInversion
{
    static class Factories
    {
        const string k_InitMethod = "Initialize";

        public static T Default<T>(IServiceProvider sp)
        {
            return Default<T, T>(sp);
        }

        public static TInterface Default<TInterface, TImplementation>(IServiceProvider sp) where TImplementation : TInterface
        {
            var ctr = typeof(TImplementation).GetConstructors().Where(p => p.IsPublic).ToList();
            if (ctr.Count == 0)
            {
                ctr = typeof(TImplementation)
                    .GetConstructors(BindingFlags.Instance | BindingFlags.NonPublic)
                    .Where(p => p.IsAssembly)
                    .ToList();
                if (ctr.Count != 1)
                {
                    throw new ConstructorNotFoundException(typeof(TImplementation));
                }
            }

            var parameters = ctr[0].GetParameters();
            var types = parameters.Select(t => sp.GetService(t.ParameterType));
            return (TInterface)ctr[0].Invoke(types.ToArray());
        }

        public static void InitializeInstance(IServiceProvider sp, object instance)
        {
            var init = instance.GetType()
                .GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .SingleOrDefault(p => p.IsPublic && p.Name == k_InitMethod);
            if (init == null)
            {
                throw new MethodNotFoundException(instance.GetType(), k_InitMethod);
            }

            var parameters = init.GetParameters();
            var types = parameters.Select(t =>
            {
                try
                {
                    return sp.GetService(t.ParameterType);
                }
                catch (Exception)
                {
                    Debug.Log(t.ParameterType);
                    throw;
                }
            });
            init.Invoke(instance, types.ToArray());
        }
    }
}
