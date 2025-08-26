using UnityEngine;
using UnityEngine.XR;

public class CameraController : MonoBehaviour
{
    [Header("VR 設定")]
    public Transform vrCameraRig;
    public Camera mainCamera;
    public Transform sparrowObject;
    
    [Header("VR 控制")]
    public XRNode controllerNode = XRNode.LeftHand;
    
    [Header("Debug 設定")]
    [Tooltip("是否在螢幕上顯示 Debug 資訊")]
    public bool showDebugInfo = true;

    [Header("Sparrow 設定")]
    public Vector3 sparrowCameraOffset = new Vector3(0f, 0.2f, -0.5f);
    public float moveSpeed = 5f;
    public float fastMoveSpeed = 10f;
    public float mouseSensitivity = 2f;

    // 內部狀態
    private InputDevice device;
    private bool lastButtonState = false;
    private bool isInSparrowView = false;
    private float rotationX = 0f;
    private float rotationY = 0f;
    
    // 原始狀態
    private Vector3 originalRigPosition;
    private Quaternion originalRigRotation;
    private Vector3 originalSparrowPosition;
    private Quaternion originalSparrowRotation;

    void Start()
    {
        InitializeSystem();
    }

    void InitializeSystem()
    {
        if (mainCamera == null)
            mainCamera = Camera.main ?? FindObjectOfType<Camera>();

        if (vrCameraRig == null)
        {
            string[] rigNames = { "XR Origin", "XR Rig", "XRRig", "[XR Rig]" };
            foreach (string name in rigNames)
            {
                GameObject vrRig = GameObject.Find(name);
                if (vrRig != null)
                {
                    vrCameraRig = vrRig.transform;
                    break;
                }
            }
            
            if (vrCameraRig == null && mainCamera != null && mainCamera.transform.parent != null)
                vrCameraRig = mainCamera.transform.parent;
        }

        if (sparrowObject == null)
        {
            GameObject sparrow = GameObject.Find("sparrow");
            if (sparrow != null)
                sparrowObject = sparrow.transform;
        }

        RecordOriginalState();
        device = InputDevices.GetDeviceAtXRNode(controllerNode);
        Debug.Log("[CameraController] 初始化完成");
    }

    void RecordOriginalState()
    {
        if (vrCameraRig != null)
        {
            originalRigPosition = vrCameraRig.position;
            originalRigRotation = vrCameraRig.rotation;
        }
        
        if (sparrowObject != null)
        {
            originalSparrowPosition = sparrowObject.position;
            originalSparrowRotation = sparrowObject.rotation;
        }
    }

    void Update()
    {
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(controllerNode);

        HandleVRInput();
        HandleKeyboardInput();
        
        if (isInSparrowView)
            HandleSparrowMovement();
    }

    void HandleVRInput()
    {
        if (!device.isValid) return;

        // 左手 X 按鈕（primaryButton）切換視角
        bool currentButtonState = false;
        if (device.TryGetFeatureValue(CommonUsages.primaryButton, out currentButtonState))
        {
            if (currentButtonState && !lastButtonState)
            {
                ToggleView();
                Debug.Log($"[CameraController] 左手 X 按鈕：切換視角");
            }
            lastButtonState = currentButtonState;
        }
    }

    void HandleKeyboardInput()
    {
        // 數字鍵 1：回到原始位置
        if (Input.GetKeyDown(KeyCode.Alpha1))
            ReturnToOriginalView();
            
        // 數字鍵 2：切換到 Sparrow 視角
        if (Input.GetKeyDown(KeyCode.Alpha2))
            SwitchToSparrowView();
    }

    void ToggleView()
    {
        if (isInSparrowView)
            ReturnToOriginalView();
        else
            SwitchToSparrowView();
    }


    void SwitchToSparrowView()
    {
        if (sparrowObject == null || vrCameraRig == null) return;

        Debug.Log("[CameraController] 切換到 Sparrow 視角");
        
        isInSparrowView = true;

        // VR 瞬移到 Sparrow 位置
        Vector3 sparrowPos = sparrowObject.position + sparrowObject.TransformDirection(sparrowCameraOffset);
        vrCameraRig.position = sparrowPos;
        vrCameraRig.rotation = sparrowObject.rotation;

        // 初始化旋轉控制
        Vector3 eulerAngles = vrCameraRig.eulerAngles;
        rotationX = eulerAngles.x;
        rotationY = eulerAngles.y;
    }

    void ReturnToOriginalView()
    {
        if (!isInSparrowView) return;

        Debug.Log("[CameraController] 回到原始視角");
        
        isInSparrowView = false;

        // VR 瞬移回原始位置
        if (vrCameraRig != null)
        {
            vrCameraRig.position = originalRigPosition;
            vrCameraRig.rotation = originalRigRotation;
        }
        
        // Sparrow 也回到原始位置
        if (sparrowObject != null)
        {
            sparrowObject.position = originalSparrowPosition;
            sparrowObject.rotation = originalSparrowRotation;
        }
    }

    void HandleSparrowMovement()
    {
        if (!isInSparrowView || vrCameraRig == null || sparrowObject == null) return;

        // WASD 移動
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;

        // 基於 VR Rig 的方向計算移動
        Vector3 forward = vrCameraRig.forward;
        Vector3 right = vrCameraRig.right;
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 movement = (forward * vertical + right * horizontal) * currentSpeed * Time.deltaTime;

        // Q/E 上下移動
        if (Input.GetKey(KeyCode.Q))
            movement.y -= currentSpeed * Time.deltaTime;
        if (Input.GetKey(KeyCode.E))
            movement.y += currentSpeed * Time.deltaTime;

        // 同時移動 VR Rig 和 Sparrow 物件
        vrCameraRig.position += movement;
        sparrowObject.position += movement;

        // 右鍵滑鼠控制旋轉
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;

            rotationY += mouseX;
            rotationX -= mouseY;
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);

            Quaternion newRotation = Quaternion.Euler(rotationX, rotationY, 0);
            
            // 同時旋轉 VR Rig 和 Sparrow 物件
            vrCameraRig.rotation = newRotation;
            sparrowObject.rotation = newRotation;
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return; // 添加開關控制
        
        int y = 10;
        int lineHeight = 20;
        
        GUI.color = Color.cyan;
        GUI.Label(new Rect(10, y, 400, lineHeight), "VR 視角控制："); y += lineHeight;
        
        string currentView = isInSparrowView ? "Sparrow 視角" : "原始 VR 視角";
        GUI.color = isInSparrowView ? Color.yellow : Color.green;
        GUI.Label(new Rect(10, y, 400, lineHeight), $"當前視角: {currentView}"); y += lineHeight;
        
        GUI.color = Color.white;
        GUI.Label(new Rect(10, y, 400, lineHeight), "左手 X 按鈕：切換視角"); y += lineHeight;
        GUI.Label(new Rect(10, y, 400, lineHeight), "1: 回到原始位置  2: Sparrow 視角"); y += lineHeight;
        
        if (isInSparrowView)
        {
            GUI.color = Color.yellow;
            GUI.Label(new Rect(10, y, 400, lineHeight), "Sparrow 視角控制："); y += lineHeight;
            GUI.Label(new Rect(10, y, 400, lineHeight), "WASD: 移動（VR + Sparrow 同步）"); y += lineHeight;
            GUI.Label(new Rect(10, y, 400, lineHeight), "Q/E: 上下移動"); y += lineHeight;
            GUI.Label(new Rect(10, y, 400, lineHeight), "右鍵+滑鼠: 旋轉（VR + Sparrow 同步）"); y += lineHeight;
            GUI.Label(new Rect(10, y, 400, lineHeight), "Shift: 快速移動"); y += lineHeight;
        }
        
        GUI.color = Color.white;
    }
}