using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(VTKFireFlowDemo))]
public class VTKFireFlowDemoEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        VTKFireFlowDemo demo = (VTKFireFlowDemo)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);
        
        // 清理按鈕
        GUI.backgroundColor = Color.red;
        if (GUILayout.Button("清理所有流線", GUILayout.Height(30)))
        {
            ClearAllStreamlines(demo);
        }
        GUI.backgroundColor = Color.white;
        
        // 重新生成按鈕
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("重新生成流線", GUILayout.Height(30)))
        {
            ClearAllStreamlines(demo);
            demo.SendMessage("GenerateStreamlines", SendMessageOptions.DontRequireReceiver);
        }
        GUI.backgroundColor = Color.white;
        
        // 顯示統計
        EditorGUILayout.Space();
        int streamlineCount = CountStreamlines(demo);
        EditorGUILayout.HelpBox($"當前流線數量: {streamlineCount}", MessageType.Info);
    }
    
    void ClearAllStreamlines(VTKFireFlowDemo demo)
    {
        // 清理所有名稱包含 Streamline 的子物件
        var toDelete = new System.Collections.Generic.List<GameObject>();
        
        foreach (Transform child in demo.transform)
        {
            if (child.name.Contains("Streamline"))
            {
                toDelete.Add(child.gameObject);
            }
        }
        
        foreach (var obj in toDelete)
        {
            // DestroyImmediate(obj);
            UnityEngine.Object.DestroyImmediate(obj);

        }
        
        // 清理列表
        var field = typeof(VTKFireFlowDemo).GetField("streamlineObjects", 
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        if (field != null)
        {
            var list = field.GetValue(demo) as System.Collections.Generic.List<GameObject>;
            if (list != null)
            {
                list.Clear();
            }
        }
        
        Debug.Log($"[編輯器] 清理了 {toDelete.Count} 條流線");
        EditorUtility.SetDirty(demo);
    }
    
    int CountStreamlines(VTKFireFlowDemo demo)
    {
        int count = 0;
        foreach (Transform child in demo.transform)
        {
            if (child.name.Contains("Streamline"))
            {
                count++;
            }
        }
        return count;
    }
}

// 選單項目
public static class VTKEditorMenu
{
    [MenuItem("Tools/VTK/清理場景中所有流線")]
    public static void ClearAllStreamlinesInScene()
    {
        var demos = GameObject.FindObjectsOfType<VTKFireFlowDemo>();
        int totalCleared = 0;
        
        foreach (var demo in demos)
        {
            var toDelete = new System.Collections.Generic.List<GameObject>();
            
            foreach (Transform child in demo.transform)
            {
                if (child.name.Contains("Streamline"))
                {
                    toDelete.Add(child.gameObject);
                }
            }
            
            foreach (var obj in toDelete)
            {
                // DestroyImmediate(obj);
                UnityEngine.Object.DestroyImmediate(obj);

            }
            
            totalCleared += toDelete.Count;
        }
        
        Debug.Log($"[選單] 清理了場景中 {totalCleared} 條流線");
    }
    
    [MenuItem("Tools/VTK/強制重新整理")]
    public static void ForceRefresh()
    {
        var demos = GameObject.FindObjectsOfType<VTKFireFlowDemo>();
        foreach (var demo in demos)
        {
            demo.SendMessage("ClearAllStreamlines", SendMessageOptions.DontRequireReceiver);
            demo.SendMessage("GenerateStreamlines", SendMessageOptions.DontRequireReceiver);
        }
        
        Debug.Log($"[選單] 強制重新整理 {demos.Length} 個 VTK 物件");
    }
}