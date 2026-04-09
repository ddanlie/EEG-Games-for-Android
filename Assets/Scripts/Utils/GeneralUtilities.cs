using System.Collections;
using System.Collections.Generic;
using UnityEngine;

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


}
