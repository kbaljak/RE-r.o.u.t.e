using UnityEngine;
using UnityEditor;

#if UNITY_EDITOR
[CustomEditor(typeof(ItemSpawner))]
public class ItemSpawnerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        ItemSpawner spawner = (ItemSpawner)target;
        
        EditorGUILayout.Space(10);
        EditorGUILayout.LabelField("Setup Tools", EditorStyles.boldLabel);
        
        if (GUILayout.Button("Generate Spawn Points from Scene Items"))
        {
            if (EditorUtility.DisplayDialog("Generate Spawn Points", 
                "This will find all GameObjects on the 'Pickup' layer and create spawn points for them. Continue?", 
                "Yes", "Cancel"))
            {
                // Use reflection to call the private method
                var method = typeof(ItemSpawner).GetMethod("GenerateSpawnPointsFromScene", 
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
                method?.Invoke(spawner, null);
                EditorUtility.SetDirty(spawner);
            }
        }
        
        EditorGUILayout.HelpBox(
            "Setup Steps:\n" +
            "1. Add pickup items to scene on 'Pickup' layer\n" +
            "2. Click 'Generate Spawn Points from Scene Items'\n" +
            "3. Assign item prefabs in inspector\n" +
            "4. Delete original scene items (spawner will create them)\n" +
            "5. Add NetworkObject to this GameObject",
            MessageType.Info);
        
        if (Application.isPlaying && spawner.IsServerInitialized)
        {
            EditorGUILayout.Space(10);
            EditorGUILayout.LabelField("Runtime Controls (Server Only)", EditorStyles.boldLabel);
            
            if (GUILayout.Button("Spawn All Items Now"))
            {
                spawner.SpawnAllItems();
            }
        }
    }
}
#endif