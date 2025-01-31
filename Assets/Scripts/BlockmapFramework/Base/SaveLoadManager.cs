using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Xml;
using UnityEngine;

namespace BlockmapFramework
{
    /// <summary>
    /// Class responsible for serializing and deserializing objects based on their type.
    /// </summary>
    public static class SaveLoadManager
    {
        public static string SaveDataPath = Application.streamingAssetsPath + "/SaveData/";

        public static bool IsSaving;
        public static bool IsLoading;
        private static XmlWriter writer;
        private static XmlReader reader;

        /// <summary>
        /// The world object that is currently being loaded.
        /// </summary>
        public static World LoadingWorld;

        /// <summary>
        /// Saves the root object to the specified file.
        /// </summary>
        /// <typeparam name="T">The type of the root object, which must implement IExposable.</typeparam>
        /// <param name="rootObject">The root object to save.</param>
        /// <param name="fileName">The name of the file where the XML data will be saved.</param>
        public static void Save<T>(T rootObject, string fileName) where T : class, ISaveAndLoadable
        {
            IsSaving = true;

            using (FileStream fileStream = new FileStream(SaveDataPath + fileName + ".xml", FileMode.Create, FileAccess.Write))
            {
                writer = XmlWriter.Create(fileStream, new XmlWriterSettings { Indent = true });
                writer.WriteStartDocument();
                writer.WriteStartElement("Root");

                SaveOrLoadObject(ref rootObject, "Data");

                EndSave();
            }
        }

        /// <summary>
        /// Finalizes the save process by closing the root element and the writer.
        /// </summary>
        public static void EndSave()
        {
            writer.WriteEndElement(); // Close "Root"
            writer.WriteEndDocument();
            writer.Close();

            IsSaving = false;
        }

        /// <summary>
        /// Loads a world from a specified XML file.
        /// </summary>
        /// <param name="fileName">The name of the XML file to load from.</param>
        /// <returns>The deserialized object of type T.</returns>
        public static World Load(string fileName)
        {
            IsLoading = true;

            string fullPath = SaveDataPath + fileName + ".xml";
            Debug.Log($"Loading world {fullPath}.");
            using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                reader = XmlReader.Create(fileStream);
                reader.ReadToFollowing("Root");

                LoadingWorld = null;
                SaveOrLoadObject(ref LoadingWorld, "Data");

                EndLoad();
                return LoadingWorld;
            }
        }

        /// <summary>
        /// Finalizes the loading process by closing the reader.
        /// </summary>
        public static void EndLoad()
        {
            reader.Close();
            IsLoading = false;
        }

        /// <summary>
        /// Deep save or deep load an object, by inspecting fields and properties of the object and serializing them if in save mode, or deserializing them if in load mode.
        /// </summary>
        public static void SaveOrLoadObject<T>(ref T obj, string label) where T : class, ISaveAndLoadable
        {
            if (IsSaving)
            {
                writer.WriteStartElement(label);

                if (obj != null)
                {
                    writer.WriteAttributeString("Type", obj.GetType().FullName);
                    obj.ExposeDataForSaveAndLoad();
                }

                writer.WriteEndElement();
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string typeName = reader.GetAttribute("Type"); // Load Type of this object so we can instantiate the correct class.
                    if (!string.IsNullOrEmpty(typeName))
                    {
                        Type type = Type.GetType(typeName);
                        if (type != null && typeof(ISaveAndLoadable).IsAssignableFrom(type))
                        {
                            obj = (T)Activator.CreateInstance(type); // Call empty new() constructor of Type
                            obj.ExposeDataForSaveAndLoad();
                        }
                        else
                        {
                            throw new Exception($"Unknown or invalid type '{typeName}' for field '{label}'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save and load a bool.
        /// </summary>
        public static void SaveOrLoadPrimitive(ref bool value, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, value.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    value = bool.Parse(reader.ReadElementContentAsString());
                }
            }
        }

        /// <summary>
        /// Save and load an int.
        /// </summary>
        public static void SaveOrLoadPrimitive(ref int value, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, value.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    value = int.Parse(reader.ReadElementContentAsString());
                }
            }
        }

        /// <summary>
        /// Save and load a float.
        /// </summary>
        public static void SaveOrLoadPrimitive(ref float value, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, value.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    value = float.Parse(reader.ReadElementContentAsString());
                }
            }
        }

        /// <summary>
        /// Save and load a string.
        /// </summary>
        public static void SaveOrLoadPrimitive(ref string value, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, value);
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    value = reader.ReadElementContentAsString();
                }
            }
        }

        /// <summary>
        /// Save or load an enum.
        /// </summary>
        public static void SaveOrLoadPrimitive<T>(ref T value, string label) where T : Enum
        {
            if (IsSaving)
            {
                // Save the enum as a string for readability.
                writer.WriteElementString(label, value.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string enumString = reader.ReadElementContentAsString();
                    try
                    {
                        // Parse the string back into the enum type.
                        value = (T)Enum.Parse(typeof(T), enumString);
                    }
                    catch (ArgumentException)
                    {
                        throw new Exception($"Failed to parse enum value '{enumString}' for enum type '{typeof(T).Name}' in field '{label}'.");
                    }
                }
            }
        }

        /// <summary>
        /// Save and load a color.
        /// </summary>
        public static void SaveOrLoadColor(ref Color color, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, color.r + "," + color.g + "," + color.b);
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(',');
                    color = new Color(float.Parse(values[0]), float.Parse(values[1]), float.Parse(values[2]));
                }
            }
        }

        /// <summary>
        /// Save and load a Def.
        /// </summary>
        public static void SaveOrLoadDef<T>(ref T value, string label) where T : Def
        {
            if (IsSaving)
            {
                writer.WriteStartElement(label);

                if (value != null)
                {
                    writer.WriteAttributeString("Type", value.GetType().FullName);
                    writer.WriteString(value.DefName); // Write the def name as the content of the element
                }

                writer.WriteEndElement();
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string typeName = reader.GetAttribute("Type");
                    string defName = reader.ReadElementContentAsString();

                    if (!string.IsNullOrEmpty(typeName) && !string.IsNullOrEmpty(defName))
                    {
                        Type type = Type.GetType(typeName);
                        if (type == null || !typeof(Def).IsAssignableFrom(type))
                        {
                            throw new Exception($"Invalid Def type '{typeName}' for label '{label}'.");
                        }

                        // Construct the DefDatabase<> type for the specific Def type
                        Type defDatabaseType = typeof(DefDatabase<>).MakeGenericType(type);
                        MethodInfo getNamedMethod = defDatabaseType.GetMethod("GetNamed", BindingFlags.Static | BindingFlags.Public);

                        if (getNamedMethod != null)
                        {
                            // Call GetNamed to fetch the Def by name
                            value = (T)getNamedMethod.Invoke(null, new object[] { defName });
                        }
                        else
                        {
                            throw new Exception($"Method 'GetNamed' not found on DefDatabase<{type.Name}> for label '{label}'.");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Save and load a list.
        /// </summary>
        public static void SaveOrLoadList<T>(ref List<T> list, string label) where T : class, ISaveAndLoadable
        {
            if (IsSaving)
            {
                writer.WriteStartElement(label); // Begin list group
                if (list != null)
                {
                    foreach (var item in list)
                    {
                        T tempItem = item; // Needed for ref parameter
                        SaveOrLoadObject(ref tempItem, "li");
                    }
                }
                writer.WriteEndElement(); // End list group
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label)) // Move to the list's group
                {
                    list = new List<T>();
                    while (reader.ReadToFollowing("li"))
                    {
                        T value = default;
                        SaveOrLoadObject(ref value, "Value");
                        list.Add(value);
                    }
                }
            }
        }

        /// <summary>
        /// Save and load a list.
        /// </summary>
        public static void SaveOrLoadStringHashSet(ref HashSet<string> list, string label) 
        {
            if (IsSaving)
            {
                string saveText = "";
                foreach (string s in list) saveText += s + ",";
                saveText = saveText.TrimEnd(',');
                writer.WriteElementString(label, saveText);
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string value = reader.ReadElementContentAsString();
                    if (value == "") list = new HashSet<string>();
                    else list = value.Split(',').ToHashSet();
                }
            }
        }

        /// <summary>
        /// Save and load an int-keyed dictionary with objects as values.
        /// </summary>
        public static void SaveOrLoadDeepDictionary<T>(ref Dictionary<int, T> dictionary, string label) where T : class, ISaveAndLoadable
        {
            if (IsSaving)
            {
                writer.WriteStartElement(label);

                foreach (var kvp in dictionary)
                {
                    writer.WriteStartElement("li");
                    writer.WriteAttributeString("Key", kvp.Key.ToString());

                    T value = kvp.Value;
                    SaveOrLoadObject(ref value, "Value");

                    writer.WriteEndElement();
                }

                writer.WriteEndElement();
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    using (XmlReader subReader = reader.ReadSubtree())
                    {
                        while (subReader.ReadToFollowing("li"))
                        {
                            string keyString = subReader.GetAttribute("Key");
                            if (!int.TryParse(keyString, out int key))
                            {
                                throw new Exception($"Invalid dictionary key '{keyString}' in field '{label}'.");
                            }

                            T value = default;
                            SaveOrLoadObject(ref value, "Value");

                            dictionary[key] = value;
                        }
                    }
                }
            }
        }

        public static void SaveOrLoadAltitudeDictionary(ref Dictionary<Direction, int> dictionary)
        {
            if (IsSaving)
            {
                writer.WriteElementString("altitude", dictionary[Direction.NE] + "," + dictionary[Direction.SE] + "," + dictionary[Direction.SW] + "," + dictionary[Direction.NW]);
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing("altitude"))
                {
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(',');
                    dictionary = new Dictionary<Direction, int>()
                    {
                        { Direction.NE, int.Parse(values[0]) },
                        { Direction.SE, int.Parse(values[1]) },
                        { Direction.SW, int.Parse(values[2]) },
                        { Direction.NW, int.Parse(values[3]) },
                    };
                }
            }
        }

        /// <summary>
        /// Save and load a Vector2Int.
        /// </summary>
        public static void SaveOrLoadVector2Int(ref Vector2Int vector, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, vector.x.ToString() + "," + vector.y.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(',');
                    vector = new Vector2Int(int.Parse(values[0]), int.Parse(values[1]));
                }
            }
        }

        /// <summary>
        /// Save and load a Vector3Int.
        /// </summary>
        public static void SaveOrLoadVector3Int(ref Vector3Int vector, string label)
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, vector.x.ToString() + "," + vector.y.ToString() + "," + vector.z.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(',');
                    vector = new Vector3Int(int.Parse(values[0]), int.Parse(values[1]), int.Parse(values[2]));
                }
            }
        }

        /// <summary>
        /// Save and load a Vector2Int.
        /// </summary>
        public static void SaveOrLoadVector2IntSet(ref HashSet<Vector2Int> list, string label)
        {
            if (IsSaving)
            {
                StringBuilder sb = new StringBuilder();
                foreach (Vector2Int v in list) sb.Append(v.x + "," + v.y + ";");
                writer.WriteElementString(label, sb.ToString().TrimEnd(';'));
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    list = new HashSet<Vector2Int>();
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(';');
                    foreach (string vecString in values)
                    {
                        string[] atts = vecString.Split(',');
                        list.Add(new Vector2Int(int.Parse(values[0]), int.Parse(values[1])));
                    }
                }
            }
        }

        /// <summary>
        /// Save and load a reference to another saveable object. If loading, make sure that the object you're referencing has already been loaded at this point.
        /// </summary>
        public static void SaveOrLoadReference<T>(ref T obj, string label) where T : WorldDatabaseObject
        {
            if (IsSaving)
            {
                writer.WriteElementString(label, obj.Id.ToString());
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    // Read the ID of the referenced object
                    int objId = int.Parse(reader.ReadElementContentAsString());

                    // Resolve the reference based on the expected type
                    if (typeof(Actor).IsAssignableFrom(typeof(T))) obj = LoadingWorld.GetActor(objId) as T;
                    else if (typeof(BlockmapNode).IsAssignableFrom(typeof(T))) obj = LoadingWorld.GetNode(objId) as T;
                    else if (typeof(Entity).IsAssignableFrom(typeof(T))) obj = LoadingWorld.GetEntity(objId) as T;
                    else throw new Exception($"Unsupported reference type '{typeof(T)}' for field '{label}'.");

                    // Validate that the reference was successfully resolved
                    if (obj == null) throw new Exception($"Failed to resolve reference for type '{typeof(T)}' with ID '{objId}' in field '{label}'.");
                }
            }
        }

        /// <summary>
        /// Save and load a list of references to other saveable objects. 
        /// If loading, make sure that the objects you're referencing have already been loaded at this point.
        /// </summary>
        public static void SaveOrLoadReferenceList<T>(ref List<T> list, string label) where T : WorldDatabaseObject
        {
            if (IsSaving)
            {
                StringBuilder sb = new StringBuilder();
                foreach (T obj in list) sb.Append(obj.Id + ",");
                writer.WriteElementString(label, sb.ToString().TrimEnd(','));
            }
            else if (IsLoading)
            {
                if (reader.ReadToFollowing(label))
                {
                    list = new List<T>();
                    string value = reader.ReadElementContentAsString();
                    string[] values = value.Split(',');
                    foreach(string idString in values)
                    {
                        T resolvedObj = null;
                        int objId = int.Parse(idString);
                        if (typeof(Actor).IsAssignableFrom(typeof(T))) resolvedObj = LoadingWorld.GetActor(objId) as T;
                        else if (typeof(BlockmapNode).IsAssignableFrom(typeof(T))) resolvedObj = LoadingWorld.GetNode(objId) as T;
                        else if (typeof(Entity).IsAssignableFrom(typeof(T))) resolvedObj = LoadingWorld.GetEntity(objId) as T;
                        else if (typeof(Wall).IsAssignableFrom(typeof(T))) resolvedObj = LoadingWorld.GetWall(objId) as T;
                        else throw new Exception($"Unsupported reference type '{typeof(T)}' in list '{label}'.");

                        // Validate that the reference was successfully resolved
                        if (resolvedObj == null) throw new Exception($"Failed to resolve reference for type '{typeof(T)}' with ID '{objId}' in field '{label}'.");

                        list.Add(resolvedObj);
                    }
                }
            }
        }
    }
}
