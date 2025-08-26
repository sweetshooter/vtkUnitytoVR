using UnityEngine;
using UnityEngine.UI;
using UnityEngine.XR;

public class SimpleVRPointer : MonoBehaviour
{
    [Header("VR 指針設定")]
    public XRNode pointerHand = XRNode.RightHand;
    public float rayLength = 5f;
    public Color rayColor = Color.cyan;
    
    [Header("視覺設定")]
    public bool showPointer = true;
    public float pointerWidth = 0.01f;

    // 內部組件
    private InputDevice device;
    private LineRenderer pointerLine;
    private GameObject pointerDot;
    private bool lastClickState = false;
    
    // 互動狀態
    private Button currentButton;

    void Start()
    {
        Initialize();
    }

    void Initialize()
    {
        device = InputDevices.GetDeviceAtXRNode(pointerHand);
        CreatePointerVisuals();
        Debug.Log("[SimpleVRPointer] 初始化完成");
    }

    void CreatePointerVisuals()
    {
        if (!showPointer) return;

        // 指針線條
        GameObject pointerObj = new GameObject("VR_Pointer_Line");
        pointerObj.transform.SetParent(transform);
        
        pointerLine = pointerObj.AddComponent<LineRenderer>();
        Material lineMat = new Material(Shader.Find("Sprites/Default"));
        lineMat.color = rayColor;
        pointerLine.material = lineMat;
        pointerLine.startWidth = pointerWidth;
        pointerLine.endWidth = pointerWidth * 0.1f;
        pointerLine.positionCount = 2;
        pointerLine.useWorldSpace = true;
        
        // 指針點
        pointerDot = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        pointerDot.name = "VR_Pointer_Dot";
        pointerDot.transform.SetParent(transform);
        pointerDot.transform.localScale = Vector3.one * 0.02f;
        
        Renderer dotRenderer = pointerDot.GetComponent<Renderer>();
        Material dotMat = new Material(Shader.Find("Standard"));
        dotMat.color = rayColor;
        dotRenderer.material = dotMat;
        
        Destroy(pointerDot.GetComponent<Collider>());
        
        SetPointerVisibility(false);
    }

    void Update()
    {
        if (!device.isValid)
            device = InputDevices.GetDeviceAtXRNode(pointerHand);

        if (device.isValid)
        {
            UpdatePointer();
            HandleInput();
        }
    }

    void UpdatePointer()
    {
        Vector3 pointerPosition;
        Quaternion pointerRotation;
        
        if (device.TryGetFeatureValue(CommonUsages.devicePosition, out pointerPosition) &&
            device.TryGetFeatureValue(CommonUsages.deviceRotation, out pointerRotation))
        {
            Vector3 rayOrigin = pointerPosition;
            Vector3 rayDirection = pointerRotation * Vector3.forward;
            
            RaycastHit hit;
            bool hitSomething = Physics.Raycast(rayOrigin, rayDirection, out hit, rayLength);
            
            if (showPointer)
            {
                bool menuVisible = IsMenuVisible();
                SetPointerVisibility(menuVisible);
                
                if (menuVisible)
                {
                    Vector3 endPoint = hitSomething ? hit.point : rayOrigin + rayDirection * rayLength;
                    
                    pointerLine.SetPosition(0, rayOrigin);
                    pointerLine.SetPosition(1, endPoint);
                    pointerDot.transform.position = endPoint;
                    
                    CheckButtonInteraction(hit, hitSomething);
                }
            }
        }
    }

    void CheckButtonInteraction(RaycastHit hit, bool hitSomething)
    {
        Button newButton = null;
        
        if (hitSomething)
        {
            newButton = hit.collider.GetComponent<Button>();
            if (newButton == null)
                newButton = hit.collider.GetComponentInParent<Button>();
        }
        
        if (newButton != currentButton)
        {
            currentButton = newButton;
        }
    }

    void HandleInput()
    {
        if (!device.isValid) return;

        bool currentClickState = false;
        if (device.TryGetFeatureValue(CommonUsages.triggerButton, out currentClickState))
        {
            if (currentClickState && !lastClickState)
            {
                OnPointerClick();
            }
            lastClickState = currentClickState;
        }
    }

    void OnPointerClick()
    {
        if (currentButton != null)
        {
            currentButton.onClick.Invoke();
            Debug.Log($"[SimpleVRPointer] 點擊按鈕: {currentButton.name}");
        }
    }

    bool IsMenuVisible()
    {
        SimpleVRMenu menu = FindObjectOfType<SimpleVRMenu>();
        return menu != null && menu.gameObject.activeSelf;
    }

    void SetPointerVisibility(bool visible)
    {
        if (pointerLine != null)
            pointerLine.enabled = visible;
            
        if (pointerDot != null)
            pointerDot.SetActive(visible);
    }

    void OnDestroy()
    {
        if (pointerDot != null)
        {
            if (Application.isPlaying)
                Destroy(pointerDot);
            else
                DestroyImmediate(pointerDot);
        }
    }
}