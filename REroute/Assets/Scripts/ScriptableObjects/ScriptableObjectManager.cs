using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

public class ScriptableObjectManager
{
    static string pathPrefix = "Assets/ScriptableObjectData/";

    public static T Load<T>() where T : ScriptableObject
    {
        //return Instance.ledge_data;
        string typeName = typeof(T).Name;
        return AssetDatabase.LoadAssetAtPath<T>(pathPrefix + typeName + ".asset");
        //return Resources.Load<T>(pathPrefix + typeName);
    }
}
