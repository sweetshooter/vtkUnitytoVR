using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class VTKColorBar : MonoBehaviour
{
    [Header("外觀")]
    public int barWidth = 20;
    public int barHeight = 200;
    public int margin = 20;               // 與螢幕左下角的邊距
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

    [Header("Canvas 設定")]
    public bool useOwnCanvas = false;     // 若為 true，使用下方 targetCanvas
    public Canvas targetCanvas = null;    // 指定既有 Canvas
    public Vector2 referenceResolution = new Vector2(1920, 1080);
    [Range(0,1)] public float matchWidthOrHeight = 1f;

    [Header("顯示控制")]
    public bool showColorBar = true;
    public KeyCode toggleKey = KeyCode.B;

    // 內部
    private static GameObject s_canvasObj;    // 防重複
    private GameObject canvasObject;
    private RectTransform bgRect;
    private RawImage colorBarImage;
    private Text[] labels;
    private Texture2D colorBarTexture;
    private Text title;

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

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    static void ResetStatic() { s_canvasObj = null; }

    private bool _built = false;

    void Awake()
    {
        BuildIfNeeded();
    }



    void Start()
    {
        CreateOrAttachCanvas();
        CreateColorBarUI();
        UpdateColorBar();
        SetVisibility(showColorBar);
    }

    void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            showColorBar = !showColorBar;
            SetVisibility(showColorBar);
        }
    }

    void SetVisibility(bool v)
    {
        if (canvasObject != null) canvasObject.SetActive(v);
    }

    void CreateOrAttachCanvas()
    {
        if (useOwnCanvas && targetCanvas != null)
        {
            canvasObject = targetCanvas.gameObject;
            return;
        }

        // 若已存在就複用
        if (s_canvasObj == null)
            s_canvasObj = GameObject.Find("ColorBarCanvas");

        if (s_canvasObj == null)
        {
            s_canvasObj = new GameObject("ColorBarCanvas");
            var canvas = s_canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;         // ★ Overlay：不受 Z/相機影響
            canvas.sortingOrder = 100;

            var scaler = s_canvasObj.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = referenceResolution;
            scaler.matchWidthOrHeight = matchWidthOrHeight;

            s_canvasObj.AddComponent<GraphicRaycaster>();
        }

        canvasObject = s_canvasObj;
    }

    void CreateColorBarUI()
    {
        // 背景框（父）
        var existing = canvasObject.transform.Find("Background");
        GameObject bgObject;
        if (existing == null)
        {
            bgObject = new GameObject("Background");
            bgObject.transform.SetParent(canvasObject.transform, false);
            var bgImg = bgObject.AddComponent<Image>();
            bgImg.color = new Color(0, 0, 0, 0.5f);
        }
        else bgObject = existing.gameObject;

        bgRect = bgObject.GetComponent<RectTransform>();
        // ★ 左下角對齊，固定在螢幕左下角
        bgRect.anchorMin = new Vector2(0f, 0f);
        bgRect.anchorMax = new Vector2(0f, 0f);
        bgRect.pivot     = new Vector2(0f, 0f);

        float frameW = barWidth + 2*margin + 60f;  // 標籤區寬度
        float frameH = barHeight + 2*margin + 20f; // 標題高度
        bgRect.sizeDelta = new Vector2(frameW, frameH);
        bgRect.anchoredPosition = new Vector2(margin, margin); // ★ 左下角距離

        // 色條影像
        var barTr = bgObject.transform.Find("ColorBar") as RectTransform;
        if (barTr == null)
        {
            var barObj = new GameObject("ColorBar");
            barObj.transform.SetParent(bgObject.transform, false);
            colorBarImage = barObj.AddComponent<RawImage>();
            barTr = colorBarImage.rectTransform;
        }
        else colorBarImage = barTr.GetComponent<RawImage>();

        barTr.anchorMin = new Vector2(0f, 0f);
        barTr.anchorMax = new Vector2(0f, 0f);
        barTr.pivot     = new Vector2(0f, 0f);
        barTr.anchoredPosition = new Vector2(margin, margin);
        barTr.sizeDelta = new Vector2(barWidth, barHeight);

        // 標籤
        if (labels == null || labels.Length != labelCount)
            labels = new Text[labelCount];

        for (int i = 0; i < labelCount; i++)
        {
            Text label = labels[i];
            if (label == null)
            {
                var labelObj = new GameObject($"Label_{i}");
                labelObj.transform.SetParent(bgObject.transform, false);
                label = labelObj.AddComponent<Text>();
                label.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
                label.fontSize = 12;
                label.color = Color.white;
                label.alignment = TextAnchor.MiddleLeft;
                labels[i] = label;
            }

            var rt = label.GetComponent<RectTransform>();
            rt.anchorMin = new Vector2(0f, 0f);
            rt.anchorMax = new Vector2(0f, 0f);
            rt.pivot     = new Vector2(0f, 0.5f);

            float t = (labelCount == 1) ? 0f : i / (float)(labelCount - 1);
            float y = margin + t * barHeight;
            rt.anchoredPosition = new Vector2(margin + barWidth + 15f, y);
            rt.sizeDelta = new Vector2(80, 20);
        }

        // 標題
        var titleTr = bgObject.transform.Find("Title") as RectTransform;
        if (titleTr == null)
        {
            var titleObj = new GameObject("Title");
            titleObj.transform.SetParent(bgObject.transform, false);
            title = titleObj.AddComponent<Text>();
            title.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            title.fontSize = 14;
            title.color = Color.white;
            title.alignment = TextAnchor.MiddleCenter;
            titleTr = title.GetComponent<RectTransform>();
        }
        else title = titleTr.GetComponent<Text>();

        title.text = titleText;
        titleTr.anchorMin = new Vector2(0.5f, 1f);
        titleTr.anchorMax = new Vector2(0.5f, 1f);
        titleTr.pivot     = new Vector2(0.5f, 1f);
        titleTr.anchoredPosition = new Vector2(0f, -5f);
        titleTr.sizeDelta = new Vector2(frameW - 10f, 20f);
    }

    void UpdateColorBar()
    {

        BuildIfNeeded();

        if (colorBarTexture == null)
        {
            colorBarTexture = new Texture2D(1, 256, TextureFormat.RGBA32, false);
            colorBarTexture.wrapMode = TextureWrapMode.Clamp;
            colorBarTexture.filterMode = FilterMode.Bilinear;
        }

        for (int i = 0; i < 256; i++)
        {
            float t = i / 255f;
            if (invertColors) t = 1f - t;
            Color c = useJetColorMap ? GetJetColor(t) : GetSimpleColor(t);
            colorBarTexture.SetPixel(0, i, c);
        }
        colorBarTexture.Apply();
        colorBarImage.texture = colorBarTexture;

        for (int i = 0; i < labelCount; i++)
        {
            float t = (labelCount == 1) ? 0f : i / (float)(labelCount - 1);
            float v = Mathf.Lerp(minValue, maxValue, t);
            labels[i].text = v.ToString(valueFormat) + unit;
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

    Color GetSimpleColor(float t) => Color.HSVToRGB(0.67f * (1f - t), 1f, 1f);

    void BuildIfNeeded()
    {
        if (_built) return;
        CreateOrAttachCanvas();
        CreateColorBarUI();
        _built = true;
    }

    public void UpdateRange(float min, float max)
    {
        minValue = min; maxValue = max;
        BuildIfNeeded();        // ★ 確保已建好 UI
        UpdateColorBar();
    }

    void OnDestroy()
    {
        if (colorBarTexture != null) DestroyImmediate(colorBarTexture);
        // 不主動銷毀 Canvas，避免其他元件也在用；若要刪，手動刪 ColorBarCanvas
    }
}
