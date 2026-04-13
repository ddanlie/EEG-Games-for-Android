using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using UnityEngine;
using UXF;

public class GeneralUtilities
{
    public static GameObject FindChildByNameRecursive(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;

            var result = FindChildByNameRecursive(child, name);
            if (result != null)
                return result;
        }
        return null;
    }

    public static Dictionary<string, GameObject> FindChildrenByNames(Transform parent, List<string> names)
    {
        var results = new Dictionary<string, GameObject>();

        foreach (Transform child in parent)
        {
            if (names.Contains(child.name) && !results.ContainsKey(child.name))
            {
                results[child.name] = child.gameObject;
            }
        }

        return results;
    }

    public static Dictionary<string, GameObject> FindChildrenByNamesRecursive(Transform parent, List<string> names)
    {
        var results = new Dictionary<string, GameObject>();

        void Search(Transform current)
        {
            if (results.Count == names.Count)
                return;

            if (names.Contains(current.name) && !results.ContainsKey(current.name))
            {
                results[current.name] = current.gameObject;
            }

            foreach (Transform child in current)
            {
                Search(child);
            }
        }

        Search(parent);

        return results;
    }

    public static void OpenWifiPanel()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
    using (var unityPlayer = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
    {
        var currentActivity = unityPlayer.GetStatic<AndroidJavaObject>("currentActivity");
        var intent = new AndroidJavaObject("android.content.Intent", "android.settings.panel.action.INTERNET_CONNECTIVITY");
        currentActivity.Call("startActivity", intent);
    }
#endif
    }

    public static void SaveEEGListAsTrialData(
        List<double[]> data,
        Session session,
        string dataName = "eeg_data")
    {
        if (data.Count == 0) { return;  };

        // make headers
        int channelCount = data[0].Length;
        string[] headers = new string[channelCount];
        for (int i = 0; i < channelCount-1; i++)
        {
            headers[i] = $"channel_{i + 1}";
        }
        headers[headers.Length - 1] = "unix_timestamp_ms";

        // fill UXFDataTable
        var table = new UXFDataTable(headers);
        foreach (var sample in data)
        {
            var row = new UXFDataRow();
            for (int i = 0; i < channelCount; i++)
            {
                row.Add((headers[i], sample[i]));
            }
            table.AddCompleteRow(row);
        }

        session.CurrentTrial.SaveDataTable(table, dataName, UXFDataType.OtherTrialData);
    }
}
