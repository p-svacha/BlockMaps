using BlockmapFramework.Defs;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
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

        /// <summary>
        /// Adds all Defs that are defined in the BlockmapFramework and are useful for all projects to their respective DefDatabases.
        /// </summary>
        public static void AddAllGlobalDefs()
        {
            DefDatabase<NodeDef>.AddDefs(GlobalNodeDefs.Defs);
            DefDatabase<SurfaceDef>.AddDefs(GlobalSurfaceDefs.Defs);
            DefDatabase<SurfacePropertyDef>.AddDefs(GlobalSurfacePropertyDefs.Defs);
            DefDatabase<WallShapeDef>.AddDefs(GlobalWallShapeDefs.Defs);
            DefDatabase<WallMaterialDef>.AddDefs(GlobalWallMaterialDefs.Defs);
        }

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

        /// <summary>
        /// Sets all values within DefOf classes to the Defs matching the name.
        /// </summary>
        public static void BindAllDefOfs()
        {
            foreach (Type defOfClass in GetAllDefOfClasses())
            {
                BindDefsFor(defOfClass);
            }
        }

        private static List<Type> GetAllDefOfClasses()
        {
            var defOfClasses = new List<Type>();
            var assemblies = AppDomain.CurrentDomain.GetAssemblies();

            foreach (var assembly in assemblies)
            {
                foreach (var type in assembly.GetTypes())
                {
                    // Check if the type has the [DefOf] attribute
                    if (type.GetCustomAttributes(typeof(DefOf), inherit: false).Any())
                    {
                        defOfClasses.Add(type);
                    }
                }
            }

            return defOfClasses;
        }

        /// <summary>
        /// Takes a type of a DefOf static class as an input and sets all Def-fields within that class.
        /// </summary>
        private static void BindDefsFor(Type type)
        {
            FieldInfo[] fields = type.GetFields(BindingFlags.Static | BindingFlags.Public);
            foreach (FieldInfo fieldInfo in fields)
            {
                Type fieldType = fieldInfo.FieldType;

                // Ensure the field's type is a Def type
                if (!typeof(Def).IsAssignableFrom(fieldType)) throw new Exception($"{fieldType} is not a Def. Aborting Binding DefOf {type}.");

                // Use the field name to look up the Def by name in the appropriate DefDatabase
                string defName = fieldInfo.Name;

                // Construct the DefDatabase type for the specific Def type (fieldType)
                Type defDatabaseType = typeof(DefDatabase<>).MakeGenericType(fieldType);

                // Get the GetNamed method of this DefDatabase type
                MethodInfo getNamedMethod = defDatabaseType.GetMethod("GetNamed", BindingFlags.Static | BindingFlags.Public);

                // Check if the method exists (it should in a correctly implemented DefDatabase)
                if (getNamedMethod == null) throw new Exception($"DefDatabase<{fieldType.Name}> does not have a GetNamed method.");

                try
                {
                    // Invoke GetNamed with defName to get the specific Def instance
                    object defInstance = getNamedMethod.Invoke(null, new object[] { defName });

                    // Assign the Def instance to the static field in the DefOf class
                    fieldInfo.SetValue(null, defInstance);
                }
                catch (TargetInvocationException e)
                {
                    throw new Exception($"Failed to bind Def named '{defName}' in {type}: " + e.InnerException.Message);
                }
            }
        }
    }
}
