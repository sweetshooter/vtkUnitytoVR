using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class SimpleVRMenu : MonoBehaviour
{
    [Header("Menu 設定")]
    public Vector3 menuLocalPosition = new Vector3(0f, 0.5f, 1.5f);
    public float menuScale = 0.001f;
    public bool autoFacePlayer = true;
    
    [Header("VR 控制")]
    public XRNode controllerNode = XRNode.RightHand;
    
    [Header("Debug 設定")]
    [Tooltip("是否在螢幕上顯示 Debug 資訊")]
    public bool showDebugInfo = true;

    [Header("外部組件參考")]
    public CameraController cameraController;
    public SimpleVRColorBar colorBar;
    public VTKFireFlowDemo vtkDemo;

    // 內部組件
    private GameObject menuCanvas;
    private Camera playerCamera;
    private InputDevice device;
    private bool lastButtonState = false;
    private bool isMenuVisible = false;
    
    // UI 元素
    private Button[] menuButtons;
    private Text titleText;
    private Text statusText;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        playerCamera = Camera.main ?? FindObjectOfType<Camera>();
        
        // 自動找到組件
        if (cameraController == null)
            cameraController = FindObjectOfType<CameraController>();
            
        if (colorBar == null)
            colorBar = FindObjectOfType<SimpleVRColorBar>();
            
        if (vtkDemo == null)
            vtkDemo = FindObjectOfType<VTKFireFlowDemo>();

        device = InputDevices.GetDeviceAtXRNode(controllerNode);
        
        CreateMenuCanvas();
        CreateMenuUI();
        SetMenuVisibility(false);
        
        Debug.Log("[SimpleVRMenu] 初始化完成");
    }

    void CreateMenuCanvas()
    {
        menuCanvas = new GameObject("VR_Menu_Canvas");
        menuCanvas.transform.SetParent(playerCamera.transform, false);
        menuCanvas.transform.localPosition = menuLocalPosition;
        menuCanvas.transform.localScale = Vector3.one * menuScale;
        
        Canvas canvas = menuCanvas.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.WorldSpace;
        
        RectTransform canvasRect = menuCanvas.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(800, 600);
        
        menuCanvas.AddComponent<GraphicRaycaster>();
    }

    void CreateMenuUI()
    {
        // 背景
        GameObject bgPanel = new GameObject("Background");
        bgPanel.transform.SetParent(menuCanvas.transform, false);
        
        Image bgImage = bgPanel.AddComponent<Image>();
        bgImage.color = new Color(0.1f, 0.1f, 0.2f, 0.9f);
        
        RectTransform bgRect = bgPanel.GetComponent<RectTransform>();
        bgRect.anchorMin = Vector2.zero;
        bgRect.anchorMax = Vector2.one;
        bgRect.offsetMin = Vector2.zero;
        bgRect.offsetMax = Vector2.zero;

        // 標題
        CreateTitle(bgPanel);
        
        // 按鈕
        CreateButtons(bgPanel);
        
        // 狀態文字
        CreateStatus(bgPanel);
    }

    void CreateTitle(GameObject parent)
    {
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(parent.transform, false);
        
        titleText = titleObj.AddComponent<Text>();
        titleText.text = "VR 控制面板";
        titleText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        titleText.fontSize = 36;
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontStyle = FontStyle.Bold;
        
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0f, 0.85f);
        titleRect.anchorMax = new Vector2(1f, 1f);
        titleRect.offsetMin = Vector2.zero;
        titleRect.offsetMax = Vector2.zero;
    }

    void CreateButtons(GameObject parent)
    {
        string[] buttonTexts = {
            "切換視角",
            "切換 ColorBar", 
            "重新生成流線",
            "清理流線",
            "關閉 Menu"
        };

        menuButtons = new Button[buttonTexts.Length];
        
        for (int i = 0; i < buttonTexts.Length; i++)
        {
            menuButtons[i] = CreateSingleButton(parent, buttonTexts[i], i);
        }
        
        SetupButtonFunctions();
    }

    Button CreateSingleButton(GameObject parent, string text, int index)
    {
        GameObject buttonObj = new GameObject($"Button_{text}");
        buttonObj.transform.SetParent(parent.transform, false);
        
        // 按鈕背景
        Image buttonBg = buttonObj.AddComponent<Image>();
        buttonBg.color = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        
        // 按鈕組件
        Button button = buttonObj.AddComponent<Button>();
        
        // 按鈕位置
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        float buttonHeight = 80f;
        float spacing = 10f;
        float startY = 250f;
        
        buttonRect.anchorMin = new Vector2(0.1f, 0.5f);
        buttonRect.anchorMax = new Vector2(0.9f, 0.5f);
        buttonRect.pivot = new Vector2(0.5f, 0.5f);
        buttonRect.sizeDelta = new Vector2(-100, buttonHeight);
        buttonRect.anchoredPosition = new Vector2(0, startY - index * (buttonHeight + spacing));
        
        // 按鈕文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.text = text;
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform textRect = buttonText.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;
        
        // 按鈕顏色
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.2f, 0.4f, 0.8f, 0.8f);
        colors.highlightedColor = new Color(0.3f, 0.6f, 1f, 1f);
        colors.pressedColor = new Color(0.1f, 0.3f, 0.6f, 1f);
        button.colors = colors;
        
        return button;
    }

    void SetupButtonFunctions()
    {
        // 按鈕 0: 切換視角
        menuButtons[0].onClick.AddListener(() => {
            if (cameraController != null)
            {
                // 切換視角
                if (cameraController.GetComponent<CameraController>())
                {
                    // 觸發視角切換
                    cameraController.SendMessage("ToggleView", SendMessageOptions.DontRequireReceiver);
                }
                UpdateStatusText("切換視角");
            }
        });
        
        // 按鈕 1: 切換 ColorBar
        menuButtons[1].onClick.AddListener(() => {
            if (colorBar != null)
            {
                colorBar.showColorBar = !colorBar.showColorBar;
                colorBar.UpdateVisibility();
                UpdateStatusText($"ColorBar: {(colorBar.showColorBar ? "顯示" : "隱藏")}");
            }
        });
        
        // 按鈕 2: 重新生成流線
        menuButtons[2].onClick.AddListener(() => {
            if (vtkDemo != null)
            {
                vtkDemo.RegenerateStreamlines();
                UpdateStatusText("重新生成流線");
            }
        });
        
        // 按鈕 3: 清理流線
        menuButtons[3].onClick.AddListener(() => {
            if (vtkDemo != null)
            {
                vtkDemo.ClearAllStreamlinesPublic();
                UpdateStatusText("清理流線完成");
            }
        });
        
        // 按鈕 4: 關閉 Menu
        menuButtons[4].onClick.AddListener(() => {
            SetMenuVisibility(false);
            UpdateStatusText("Menu 已關閉");
        });
    }

    void CreateStatus(GameObject parent)
    {
        GameObject statusObj = new GameObject("Status");
        statusObj.transform.SetParent(parent.transform, false);
        
        statusText = statusObj.AddComponent<Text>();
        statusText.text = "VR Menu 已就緒";
        statusText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        statusText.fontSize = 20;
        statusText.color = Color.cyan;
        statusText.alignment = TextAnchor.MiddleCenter;
        
        RectTransform statusRect = statusText.GetComponent<RectTransform>();
        statusRect.anchorMin = new Vector2(0f, 0f);
        statusRect.anchorMax = new Vector2(1f, 0.25f);
        statusRect.offsetMin = Vector2.zero;
        statusRect.offsetMax = Vector2.zero;
    }

    void Update()
    {
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(controllerNode);

        HandleMenuToggle();
        
        if (autoFacePlayer && isMenuVisible)
        {
            UpdateMenuOrientation();
        }
    }

    void HandleMenuToggle()
    {
        if (!device.isValid) return;

        bool currentButtonState = false;
        if (device.TryGetFeatureValue(CommonUsages.secondaryButton, out currentButtonState))
        {
            if (currentButtonState && !lastButtonState)
            {
                ToggleMenu();
                Debug.Log($"[SimpleVRMenu] {controllerNode} B/Y 按鈕：切換 Menu");
            }
            lastButtonState = currentButtonState;
        }
        
        // 鍵盤快捷鍵
        if (Input.GetKeyDown(KeyCode.M))
        {
            ToggleMenu();
        }
    }

    void ToggleMenu()
    {
        SetMenuVisibility(!isMenuVisible);
    }

    void SetMenuVisibility(bool visible)
    {
        isMenuVisible = visible;
        if (menuCanvas != null)
        {
            menuCanvas.SetActive(visible);
        }
        
        Debug.Log($"[SimpleVRMenu] Menu {(visible ? "顯示" : "隱藏")}");
    }

    void UpdateMenuOrientation()
    {
        if (menuCanvas != null && playerCamera != null)
        {
            Vector3 direction = playerCamera.transform.position - menuCanvas.transform.position;
            direction.y = 0;
            if (direction.magnitude > 0.1f)
            {
                // 方法 1: 使用反向來修正左右顛倒
                menuCanvas.transform.rotation = Quaternion.LookRotation(-direction);
                
                // 或者可以使用方法 2: 
                // Quaternion lookRotation = Quaternion.LookRotation(direction);
                // menuCanvas.transform.rotation = lookRotation * Quaternion.Euler(0, 180, 0);
            }
        }
    }

    void UpdateStatusText(string message)
    {
        if (statusText != null)
        {
            statusText.text = $"執行: {message}";
            Invoke(nameof(RestoreStatusText), 2f);
        }
    }

    void RestoreStatusText()
    {
        if (statusText != null)
        {
            statusText.text = "VR Menu 已就緒 - 按 B/Y 或 M 鍵開關";
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return; // 添加開關控制
        
        int y = Screen.height - 100;
        GUI.color = Color.green;
        GUI.Label(new Rect(10, y, 300, 20), $"VR Menu: 按 {controllerNode} B/Y 或 M 鍵");
        GUI.Label(new Rect(10, y + 20, 300, 20), $"Menu 狀態: {(isMenuVisible ? "顯示" : "隱藏")}");
        GUI.color = Color.white;
    }

    void OnDestroy()
    {
        if (menuCanvas != null)
        {
            if (Application.isPlaying)
                Destroy(menuCanvas);
            else
                DestroyImmediate(menuCanvas);
        }
    }
}