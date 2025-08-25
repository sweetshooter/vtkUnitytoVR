using UnityEngine;

[RequireComponent(typeof(CharacterController))]
public class PersonViewController : MonoBehaviour
{
    [Header("References")]
    public Camera mainCamera;             // 拖主攝影機（空則用 Camera.main）
    public Behaviour freeCamController;   // 拖主攝影機上的 ImprovedCameraController

    [Header("Switch")]
    public KeyCode toggleKey = KeyCode.V;

    [Header("First-Person Movement")]
    public float moveSpeed = 4.5f;
    public float sprintMultiplier = 1.6f;
    public float mouseSensitivity = 2.0f;
    public Vector3 cameraLocalOffset = new Vector3(0f, 1.7f, 0f); // 眼睛高度

    [Header("First-Person Gravity")]
    public bool lockYInFirstPerson = true; // 不受重力
    private float firstPersonY;

    [Header("First-Person Rotation Offset")]
    public float yawOffset = 90f; // 左轉 90°；改 -90 就是右轉

    // 內部
    private CharacterController cc;
    private float yaw;   // 水平角
    private float pitch; // 垂直角
    private Vector3 velocity;

    private enum ViewMode { Overview, FirstPerson }
    private ViewMode mode = ViewMode.Overview;

    // 俯瞰快照（每次切入第一人稱前更新）
    private Vector3 overviewPos;
    private Quaternion overviewRot;
    private float overviewFOV;

    void Awake()
    {
        cc = GetComponent<CharacterController>();
        if (!mainCamera) mainCamera = Camera.main;

        // 若沒手動指定自由鏡頭控制器，嘗試在主攝影機上找類名為 ImprovedCameraController 的元件
        if (!freeCamController && mainCamera)
        {
            foreach (var b in mainCamera.GetComponents<Behaviour>())
            {
                if (b && b.GetType().Name == "ImprovedCameraController")
                {
                    freeCamController = b;
                    break;
                }
            }
        }
    }

    void Start()
    {
        SetOverview(true);
    }

    void Update()
    {
        // 切換鍵
        if (Input.GetKeyDown(toggleKey))
        {
            if (mode == ViewMode.Overview)
            {
                CaptureOverviewSnapshot();  // 先記住目前自由鏡頭
                SetFirstPerson();
            }
            else
            {
                SetOverview(true);
            }
        }

        if (mode == ViewMode.FirstPerson)
        {
            UpdateFirstPersonInput();
            UpdateFirstPersonMove();
            UpdateFirstPersonCamera();
        }
    }

    // ========== 模式切換 ==========
    void SetOverview(bool snapToSnapshot)
    {
        mode = ViewMode.Overview;

        if (cc) cc.enabled = false;                  // 停止人物控制
        if (freeCamController) freeCamController.enabled = true; // 開啟自由鏡頭

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (snapToSnapshot && mainCamera && overviewFOV > 0f)
        {
            mainCamera.transform.position = overviewPos;
            mainCamera.transform.rotation = overviewRot;
            mainCamera.fieldOfView = overviewFOV;
        }
    }

    void SetFirstPerson()
    {
        mode = ViewMode.FirstPerson;

        if (freeCamController) freeCamController.enabled = false; // 關自由鏡頭
        if (cc && !cc.gameObject.activeInHierarchy) cc.gameObject.SetActive(true);
        if (cc) cc.enabled = true;

        // 初始角度：以「當前相機的水平角」為基準，再加 yawOffset
        float basisYaw = (mainCamera ? mainCamera.transform.eulerAngles.y : transform.eulerAngles.y);
        yaw   = basisYaw + yawOffset; // ★ 左轉 90°
        pitch = 0f;                   // 水平看（需要仰/俯就在 Inspector 改）

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (lockYInFirstPerson) firstPersonY = transform.position.y;

        UpdateFirstPersonCamera(true); // 立刻把相機放到頭部
    }

    void CaptureOverviewSnapshot()
    {
        if (!mainCamera) return;
        overviewPos = mainCamera.transform.position;
        overviewRot = mainCamera.transform.rotation;
        overviewFOV = mainCamera.fieldOfView;
    }

    // ========== 第一人稱：輸入 / 移動 / 相機 ==========
    void UpdateFirstPersonInput()
    {
        float mx = Input.GetAxisRaw("Mouse X") * mouseSensitivity;
        float my = Input.GetAxisRaw("Mouse Y") * mouseSensitivity;

        yaw   += mx;
        pitch -= my;
        pitch = Mathf.Clamp(pitch, -89f, 89f);
    }

    void UpdateFirstPersonMove()
    {
        if (!cc || !cc.enabled || !cc.gameObject.activeInHierarchy) return;

        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");
        Vector3 inputDir = Vector3.ClampMagnitude(new Vector3(h, 0f, v), 1f);

        // 以 yaw 旋到世界空間
        Quaternion yawRot = Quaternion.Euler(0f, yaw, 0f);
        Vector3 moveDir = yawRot * inputDir;

        float speed = moveSpeed * (Input.GetKey(KeyCode.LeftShift) ? sprintMultiplier : 1f);
        Vector3 horizontal = moveDir * speed;

        // 第一人稱預設不受重力
        float vy = 0f;
        if (!lockYInFirstPerson)
        {
            if (cc.isGrounded)
            {
                velocity.y = -0.5f;
                if (Input.GetButtonDown("Jump")) velocity.y = 5f;
            }
            else
            {
                velocity.y -= 9.81f * Time.deltaTime;
            }
            vy = velocity.y;
        }

        cc.Move(new Vector3(horizontal.x, vy, horizontal.z) * Time.deltaTime);

        if (lockYInFirstPerson)
        {
            var p = transform.position;
            transform.position = new Vector3(p.x, firstPersonY, p.z); // 鎖定高度
        }
    }

    void UpdateFirstPersonCamera(bool instant = false)
    {
        if (!mainCamera) return;

        Vector3 eyeWorld = transform.TransformPoint(cameraLocalOffset);
        mainCamera.transform.position = eyeWorld; // 需要平滑再改成 Lerp

        mainCamera.transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
    }
}
