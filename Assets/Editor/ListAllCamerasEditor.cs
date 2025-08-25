// Assets/Editor/ListAllCamerasEditor.cs
using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Collections.Generic;

public class ListAllCamerasEditor : EditorWindow
{
    [MenuItem("Tools/Debug/List All Cameras (Scene + Assets)")]
    static void Open()
    {
        GetWindow<ListAllCamerasEditor>("List All Cameras");
    }

    void OnGUI()
    {
        if (GUILayout.Button("Find & Log All Cameras (including inactive & prefabs)"))
        {
            LogAllCameras();
        }

        GUILayout.Space(8);
        GUILayout.Label("Instructions:", EditorStyles.boldLabel);
        GUILayout.Label("- This tool uses Resources.FindObjectsOfTypeAll to locate Cameras anywhere Unity knows about.");
        GUILayout.Label("- After clicking the button, check Console for a detailed list.");
        GUILayout.Label("- If you find unexpected cameras, you can select them in the Hierarchy or Project to inspect/delete/disable.");
    }

    static void LogAllCameras()
    {
        Debug.Log("=== Listing all Camera objects Unity can find ===");

        // FindObjectsOfTypeAll returns inactive objects and assets/prefabs too
        Object[] allCams = Resources.FindObjectsOfTypeAll(typeof(Camera));
        if (allCams == null || allCams.Length == 0)
        {
            Debug.Log("No Camera objects found by Resources.FindObjectsOfTypeAll.");
            return;
        }

        int idx = 0;
        foreach (Object obj in allCams)
        {
            idx++;
            Camera cam = obj as Camera;
            if (cam == null) continue;

            string name = cam.gameObject.name;
            string active = cam.gameObject.activeInHierarchy ? "ActiveInHierarchy" : "InactiveInHierarchy";
            string tag = cam.gameObject.tag;
            Vector3 pos = cam.transform.position;
            string sceneName = (cam.gameObject.scene.IsValid()) ? cam.gameObject.scene.name : "<no-scene (asset/prefab)>";
            string path = AssetDatabase.GetAssetPath(cam.gameObject);
            if (string.IsNullOrEmpty(path))
                path = "<no asset path - scene object or dynamically created>";
            bool isPrefabAsset = !string.IsNullOrEmpty(AssetDatabase.GetAssetPath(cam)) && AssetDatabase.GetMainAssetTypeAtPath(AssetDatabase.GetAssetPath(cam)) != null;
            string parent = cam.transform.parent != null ? cam.transform.parent.name : "<root>";
            string info = string.Format(
                "{0}) Name: {1} | {2} | Tag:{3} | Scene:{4} | Parent:{5} | Pos:{6} | AssetPath:{7}",
                idx, name, active, tag, sceneName, parent, pos.ToString("F3"), path
            );

            Debug.Log(info, cam.gameObject);
        }

        Debug.Log(string.Format("=== Found {0} Camera objects ===", allCams.Length));
    }
}
