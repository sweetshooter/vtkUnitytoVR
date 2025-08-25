using UnityEngine;

public class SimpleCameraController : MonoBehaviour
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
    
    private float rotationX = 0f;
    private float rotationY = 0f;
    private bool isControlEnabled = true;
    
    void Start()
    {
        // 取得當前旋轉
        Vector3 rotation = transform.eulerAngles;
        rotationX = rotation.x;
        rotationY = rotation.y;
    }
    
    void Update()
    {
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
        if (Input.GetMouseButton(1) && isControlEnabled) // 右鍵按住
        {
            // 鎖定游標
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            
            // 滑鼠控制旋轉
            float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity;
            float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity;
            
            rotationY += mouseX;
            rotationX -= mouseY * (invertY ? -1 : 1);
            rotationX = Mathf.Clamp(rotationX, -90f, 90f);
            
            transform.rotation = Quaternion.Euler(rotationX, rotationY, 0);
        }
        else
        {
            // 釋放游標
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        
        // 鍵盤控制移動（不受滑鼠影響）
        HandleKeyboardMovement();
        
        // 滑鼠滾輪縮放（檢查是否在可互動物件上）
        if (isControlEnabled)
        {
            float scroll = Input.GetAxis("Mouse ScrollWheel");
            if (scroll != 0)
            {
                transform.position += transform.forward * scroll * zoomSpeed;
            }
        }
        
        // 快速切換視角
        HandleQuickViews();
    }
    
    void HandleKeyboardMovement()
    {
        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? fastMoveSpeed : moveSpeed;
        
        // WASD 移動
        float horizontal = Input.GetAxis("Horizontal"); // A/D
        float vertical = Input.GetAxis("Vertical");     // W/S
        
        Vector3 movement = new Vector3(horizontal, 0, vertical);
        movement = transform.TransformDirection(movement);
        movement *= currentSpeed * Time.deltaTime;
        
        // Q/E 上下移動
        if (Input.GetKey(KeyCode.Q))
        {
            movement.y -= currentSpeed * Time.deltaTime;
        }
        if (Input.GetKey(KeyCode.E))
        {
            movement.y += currentSpeed * Time.deltaTime;
        }
        
        transform.position += movement;
    }
    
    void HandleQuickViews()
    {
        if (Input.GetKeyDown(KeyCode.Alpha1)) // 數字鍵 1
        {
            // 俯視圖
            transform.position = new Vector3(2.5f, 10f, 1.2f);
            transform.rotation = Quaternion.Euler(90, 0, 0);
            rotationX = 90;
            rotationY = 0;
        }
        if (Input.GetKeyDown(KeyCode.Alpha2)) // 數字鍵 2
        {
            // 側視圖
            transform.position = new Vector3(10f, 2f, 1.2f);
            transform.rotation = Quaternion.Euler(0, -90, 0);
            rotationX = 0;
            rotationY = -90;
        }
        if (Input.GetKeyDown(KeyCode.Alpha3)) // 數字鍵 3
        {
            // 斜視圖
            transform.position = new Vector3(6f, 6f, 6f);
            transform.LookAt(new Vector3(2.5f, 1f, 1.2f));
            Vector3 rotation = transform.eulerAngles;
            rotationX = rotation.x;
            rotationY = rotation.y;
        }
    }
    
    bool IsMouseOverInteractable()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            // 檢查是否是可拖曳的物件
            if (hit.collider.GetComponent<SeedDragger>() != null)
            {
                return true;
            }
        }
        
        return false;
    }
    
    void OnGUI()
    {
        // 顯示控制提示
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
            
            if (isControlEnabled == false)
            {
                GUI.color = Color.yellow;
                GUI.Label(new Rect(10, y, 300, lineHeight), "滑鼠在可互動物件上"); y += lineHeight;
                GUI.color = Color.white;
            }
        }
    }
}