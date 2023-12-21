using BlockmapFramework;
using Newtonsoft.Json;
using Newtonsoft.Json.Converters;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class JsonUtilities
{
    private static string DATA_FILES_PATH = Application.streamingAssetsPath + "/SaveData/";

    public static void SaveWorld(WorldData data)
    {
        string json = JsonConvert.SerializeObject(data, Formatting.Indented);
        string path = DATA_FILES_PATH + data.Name + ".json";
        File.WriteAllText(path, json);
        Debug.Log("Successfully saved " + data.Name + " data:\n\n" + json);
    }

    public static WorldData LoadWorld(string name)
    {
        string jsonFilePath = DATA_FILES_PATH + name + ".json";
        WorldData data = null;

        using (StreamReader r = new StreamReader(jsonFilePath))
        {
            string json = r.ReadToEnd();
            data = JsonConvert.DeserializeObject<WorldData>(json);
        }

        if (data == null) throw new System.Exception("Didn't find the save file " + jsonFilePath);
        return data;
    }
}
