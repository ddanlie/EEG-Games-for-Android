using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class GeneralUtilities
{
    public static GameObject FindChildByName(Transform parent, string name)
    {
        foreach (Transform child in parent)
        {
            if (child.name == name)
                return child.gameObject;

            var result = FindChildByName(child, name);
            if (result != null)
                return result;
        }
        return null;
    }
}
