using UnityEngine;

[RequireComponent(typeof(Collider))]
public class SeedDragger : MonoBehaviour
{
    [HideInInspector]
    public VTKFireFlowDemo owner;  // 確保類型正確
    
    [Header("拖曳設定")]
    [Tooltip("拖曳時的視覺回饋")]
    public bool showVisualFeedback = true;
    
    [Tooltip("限制拖曳範圍")]
    public bool constrainToBounds = true;
    public Bounds dragBounds = new Bounds(new Vector3(2.5f, 1.2f, 1.2f), new Vector3(6f, 2.5f, 2.4f));
    
    // 拖曳狀態
    private bool isDragging = false;
    private bool isHovering = false;
    private Vector3 dragOffset;
    private Camera dragCamera;
    private Material originalMaterial;
    private Material hoverMaterial;
    private Material dragMaterial;
    private Renderer sphereRenderer;
    
    // 原始拖曳平面方式
    private Plane dragPlane;
    private bool usePlaneMethod = false;
    
    void Start()
    {
        sphereRenderer = GetComponent<Renderer>();
        if (sphereRenderer != null)
        {
            originalMaterial = sphereRenderer.material;
            
            // 建立滑鼠懸停材質
            hoverMaterial = new Material(originalMaterial);
            hoverMaterial.color = Color.Lerp(originalMaterial.color, Color.white, 0.3f);
            
            // 建立拖曳中材質
            dragMaterial = new Material(originalMaterial);
            dragMaterial.color = Color.Lerp(originalMaterial.color, Color.yellow, 0.5f);
        }
        
        // 確保有 Collider
        var collider = GetComponent<Collider>();
        if (collider == null)
        {
            gameObject.AddComponent<SphereCollider>();
        }
    }
    
    void Update()
    {
        // 檢查滑鼠是否在球上
        CheckMouseHover();
        
        // 處理拖曳
        if (isDragging)
        {
            HandleDragging();
        }
    }
    
    void CheckMouseHover()
    {
        if (isDragging) return;
        
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        bool wasHovering = isHovering;
        isHovering = false;
        
        if (Physics.Raycast(ray, out hit))
        {
            if (hit.collider.gameObject == gameObject)
            {
                isHovering = true;
                
                if (!wasHovering && showVisualFeedback && sphereRenderer != null)
                {
                    sphereRenderer.material = hoverMaterial;
                }
            }
        }
        
        if (wasHovering && !isHovering && showVisualFeedback && sphereRenderer != null)
        {
            sphereRenderer.material = originalMaterial;
        }
    }
    
    void OnMouseDown()
    {
        if (owner == null) return;
        
        // 檢查是否有其他 UI 或控制正在使用滑鼠
        if (Input.GetMouseButton(1)) return; // 右鍵用於攝影機控制
        
        dragCamera = Camera.main;
        if (dragCamera == null) return;
        
        isDragging = true;
        
        // 計算拖曳偏移
        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
        RaycastHit hit;
        
        if (Physics.Raycast(ray, out hit))
        {
            dragOffset = transform.position - hit.point;
        }
        
        // 決定使用哪種拖曳方法
        Vector3 cameraForward = dragCamera.transform.forward;
        float dotY = Mathf.Abs(Vector3.Dot(cameraForward, Vector3.up));
        
        if (dotY > 0.7f)
        {
            // 攝影機接近垂直向下或向上，使用 XZ 平面
            dragPlane = new Plane(Vector3.up, transform.position);
            usePlaneMethod = true;
        }
        else
        {
            // 使用面向攝影機的平面
            dragPlane = new Plane(-cameraForward, transform.position);
            usePlaneMethod = true;
        }
        
        // 視覺回饋
        if (showVisualFeedback && sphereRenderer != null)
        {
            sphereRenderer.material = dragMaterial;
        }
        
        // 通知開始拖曳
        Debug.Log($"[SeedDragger] 開始拖曳，使用平面法: {usePlaneMethod}");
    }
    
    void HandleDragging()
    {
        if (!isDragging || dragCamera == null) return;
        
        Ray ray = dragCamera.ScreenPointToRay(Input.mousePosition);
        Vector3 newPosition = transform.position;
        
        if (usePlaneMethod)
        {
            // 使用平面投射方法
            float distance;
            if (dragPlane.Raycast(ray, out distance))
            {
                newPosition = ray.GetPoint(distance) + dragOffset;
            }
        }
        else
        {
            // 使用射線投射方法
            RaycastHit hit;
            int layerMask = ~LayerMask.GetMask("UI", "Ignore Raycast");
            
            if (Physics.Raycast(ray, out hit, Mathf.Infinity, layerMask))
            {
                newPosition = hit.point + dragOffset;
            }
        }
        
        // 限制在邊界內
        if (constrainToBounds)
        {
            newPosition = new Vector3(
                Mathf.Clamp(newPosition.x, dragBounds.min.x, dragBounds.max.x),
                Mathf.Clamp(newPosition.y, dragBounds.min.y, dragBounds.max.y),
                Mathf.Clamp(newPosition.z, dragBounds.min.z, dragBounds.max.z)
            );
        }
        
        // 更新位置
        transform.localPosition = transform.parent.InverseTransformPoint(newPosition);
        
        // 通知 owner 更新
        if (owner != null)
        {
            owner.SetSeedCenter(transform.localPosition);
        }
    }
    
    void OnMouseUp()
    {
        if (!isDragging) return;
        
        isDragging = false;
        
        // 恢復材質
        if (showVisualFeedback && sphereRenderer != null)
        {
            sphereRenderer.material = isHovering ? hoverMaterial : originalMaterial;
        }
        
        Debug.Log($"[SeedDragger] 結束拖曳，最終位置: {transform.localPosition}");
    }
    
    void OnMouseEnter()
    {
        if (!isDragging && showVisualFeedback && sphereRenderer != null)
        {
            sphereRenderer.material = hoverMaterial;
        }
    }
    
    void OnMouseExit()
    {
        if (!isDragging && showVisualFeedback && sphereRenderer != null)
        {
            sphereRenderer.material = originalMaterial;
        }
    }
    
    void OnDrawGizmosSelected()
    {
        if (constrainToBounds)
        {
            // 顯示拖曳邊界
            Gizmos.color = new Color(1, 1, 0, 0.3f);
            Gizmos.DrawCube(dragBounds.center, dragBounds.size);
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireCube(dragBounds.center, dragBounds.size);
        }
    }
}