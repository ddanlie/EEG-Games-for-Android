using System.IO;
using UnityEngine;

public static class LocalStorage
{
    private static string FilePath(string fileName, bool temporary) =>
        Path.Combine(temporary ? Application.temporaryCachePath : Application.persistentDataPath, fileName);

    public static void Save<T>(string fileName, T data, bool temporary = false)
    {
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(FilePath(fileName, temporary), json);
    }

    public static T Load<T>(string fileName, bool temporary = false)
    {
        string path = FilePath(fileName, temporary);
        if (!File.Exists(path))
            return default;

        string json = File.ReadAllText(path);
        return JsonUtility.FromJson<T>(json);
    }

    public static void Delete(string fileName, bool temporary = false)
    {
        string path = FilePath(fileName, temporary);
        if (File.Exists(path))
            File.Delete(path);
    }
}