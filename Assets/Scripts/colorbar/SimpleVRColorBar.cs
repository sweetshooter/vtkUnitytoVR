using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SimpleVRColorBar : MonoBehaviour
{
    [Header("外觀")]
    public int barWidth = 20;
    public int barHeight = 200;
    public int margin = 20;
    public int labelCount = 5;
    public string valueFormat = "F1";
    public string unit = "K";
    public string titleText = "Temperature";

    [Header("色彩")]
    public bool useJetColorMap = true;
    public bool invertColors = true;

    [Header("資料範圍")]
    public float minValue = 293f;
    public float maxValue = 873f;

    [Header("VR 固定位置設定")]
    [Tooltip("相對於攝影機的本地位置")]
    public Vector3 localPosition = new Vector3(-0.4f, 0.3f, 0.8f);
    [Tooltip("相對於攝影機的本地旋轉")]
    public Vector3 localRotation = new Vector3(0f, 15f, 0f);
    [Tooltip("ColorBar 的縮放")]
    public float scale = 0.001f;

    [Header("控制")]
    public bool showColorBar = true;
    [Tooltip("切換顯示的按鍵")]
    public KeyCode toggleKey = KeyCode.B;

    [Header("Debug 設定")]
    [Tooltip("是否在螢幕上顯示 Debug 資訊")]
    public bool showDebugOnScreen = false;

    // 內部變數
    private Camera playerCamera;
    private GameObject canvasObject;
    private RawImage colorBarImage;
    private Text[] labels;
    private Texture2D colorBarTexture;
    private Text title;
    private bool isInitialized = false;

    // Jet 色彩映射
    static readonly Color[] JetLUT = new Color[]
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

    void Start()
    {
        Initialize();
    }

    void OnEnable()
    {
        // 在 Editor 模式下也要初始化
        if (!isInitialized)
        {
            Initialize();
        }
    }

    void Initialize()
    {
        if (isInitialized) return;

        // 找到主攝影機
        playerCamera = Camera.main;
        if (playerCamera == null)
        {
            playerCamera = FindObjectOfType<Camera>();
        }

        if (playerCamera == null)
        {
            Debug.LogError("[SimpleVRColorBar] 找不到攝影機！");
            return;
        }

        // 清理現有的 Canvas（避免重複）
        if (canvasObject != null)
        {
            if (Application.isPlaying)
                Destroy(canvasObject);
            else
                DestroyImmediate(canvasObject);
        }

        CreateVRCanvas();
        CreateColorBarUI();
        UpdateColorBar();
        UpdateVisibility();

        isInitialized = true;
        Debug.Log($"[SimpleVRColorBar] 初始化完成，Canvas: {canvasObject != null}, 初始顯示設定: {showColorBar}");
        
        // 確保初始狀態正確
        if (canvasObject != null)
        {
            Debug.Log($"[Initialize] Canvas 初始 active 狀態: {canvasObject.activeSelf}");
        }
    }

    void Update()
    {
        if (!isInitialized) 
        {
            Initialize(); // 嘗試重新初始化
            return;
        }

        // 處理顯示切換 - 加強版按鍵檢測
        if (Input.GetKeyDown(toggleKey))
        {
            Debug.Log($"[按鍵檢測] 偵測到 {toggleKey} 按下");
            
            bool oldState = showColorBar;
            showColorBar = !showColorBar;
            
            Debug.Log($"[按鍵檢測] 狀態改變: {oldState} → {showColorBar}");
            
            UpdateVisibility();
            
            // 額外驗證
            if (canvasObject != null)
            {
                Debug.Log($"[按鍵檢測] Canvas 最終狀態: {canvasObject.activeSelf}");
            }
        }

        // 額外的按鍵檢測（備用）
        if (Input.GetKeyDown(KeyCode.V))
        {
            Debug.Log("[備用按鍵] V 鍵切換 ColorBar");
            showColorBar = !showColorBar;
            UpdateVisibility();
        }

        // 更新位置跟隨攝影機
        UpdatePosition();
    }

    void CreateVRCanvas()
    {
        // 創建 Canvas GameObject
        canvasObject = new GameObject("VR_ColorBar_Canvas");
        
        // 設為攝影機的子物件，這樣會自動跟隨
        canvasObject.transform.SetParent(playerCamera.transform, false);
        
        // 設定本地位置和旋轉
        canvasObject.transform.localPosition = localPosition;
        canvasObject.transform.localRotation = Quaternion.Euler(localRotation);
        canvasObject.transform.localScale = Vector3.one * scale;

        // 設定 Canvas 組件
        var canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        // 設定 Canvas 尺寸
        var canvasRect = canvasObject.GetComponent<RectTransform>();
        float frameW = barWidth + 2 * margin + 100f;
        float frameH = barHeight + 2 * margin + 40f;
        canvasRect.sizeDelta = new Vector2(frameW, frameH);

        // 添加 GraphicRaycaster（雖然在這個簡化版本中可能不需要）
        canvasObject.AddComponent<GraphicRaycaster>();
    }

    void CreateColorBarUI()
    {
        // 背景
        GameObject bgObject = new GameObject("Background");
        bgObject.transform.SetParent(canvasObject.transform, false);
        var bgImg = bgObject.AddComponent<Image>();
        bgImg.color = new Color(0, 0, 0, 0.8f);

        var bgRect = bgObject.GetComponent<RectTransform>();
        bgRect.anchorMin = new Vector2(0.5f, 0.5f);
        bgRect.anchorMax = new Vector2(0.5f, 0.5f);
        bgRect.pivot = new Vector2(0.5f, 0.5f);
        bgRect.anchoredPosition = Vector2.zero;
        
        float frameW = barWidth + 2 * margin + 100f;
        float frameH = barHeight + 2 * margin + 40f;
        bgRect.sizeDelta = new Vector2(frameW, frameH);

        // 色條
        var barObj = new GameObject("ColorBar");
        barObj.transform.SetParent(bgObject.transform, false);
        colorBarImage = barObj.AddComponent<RawImage>();
        
        var barRect = colorBarImage.rectTransform;
        barRect.anchorMin = new Vector2(0f, 0f);
        barRect.anchorMax = new Vector2(0f, 0f);
        barRect.pivot = new Vector2(0f, 0f);
        barRect.anchoredPosition = new Vector2(margin, margin);
        barRect.sizeDelta = new Vector2(barWidth, barHeight);

        // 標籤
        labels = new Text[labelCount];
        for (int i = 0; i < labelCount; i++)
        {
            var labelObj = new GameObject($"Label_{i}");
            labelObj.transform.SetParent(bgObject.transform, false);
            var label = labelObj.AddComponent<Text>();
            label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            label.fontSize = 14;
            label.color = Color.white;
            label.alignment = TextAnchor.MiddleLeft;
            labels[i] = label;

            var labelRect = label.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0f, 0f);
            labelRect.anchorMax = new Vector2(0f, 0f);
            labelRect.pivot = new Vector2(0f, 0.5f);

            float t = (labelCount == 1) ? 0f : i / (float)(labelCount - 1);
            float y = margin + t * barHeight;
            labelRect.anchoredPosition = new Vector2(margin + barWidth + 15f, y);
            labelRect.sizeDelta = new Vector2(80, 20);
        }

        // 標題
        var titleObj = new GameObject("Title");
        titleObj.transform.SetParent(bgObject.transform, false);
        title = titleObj.AddComponent<Text>();
        title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        title.fontSize = 16;
        title.color = Color.white;
        title.alignment = TextAnchor.MiddleCenter;
        title.text = titleText;

        var titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -5f);
        titleRect.sizeDelta = new Vector2(frameW - 10f, 20f);
    }

    void UpdateColorBar()
    {
        if (colorBarImage == null) return;

        // 建立色條材質
        if (colorBarTexture == null)
        {
            colorBarTexture = new Texture2D(1, 256, TextureFormat.RGBA32, false);
            colorBarTexture.wrapMode = TextureWrapMode.Clamp;
            colorBarTexture.filterMode = FilterMode.Bilinear;
        }

        // 填充色條
        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            if (invertColors) t = 1f - t;
            Color c = useJetColorMap ? GetJetColor(t) : GetSimpleColor(t);
            colorBarTexture.SetPixel(0, i, c);
        }
        colorBarTexture.Apply();
        colorBarImage.texture = colorBarTexture;

        // 更新標籤
        if (labels != null)
        {
            for (int i = 0; i < labelCount && i < labels.Length; i++)
            {
                if (labels[i] != null)
                {
                    float t = (labelCount == 1) ? 0f : i / (float)(labelCount - 1);
                    float v = Mathf.Lerp(minValue, maxValue, t);
                    labels[i].text = v.ToString(valueFormat) + unit;
                }
            }
        }
    }

    void UpdatePosition()
    {
        if (canvasObject != null && playerCamera != null)
        {
            // 確保位置跟隨攝影機
            canvasObject.transform.localPosition = localPosition;
            canvasObject.transform.localRotation = Quaternion.Euler(localRotation);
        }
    }

    public void UpdateVisibility()
    {
        if (canvasObject != null)
        {
            bool wasActive = canvasObject.activeSelf;
            canvasObject.SetActive(showColorBar);
            bool isNowActive = canvasObject.activeSelf;
            
            Debug.Log($"[UpdateVisibility] showColorBar={showColorBar}, was={wasActive}, now={isNowActive}, changed={wasActive != isNowActive}");
            
            // 如果狀態沒有改變，可能是其他原因
            if (wasActive == isNowActive && wasActive != showColorBar)
            {
                Debug.LogWarning($"[UpdateVisibility] Canvas 狀態沒有改變！預期={showColorBar}, 實際={isNowActive}");
                
                // 嘗試強制設定
                canvasObject.SetActive(!showColorBar);
                canvasObject.SetActive(showColorBar);
                Debug.Log($"[UpdateVisibility] 強制切換後狀態: {canvasObject.activeSelf}");
            }
        }
        else
        {
            Debug.LogWarning("[UpdateVisibility] canvasObject 為 null！需要重新初始化");
        }
    }

    Color GetJetColor(float t)
    {
        t = Mathf.Clamp01(t);
        float s = t * (JetLUT.Length - 1);
        int idx = Mathf.FloorToInt(s);
        float f = s - idx;
        if (idx >= JetLUT.Length - 1) return JetLUT[JetLUT.Length - 1];
        return Color.Lerp(JetLUT[idx], JetLUT[idx + 1], f);
    }

    Color GetSimpleColor(float t)
    {
        return Color.HSVToRGB(0.67f * (1f - t), 1f, 1f);
    }

    // 公開方法供 VTKFireFlowDemo 調用
    public void UpdateRange(float min, float max)
    {
        minValue = min;
        maxValue = max;
        
        if (isInitialized)
        {
            UpdateColorBar();
        }
    }

    void OnDestroy()
    {
        if (colorBarTexture != null) 
        {
            DestroyImmediate(colorBarTexture);
        }
        
        if (canvasObject != null)
        {
            DestroyImmediate(canvasObject);
        }
    }

    void OnValidate()
    {
        // 在 Inspector 中修改設定時即時更新
        if (isInitialized && Application.isPlaying)
        {
            UpdatePosition();
            UpdateColorBar();
        }
    }

    // 調試方法
    [ContextMenu("強制重新初始化")]
    public void ForceReinitialize()
    {
        Debug.Log("[ForceReinitialize] 開始強制重新初始化...");
        
        // 先清理現有物件
        if (canvasObject != null)
        {
            Debug.Log($"[ForceReinitialize] 銷毀現有 Canvas: {canvasObject.name}");
            if (Application.isPlaying)
                Destroy(canvasObject);
            else
                DestroyImmediate(canvasObject);
            canvasObject = null;
        }
        
        // 清理材質
        if (colorBarTexture != null)
        {
            DestroyImmediate(colorBarTexture);
            colorBarTexture = null;
        }
        
        // 重置狀態
        isInitialized = false;
        labels = null;
        title = null;
        colorBarImage = null;
        
        // 重新初始化
        Initialize();
    }

    [ContextMenu("調試狀態")]
    public void DebugStatus()
    {
        Debug.Log("=== SimpleVRColorBar 狀態 ===");
        Debug.Log($"已初始化: {isInitialized}");
        Debug.Log($"showColorBar: {showColorBar}");
        
        if (canvasObject != null)
        {
            Debug.Log($"Canvas 物件: {canvasObject.name}");
            Debug.Log($"Canvas activeSelf: {canvasObject.activeSelf}");
            Debug.Log($"Canvas activeInHierarchy: {canvasObject.activeInHierarchy}");
            Debug.Log($"Canvas 父物件: {(canvasObject.transform.parent != null ? canvasObject.transform.parent.name : "無")}");
            
            // 檢查父物件是否啟用
            Transform parent = canvasObject.transform.parent;
            while (parent != null)
            {
                Debug.Log($"父物件 {parent.name}: activeSelf={parent.gameObject.activeSelf}");
                parent = parent.parent;
            }
            
            // 檢查 Canvas 組件
            var canvas = canvasObject.GetComponent<Canvas>();
            if (canvas != null)
            {
                Debug.Log($"Canvas enabled: {canvas.enabled}");
                Debug.Log($"Canvas renderMode: {canvas.renderMode}");
            }
        }
        else
        {
            Debug.Log("Canvas 物件: null");
        }
        
        Debug.Log($"攝影機: {(playerCamera != null ? playerCamera.name : "未找到")}");
        Debug.Log($"色條材質: {(colorBarTexture != null ? "存在" : "不存在")}");
        Debug.Log($"標籤數量: {(labels != null ? labels.Length.ToString() : "null")}");
        Debug.Log("========================");
    }

    void OnGUI()
    {
        if (!showDebugOnScreen) return; // 添加開關控制
        
        // 在 Scene 視窗中顯示調試資訊
        if (!Application.isPlaying) return;

        int y = Screen.height - 100;
        int lineHeight = 20;
        
        GUI.color = Color.yellow;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"ColorBar 顯示: {showColorBar}"); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"Canvas: {(canvasObject != null ? "存在" : "不存在")}"); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"按 {toggleKey} 切換顯示"); y += lineHeight;
        GUI.color = Color.white;
    }
}