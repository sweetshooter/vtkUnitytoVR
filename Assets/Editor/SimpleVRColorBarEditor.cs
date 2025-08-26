#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(SimpleVRColorBar))]
public class SimpleVRColorBarEditor : Editor
{
    private Texture2D previewTexture;

    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        
        SimpleVRColorBar colorBar = (SimpleVRColorBar)target;
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("編輯器預覽", EditorStyles.boldLabel);
        
        // 顯示 ColorMap 預覽
        DrawColorMapPreview(colorBar);
        
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("工具", EditorStyles.boldLabel);
        
        // 強制重新初始化按鈕
        if (GUILayout.Button("強制重新初始化", GUILayout.Height(30)))
        {
            colorBar.ForceReinitialize();
        }
        
        // 調試狀態按鈕
        if (GUILayout.Button("顯示調試狀態"))
        {
            colorBar.DebugStatus();
        }
        
        // 切換顯示按鈕（編輯器中測試用）
        GUI.backgroundColor = colorBar.showColorBar ? Color.red : Color.green;
        if (GUILayout.Button(colorBar.showColorBar ? "隱藏 ColorBar" : "顯示 ColorBar"))
        {
            colorBar.showColorBar = !colorBar.showColorBar;
            colorBar.UpdateVisibility();
        }
        GUI.backgroundColor = Color.white;
        
        EditorGUILayout.Space();
        
        // 顯示當前狀態
        EditorGUILayout.HelpBox($"顯示狀態: {colorBar.showColorBar}\n" +
                               $"切換鍵: {colorBar.toggleKey}\n" +
                               $"位置: {colorBar.localPosition}\n" +
                               $"縮放: {colorBar.scale}", 
                               MessageType.Info);
        
        // 如果值改變了，重繪預覽
        if (GUI.changed)
        {
            if (previewTexture != null)
            {
                DestroyImmediate(previewTexture);
                previewTexture = null;
            }
        }
    }

    void DrawColorMapPreview(SimpleVRColorBar colorBar)
    {
        if (previewTexture == null)
        {
            CreatePreviewTexture(colorBar);
        }

        if (previewTexture != null)
        {
            // 顯示色條預覽
            Rect previewRect = GUILayoutUtility.GetRect(200, 30);
            EditorGUI.DrawPreviewTexture(previewRect, previewTexture);
            
            // 顯示數值標籤
            Rect labelRect = GUILayoutUtility.GetRect(200, 20);
            string minLabel = colorBar.minValue.ToString(colorBar.valueFormat) + colorBar.unit;
            string maxLabel = colorBar.maxValue.ToString(colorBar.valueFormat) + colorBar.unit;
            
            GUI.Label(new Rect(labelRect.x, labelRect.y, 100, 20), minLabel);
            GUI.Label(new Rect(labelRect.x + 100, labelRect.y, 100, 20), maxLabel, new GUIStyle(GUI.skin.label) { alignment = TextAnchor.MiddleRight });
        }
    }

    void CreatePreviewTexture(SimpleVRColorBar colorBar)
    {
        previewTexture = new Texture2D(256, 1, TextureFormat.RGBA32, false);
        
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            if (colorBar.invertColors) t = 1f - t;
            
            Color c;
            if (colorBar.useJetColorMap)
            {
                c = GetJetColorPreview(t);
            }
            else
            {
                c = Color.HSVToRGB(0.67f * (1f - t), 1f, 1f);
            }
            
            previewTexture.SetPixel(i, 0, c);
        }
        
        previewTexture.Apply();
        previewTexture.wrapMode = TextureWrapMode.Clamp;
        previewTexture.filterMode = FilterMode.Bilinear;
    }

    Color GetJetColorPreview(float t)
    {
        // 複製 Jet 色彩映射
        Color[] jetLUT = new Color[]
        {
            new Color(0.0f, 0.0f, 0.5f),
            new Color(0.0f, 0.0f, 1.0f),
            new Color(0.0f, 0.5f, 1.0f),
            new Color(0.0f, 1.0f, 1.0f),
            new Color(0.5f, 1.0f, 0.5f),
            new Color(1.0f, 1.0f, 0.0f),
            new Color(1.0f, 0.5f, 0.0f),
            new Color(1.0f, 0.0f, 0.0f),
            new Color(0.5f, 0.0f, 0.0f)
        };

        t = Mathf.Clamp01(t);
        float s = t * (jetLUT.Length - 1);
        int idx = Mathf.FloorToInt(s);
        float f = s - idx;
        if (idx >= jetLUT.Length - 1) return jetLUT[jetLUT.Length - 1];
        return Color.Lerp(jetLUT[idx], jetLUT[idx + 1], f);
    }

    void OnDisable()
    {
        if (previewTexture != null)
        {
            DestroyImmediate(previewTexture);
            previewTexture = null;
        }
    }
}
#endif