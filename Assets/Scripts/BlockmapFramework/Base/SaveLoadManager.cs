using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
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
        /// Loads an object of type T from a specified XML file.
        /// </summary>
        /// <typeparam name="T">The type of the object to load, which must implement IExposable.</typeparam>
        /// <param name="fileName">The name of the XML file to load from.</param>
        /// <returns>The deserialized object of type T.</returns>
        public static T Load<T>(string fileName) where T : class, ISaveAndLoadable
        {
            IsLoading = true;

            string fullPath = SaveDataPath + fileName + ".xml";
            Debug.Log($"Loading world {fullPath}.");
            using (FileStream fileStream = new FileStream(fullPath, FileMode.Open, FileAccess.Read))
            {
                reader = XmlReader.Create(fileStream);
                reader.ReadToFollowing("Root");

                T rootObject = null;
                SaveOrLoadObject(ref rootObject, "Data");

                EndLoad();
                return rootObject;
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
                            Debug.Log($"Creating instance of type: {type.FullName}");
                            Debug.Log($"Expected type: {typeof(T).FullName}");

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
        /// Save and load an int.
        /// </summary>
        public static void SaveOrLoadInt(ref int value, string label)
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
        /// Save and load a string.
        /// </summary>
        public static void SaveOrLoadString(ref string value, string label)
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
                    dictionary = new Dictionary<int, T>();

                    while (reader.ReadToFollowing("li"))
                    {
                        string keyString = reader.GetAttribute("Key");
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
    }
}
