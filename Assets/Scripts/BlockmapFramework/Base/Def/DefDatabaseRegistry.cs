using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// The central registry that contains references to all DefDatabases (1 for each Def-type).
    /// <br/>Functions that should be run on all DefDatases should only be executed through this registry.
    /// </summary>
    public static class DefDatabaseRegistry
    {
        // Stores all DefDatabase types registered
        private static readonly List<Type> registeredDefDatabases = new List<Type>();

        // Called when a DefDatabase<T> type is accessed for the first time
        public static void RegisterDefDatabase(Type defDatabaseType)
        {
            if (!registeredDefDatabases.Contains(defDatabaseType))
            {
                registeredDefDatabases.Add(defDatabaseType);
            }
        }

        // Calls ResolveReferences on each registered DefDatabase type
        public static void ResolveAllReferences()
        {
            foreach (Type defDatabaseType in registeredDefDatabases)
            {
                // Invoke the static ResolveReferences method
                MethodInfo resolveMethod = defDatabaseType.GetMethod("ResolveReferences", BindingFlags.Static | BindingFlags.Public);
                resolveMethod?.Invoke(null, null);
            }
        }

        // Calls OnLoadingDone on each registered DefDatabase type
        public static void OnLoadingDone()
        {
            foreach (Type defDatabaseType in registeredDefDatabases)
            {
                // Invoke the static OnLoadingDone method
                MethodInfo resolveMethod = defDatabaseType.GetMethod("OnLoadingDone", BindingFlags.Static | BindingFlags.Public);
                resolveMethod?.Invoke(null, null);
            }
        }
    }
}
