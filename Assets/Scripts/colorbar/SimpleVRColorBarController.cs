using UnityEngine;
using UnityEngine.XR;

public class SimpleVRColorBarController : MonoBehaviour
{
    [Header("ColorBar 參考")]
    public SimpleVRColorBar colorBar;
    
    [Header("控制設定")]
    [Tooltip("用於切換 ColorBar 顯示的 VR 按鈕")]
    public InputFeatureUsage<bool> toggleButton = CommonUsages.primaryButton; // A/X 按鈕
    
    [Tooltip("使用哪隻手的控制器")]
    public XRNode controllerNode = XRNode.RightHand;
    
    [Tooltip("鍵盤快捷鍵")]
    public KeyCode keyboardToggle = KeyCode.B;

    [Header("Debug 設定")]
    [Tooltip("是否在螢幕上顯示 Debug 資訊")]
    public bool showDebugInfo = false;

    // 內部狀態
    private InputDevice device;
    private bool lastButtonState = false;

    void Start()
    {
        // 自動找到 ColorBar 如果沒指定
        if (colorBar == null)
        {
            colorBar = FindObjectOfType<SimpleVRColorBar>();
        }

        if (colorBar == null)
        {
            Debug.LogError("[SimpleVRColorBarController] 找不到 SimpleVRColorBar 組件！");
        }

        InitializeDevice();
    }

    void InitializeDevice()
    {
        device = InputDevices.GetDeviceAtXRNode(controllerNode);
    }

    void Update()
    {
        if (colorBar == null) return;

        // 重新初始化設備如果無效
        if (!device.isValid)
        {
            InitializeDevice();
        }

        // 處理 VR 按鈕輸入
        HandleVRInput();
        
        // 處理鍵盤輸入
        HandleKeyboardInput();
    }

    void HandleVRInput()
    {
        if (!device.isValid) return;

        bool currentButtonState = false;
        if (device.TryGetFeatureValue(toggleButton, out currentButtonState))
        {
            // 檢測按鈕按下（邊緣觸發）
            if (currentButtonState && !lastButtonState)
            {
                ToggleColorBar();
                Debug.Log($"[VRController] {controllerNode} 按鈕：切換 ColorBar");
            }
            lastButtonState = currentButtonState;
        }
    }

    void HandleKeyboardInput()
    {
        if (Input.GetKeyDown(keyboardToggle))
        {
            ToggleColorBar();
            Debug.Log($"[VRController] {keyboardToggle} 鍵：切換 ColorBar");
        }
    }

    void ToggleColorBar()
    {
        if (colorBar != null)
        {
            colorBar.showColorBar = !colorBar.showColorBar;
            colorBar.UpdateVisibility();
            Debug.Log($"[VRController] ColorBar 切換為: {colorBar.showColorBar}");
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return; // 添加開關控制
        
        // 簡單的控制提示
        if (colorBar != null)
        {
            int y = Screen.height - 80;
            int lineHeight = 20;
            
            GUI.color = Color.white;
            GUI.Label(new Rect(10, y, 300, lineHeight), "ColorBar 控制："); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), $"{controllerNode} {GetButtonName()} 或 {keyboardToggle}：切換顯示"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), $"ColorBar 狀態: {(colorBar.showColorBar ? "顯示" : "隱藏")}");
            
            GUI.color = Color.white;
        }
    }

    string GetButtonName()
    {
        if (toggleButton.Equals(CommonUsages.primaryButton))
            return "主按鈕(A/X)";
        else if (toggleButton.Equals(CommonUsages.secondaryButton))
            return "副按鈕(B/Y)";
        else if (toggleButton.Equals(CommonUsages.gripButton))
            return "握把";
        else if (toggleButton.Equals(CommonUsages.triggerButton))
            return "扳機";
        else
            return "按鈕";
    }
}