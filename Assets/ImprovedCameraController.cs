using UnityEngine;
using System.Collections.Generic;

public class ImprovedCameraController : MonoBehaviour
{
    [Header("移動設定")]
    public float moveSpeed = 10f;
    public float fastMoveSpeed = 20f;
    public float rotationSpeed = 100f;
    public float zoomSpeed = 10f;
    
    [Header("滑鼠設定")]
    public float mouseSensitivity = 2f;
    public bool invertY = false;
    
    [Header("控制設定")]
    [Tooltip("防止與拖曳衝突")]
    public bool disableWhenOverUI = true;
    
    [Header("綁定物件")]
    [Tooltip("要跟隨攝影機移動的物件")]
    public List<Transform> boundObjects = new List<Transform>();
    
    [Header("移動模式")]
    [Tooltip("true: 移動攝影機和綁定物件, false: 只移動攝影機")]
    public bool moveWithBoundObjects = true;
    [Tooltip("切換移動模式的按鍵")]
    public KeyCode toggleMoveMode = KeyCode.M;
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isControlEnabled = true;
    
    // 記錄綁定物件的相對位置
    private Dictionary<Transform, Vector3> relativePositions = new Dictionary<Transform, Vector3>();
    
    void Start()
    {
        // 取得當前旋轉
        Vector3 rotation = transform.eulerAngles;
        rotationX = rotation.x;
        rotationY = rotation.y;
        
        // 記錄綁定物件的相對位置
        RecordRelativePositions();

        moveWithBoundObjects = false;
    }
    
    void RecordRelativePositions()
    {
        relativePositions.Clear();
        foreach (var obj in boundObjects)
        {
            if (obj != null)
            {
                relativePositions[obj] = obj.position - transform.position;
            }
        }
    }
    
    void Update()
    {
        // 切換移動模式
        if (Input.GetKeyDown(toggleMoveMode))
        {
            moveWithBoundObjects = !moveWithBoundObjects;
            Debug.Log($"移動模式: {(moveWithBoundObjects ? "攝影機+綁定物件" : "僅攝影機")}");
            
            if (moveWithBoundObjects)
            {
                RecordRelativePositions();
            }
        }
        
        // 檢查是否有物件被拖曳
        if (disableWhenOverUI && IsMouseOverInteractable())
        {
            isControlEnabled = false;
        }
        else
        {
            isControlEnabled = true;
        }
        
        // 滑鼠右鍵控制視角
        if (Input.GetMouseButton(1) && isControlEnabled)
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            rotationY += mouseX;
            rotationX -= mouseY * (invertY ? -1 : 1);
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // 鍵盤控制移動
        HandleKeyboardMovement();
        
        // 滑鼠滾輪縮放
        if (isControlEnabled)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                Vector3 movement = transform.forward * scroll * zoomSpeed;
                MoveCamera(movement);
            }
        }
        
        // 快速切換視角
        HandleQuickViews();
    }
    
    void HandleKeyboardMovement()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        movement = transform.TransformDirection(movement);
        movement *= currentSpeed * Time.deltaTime;
        
        if (Input.GetKey(KeyCode.Q))
        {
            movement.y -= currentSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            movement.y += currentSpeed * Time.deltaTime;
        }
        
        if (movement != Vector3.zero)
        {
            MoveCamera(movement);
        }
    }
    
    void MoveCamera(Vector3 movement)
    {
        transform.position += movement;
        
        // 移動綁定的物件
        if (moveWithBoundObjects)
        {
            foreach (var obj in boundObjects)
            {
                if (obj != null)
                {
                    obj.position += movement;
                }
            }
        }
    }
    
    void HandleQuickViews()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1))
        {
            SetViewPosition(new Vector3(5f, 1f, -15f), Quaternion.Euler(0, 0, 0));
            rotationX = 90;
            rotationY = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2))
        {
            SetViewPosition(new Vector3(10f, 2f, 1.2f), Quaternion.Euler(0, -90, 0));
            rotationX = 0;
            rotationY = -90;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            SetViewPosition(new Vector3(6f, 6f, 6f), Quaternion.identity);
            transform.LookAt(new Vector3(2.5f, 1f, 1.2f));
            Vector3 rotation = transform.eulerAngles;
            rotationX = rotation.x;
            rotationY = rotation.y;
        }
    }
    
    void SetViewPosition(Vector3 newPosition, Quaternion newRotation)
    {
        Vector3 delta = newPosition - transform.position;
        transform.position = newPosition;
        transform.rotation = newRotation;
        
        if (moveWithBoundObjects)
        {
            foreach (var obj in boundObjects)
            {
                if (obj != null)
                {
                    obj.position += delta;
                }
            }
        }
    }
    
    bool IsMouseOverInteractable()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.GetComponent<SeedDragger>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnGUI()
    {
        if (!Input.GetMouseButton(1))
        {
            int y = 10;
            int lineHeight = 20;
            
            GUI.Label(new Rect(10, y, 300, lineHeight), "攝影機控制："); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "右鍵 + 滑鼠：旋轉視角"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "WASD：前後左右移動"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "Q/E：上下移動"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "Shift：加速移動"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "滾輪：前後移動"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), "1/2/3：快速切換視角"); y += lineHeight;
            GUI.Label(new Rect(10, y, 300, lineHeight), $"M：切換移動模式 (當前: {(moveWithBoundObjects ? "全部" : "僅攝影機")})"); y += lineHeight;
            
            if (isControlEnabled == false)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, y, 300, lineHeight), "滑鼠在可互動物件上"); y += lineHeight;
                GUI.color = Color.white;
            }
        }
    }
    
    // 公開方法：添加綁定物件
    public void AddBoundObject(Transform obj)
    {
        if (!boundObjects.Contains(obj))
        {
            boundObjects.Add(obj);
            if (obj != null)
            {
                relativePositions[obj] = obj.position - transform.position;
            }
        }
    }
    
    public void RemoveBoundObject(Transform obj)
    {
        boundObjects.Remove(obj);
        relativePositions.Remove(obj);
    }
}