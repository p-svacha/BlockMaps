using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Static class that manages collections of all Defs of a given type.
    /// <br/>Provides methods to look up, retrieve, and list Defs by type or name.
    /// </summary>
    public static class DefDatabase<T> where T : Def
    {
		// Called once when accessing a DefDatabase of a specific type T for the first time
		static DefDatabase()
		{
			// Register this DefDatabase<T> type in the registry
			DefDatabaseRegistry.RegisterDefDatabase(typeof(DefDatabase<T>));
		}

		private static List<T> defsList = new List<T>();
		private static Dictionary<string, T> defsByName = new Dictionary<string, T>();

		/// <summary>
		/// Provides a list of all Defs of this particular type.
		/// </summary>
		public static List<T> AllDefs => defsList;

		/// <summary>
		/// Adds a collection of defs to this database. Should only be called at the very start of an application.
		/// </summary>
		public static void AddDefs(List<T> defCollection)
        {
			foreach(T def in defCollection)
            {
				if (!def.Validate()) throw new System.Exception("Loading Defs aborted due to an invalid Def");

				defsList.Add(def);
				defsByName.Add(def.DefName, def);
            }
        }

		/// <summary>
		/// Resolves all references of all defs within the database so references within defs to other defs can be accessed correctly.
		/// <br/>Gets called through DefDatabaseRegistry.
		/// </summary>
		public static void ResolveReferences()
        {
			foreach (T def in defsList)
			{
				try
				{
					def.ResolveReferences();
				}
				catch (System.Exception e)
				{
					throw new System.Exception("Failed to resolve references for Def '" + def.DefName + "' of type " + def.GetType() + ": " + e.Message);

				}
			}
        }

		/// <summary>
		/// Gets called through DefDatabaseRegistry after all loading steps are done.
		/// </summary>
		public static void OnLoadingDone()
        {
			Debug.Log("DefDatabase<" + typeof(T) + "> has loaded " + defsList.Count + " Defs.");
        }

		/// <summary>
		/// Returns the Def of the given name. Throws an exception if it does not exist.
		/// </summary>
		public static T GetNamed(string defName)
		{
			if (defsByName.TryGetValue(defName, out var value))
			{
				return value;
			}
			throw new System.Exception(string.Concat("Failed to find ", typeof(T), " named ", defName, "."));
		}
	}
}
