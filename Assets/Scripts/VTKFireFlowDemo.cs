using UnityEngine;
using System.Collections.Generic;
using Kitware.VTK;

[ExecuteAlways]
public class VTKFireFlowDemo : MonoBehaviour
{
    [Header("—— 檔案設定 ——")]
    public string vtuFileName = "fire_ug.vtu";
    [Tooltip("是否反轉 Z 座標")]
    public bool flipZ = true;
    [Tooltip("Z 座標反轉基準值")]
    public float zFlipValue = 2.4f;
    
    [Header("—— 種子點設定 ——")]
    public Vector3 seedCenter = new Vector3(3.0f, 1.6f, 1.25f);
    public float seedRadius = 0.2f;
    public int numSeedPoints = 50;
    
    [Header("—— StreamTracer 參數 ——")]
    public float maxPropagation = 50f;
    public float initialStep = 0.2f;
    public float minStep = 0.01f;
    
    [Header("—— 流線視覺設定 ——")]
    public float lineWidth = 0.04f;
    public bool useVTKColorMap = true;
    [Tooltip("留空使用 active scalar，或指定欄位名稱如 't' 或 'velocity'")]
    public string scalarFieldName = "";
    [Tooltip("反轉顏色映射（冷熱對調）")]
    public bool invertColorMap = true;
    
    [Header("—— 種子球設定 ——")]
    public Color sphereColor = new Color(1f, 0.41f, 0.71f);
    
    [Header("—— 除錯資訊 ——")]
    public bool showDebugInfo = true;
    [Tooltip("手動清理流線的快捷鍵")]
    public KeyCode clearStreamlinesKey = KeyCode.C;
    
    // VTK 物件
    vtkUnstructuredGrid uGrid;
    
    // Unity 物件
    List<GameObject> streamlineObjects = new List<GameObject>();
    GameObject seedSphereObject;
    
    // 溫度範圍
    float globalTMin, globalTMax;
    
    // Jet Colormap 查找表
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
    
    // 狀態追蹤
    bool isInitialized = false;
    Vector3 lastSeedCenter;
    float lastSeedRadius;
    
    void Start()
    {
        Initialize();
    }
    
    void OnEnable()
    {
        if (!isInitialized)
        {
            Initialize();
        }
    }
    
    void Initialize()
    {
        if (LoadVTUData())
        {
            isInitialized = true;
            CreateSeedSphere();
            GenerateStreamlines();
            lastSeedCenter = seedCenter;
            lastSeedRadius = seedRadius;
        }
    }
    
    void Update()
    {
        // 手動清理流線
        if (Input.GetKeyDown(clearStreamlinesKey))
        {
            Debug.Log("[VTK] 手動清理流線");
            ClearAllStreamlines();
        }
        
        // 檢查種子球位置是否改變（使用本地座標）
        if (seedSphereObject != null)
        {
            Vector3 currentLocalPos = seedSphereObject.transform.localPosition;
            if (currentLocalPos != seedCenter)
            {
                seedCenter = currentLocalPos;
            }
        }
        
        // 檢查半徑是否改變
        bool radiusChanged = Mathf.Abs(seedRadius - lastSeedRadius) > 0.0001f;
        if (radiusChanged)
        {
            UpdateSeedSphere();
            if (showDebugInfo)
            {
                Debug.Log($"[Update] 種子半徑改變: {lastSeedRadius} → {seedRadius}");
            }
        }
        
        // 檢查是否需要更新流線（位置或半徑改變時）
        bool positionChanged = Vector3.Distance(seedCenter, lastSeedCenter) > 0.001f;
        if (positionChanged || radiusChanged)
        {
            if (showDebugInfo && positionChanged)
            {
                Debug.Log($"[Update] 種子位置改變: {lastSeedCenter} → {seedCenter}");
            }
            
            UpdateStreamlines();
            lastSeedCenter = seedCenter;
            lastSeedRadius = seedRadius;
        }
    }
    
    void OnValidate()
    {
        if (isInitialized && !Application.isPlaying)
        {
            UpdateSeedSphere();
            UpdateStreamlines();
        }
    }
    
    bool LoadVTUData()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, vtuFileName);
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError($"找不到 VTU 檔案: {path}");
            return false;
        }
        
        try
        {
            var reader = vtkXMLUnstructuredGridReader.New();
            reader.SetFileName(path);
            reader.Update();
            uGrid = reader.GetOutput();
            
            if (uGrid == null || uGrid.GetNumberOfCells() == 0)
            {
                Debug.LogError("VTU 檔案載入失敗或沒有資料");
                return false;
            }
            
            double[] bounds = uGrid.GetBounds();
            Debug.Log($"[VTU] 載入成功！");
            Debug.Log($"[VTU] 邊界: X[{bounds[0]:F2}, {bounds[1]:F2}], Y[{bounds[2]:F2}, {bounds[3]:F2}], Z[{bounds[4]:F2}, {bounds[5]:F2}]");
            Debug.Log($"[VTU] 點數: {uGrid.GetNumberOfPoints()}, 格子數: {uGrid.GetNumberOfCells()}");
            
            return true;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"載入 VTU 失敗: {e.Message}");
            return false;
        }
    }
    
    void UpdateStreamlines()
    {
        if (!isInitialized || uGrid == null) return;
        
        ClearAllStreamlines();
        GenerateStreamlines();
    }
    
    void GenerateStreamlines()
    {
        if (uGrid == null) return;
        
        ClearAllStreamlines();
        
        try
        {
            // 1) 建立種子點
            var seeds = vtkPointSource.New();
            float vtkZ = flipZ ? (zFlipValue - seedCenter.z) : seedCenter.z;
            seeds.SetCenter(seedCenter.x, seedCenter.y, vtkZ);
            seeds.SetRadius(seedRadius);
            seeds.SetNumberOfPoints(numSeedPoints);
            seeds.Update();
            
            if (showDebugInfo)
            {
                Debug.Log($"[Seeds] Unity 中心: {seedCenter}, VTK 中心: ({seedCenter.x}, {seedCenter.y}, {vtkZ})");
                Debug.Log($"[Seeds] 半徑: {seedRadius}, 數量: {numSeedPoints}");
                Debug.Log($"[Seeds] 實際產生: {seeds.GetOutput().GetNumberOfPoints()} 個種子點");
            }
            
            // 2) 設定 StreamTracer
            var tracer = vtkStreamTracer.New();
            tracer.SetInputData(uGrid);
            tracer.SetSourceConnection(seeds.GetOutputPort());
            
            tracer.SetIntegrationDirectionToBoth();
            tracer.SetMaximumPropagation(maxPropagation);
            tracer.SetInitialIntegrationStep(initialStep);
            tracer.SetMinimumIntegrationStep(minStep);
            tracer.SetIntegratorType(2);
            tracer.SetComputeVorticity(true);
            
            tracer.Update();
            
            // 3) 取得流線資料
            vtkPolyData lines = tracer.GetOutput();
            int lineCount = (int)lines.GetNumberOfCells();
            
            if (showDebugInfo)
            {
                Debug.Log($"[Tracer] 產生 {lineCount} 條流線, {lines.GetNumberOfPoints()} 個點");
            }
            
            if (lineCount == 0)
            {
                Debug.LogWarning("沒有產生任何流線！請檢查種子位置是否在流場內");
                return;
            }
            
            CreateLineRenderers(lines);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"GenerateStreamlines 錯誤: {e.Message}\n{e.StackTrace}");
        }
    }
    
    void CreateLineRenderers(vtkPolyData lines)
    {
        vtkCellArray cellArray = lines.GetLines();
        vtkPoints pts = lines.GetPoints();
        
        vtkDataArray scalarArr = null;
        
        // 選擇標量欄位
        if (!string.IsNullOrEmpty(scalarFieldName))
        {
            scalarArr = lines.GetPointData().GetArray(scalarFieldName);
            if (scalarArr == null)
            {
                Debug.LogWarning($"找不到指定的標量欄位 '{scalarFieldName}'，改用 active scalar");
            }
        }
        
        if (scalarArr == null)
        {
            scalarArr = lines.GetPointData().GetScalars();
            if (scalarArr == null && lines.GetPointData().GetNumberOfArrays() > 0)
            {
                scalarArr = lines.GetPointData().GetArray(0);
            }
        }
        
        if (scalarArr == null)
        {
            Debug.LogError("沒有找到任何標量資料");
            return;
        }
        
        CalculateGlobalScalarRange(scalarArr);
        
        if (showDebugInfo)
        {
            Debug.Log($"[標量範圍] {globalTMin:F1} - {globalTMax:F1}");
        }
        
        // 更新 ColorBar
        var simpleColorBar = GameObject.FindObjectOfType<SimpleVRColorBar>();
        if (simpleColorBar != null)
        {
            simpleColorBar.UpdateRange(globalTMin, globalTMax);
        }
                
        vtkIdList ids = vtkIdList.New();
        int lineIndex = 0;
        
        cellArray.InitTraversal();
        while (cellArray.GetNextCell(ids) == 1)
        {
            int n = (int)ids.GetNumberOfIds();
            if (n >= 2)
            {
                CreateSingleLineRenderer(ids, pts, scalarArr, lineIndex++);
            }
        }
        
        if (showDebugInfo)
        {
            Debug.Log($"[渲染] 建立了 {lineIndex} 條 LineRenderer");
        }
    }
    
    void CalculateGlobalScalarRange(vtkDataArray scalarArr)
    {
        int nt = (int)scalarArr.GetNumberOfTuples();
        globalTMin = float.MaxValue;
        globalTMax = float.MinValue;
        
        for (int i = 0; i < nt; i++)
        {
            float val = (float)scalarArr.GetComponent(i, 0);
            if (val < globalTMin) globalTMin = val;
            if (val > globalTMax) globalTMax = val;
        }
        
        if (globalTMax - globalTMin < 0.001f)
        {
            globalTMax = globalTMin + 1f;
        }
    }
    
    void CreateSingleLineRenderer(vtkIdList ids, vtkPoints pts, vtkDataArray scalarArr, int lineIndex)
    {
        int n = (int)ids.GetNumberOfIds();
        var positions = new Vector3[n];
        var colors = new Color[n];
        
        float invRange = 1f / (globalTMax - globalTMin);
        
        for (int i = 0; i < n; i++)
        {
            long pid = ids.GetId(i);
            double[] d = pts.GetPoint(pid);
            float z = flipZ ? (zFlipValue - (float)d[2]) : (float)d[2];
            positions[i] = new Vector3((float)d[0], (float)d[1], z);
            
            float val = (float)scalarArr.GetComponent(pid, 0);
            float valNorm = (val - globalTMin) * invRange;
            
            if (invertColorMap)
            {
                valNorm = 1f - valNorm;
            }
            
            colors[i] = useVTKColorMap ? GetJetColorLUT(valNorm) : GetSimpleTemperatureColor(valNorm);
        }
        
        var go = new GameObject($"Streamline_{lineIndex}");
        go.transform.SetParent(transform, false);
        
        var lr = go.AddComponent<LineRenderer>();
        lr.positionCount = n;
        lr.SetPositions(positions);
        lr.colorGradient = CreateGradient(colors);
        lr.widthCurve = AnimationCurve.Constant(0, 1, lineWidth);
        
        var mat = new Material(Shader.Find("Sprites/Default"));
        lr.material = mat;
        lr.textureMode = LineTextureMode.Stretch;
        lr.alignment = LineAlignment.View;
        lr.numCornerVertices = 4;
        lr.numCapVertices = 4;
        
        streamlineObjects.Add(go);
    }
    
    Color GetJetColorLUT(float t)
    {
        t = Mathf.Clamp01(t);
        float scaledT = t * (JetLUT.Length - 1);
        int index = Mathf.FloorToInt(scaledT);
        float fraction = scaledT - index;
        
        if (index >= JetLUT.Length - 1)
        {
            return JetLUT[JetLUT.Length - 1];
        }
        
        return Color.Lerp(JetLUT[index], JetLUT[index + 1], fraction);
    }
    
    Color GetSimpleTemperatureColor(float t)
    {
        return Color.HSVToRGB(0.67f * (1f - t), 1f, 1f);
    }
    
    Gradient CreateGradient(Color[] colors)
    {
        var gradient = new Gradient();
        int keyCount = Mathf.Min(colors.Length, 8);
        var colorKeys = new GradientColorKey[keyCount];
        
        for (int i = 0; i < keyCount; i++)
        {
            float t = i / (float)(keyCount - 1);
            int idx = Mathf.FloorToInt(t * (colors.Length - 1));
            colorKeys[i] = new GradientColorKey(colors[idx], t);
        }
        
        gradient.SetKeys(colorKeys, new[] {
            new GradientAlphaKey(1f, 0f),
            new GradientAlphaKey(1f, 1f)
        });
        
        return gradient;
    }
    
    void CreateSeedSphere()
    {
        if (seedSphereObject == null)
        {
            Transform existing = transform.Find("SeedSphere");
            if (existing != null)
            {
                seedSphereObject = existing.gameObject;
                var dragger = seedSphereObject.GetComponent<SeedDragger>();
                if (dragger == null)
                {
                    dragger = seedSphereObject.AddComponent<SeedDragger>();
                    dragger.owner = this;
                }
            }
            else
            {
                seedSphereObject = GameObject.CreatePrimitive(PrimitiveType.Sphere);
                seedSphereObject.name = "SeedSphere";
                seedSphereObject.transform.SetParent(transform, false);
                
                var mr = seedSphereObject.GetComponent<MeshRenderer>();
                var mat = new Material(Shader.Find("Standard"));
                mat.color = sphereColor;
                mat.SetFloat("_Metallic", 0);
                mat.SetFloat("_Glossiness", 0.4f);
                mr.material = mat;
                
                var dragger = seedSphereObject.AddComponent<SeedDragger>();
                dragger.owner = this;
            }
        }
        
        UpdateSeedSphere();
    }
    
    void UpdateSeedSphere()
    {
        if (seedSphereObject != null)
        {
            seedSphereObject.transform.localPosition = seedCenter;
            seedSphereObject.transform.localScale = Vector3.one * (seedRadius * 2f);
        }
    }
    
    void ClearStreamlines()
    {
        foreach (var obj in streamlineObjects)
        {
            if (obj != null)
            {
                DestroyImmediate(obj);
            }
        }
        streamlineObjects.Clear();
    }
    
    void ClearAllStreamlines()
    {
        // 從列表清理
        for (int i = streamlineObjects.Count - 1; i >= 0; i--)
        {
            if (streamlineObjects[i] != null)
            {
                DestroyImmediate(streamlineObjects[i]);
            }
        }
        streamlineObjects.Clear();
        
        // 從 Transform 清理
        var children = new List<Transform>();
        foreach (Transform child in transform)
        {
            if (child.name.StartsWith("Streamline_"))
            {
                children.Add(child);
            }
        }
        
        foreach (var child in children)
        {
            DestroyImmediate(child.gameObject);
        }
    }
    
    void OnDestroy()
    {
        ClearAllStreamlines();
        if (seedSphereObject != null)
        {
            if (Application.isPlaying)
                Destroy(seedSphereObject);
            else
                DestroyImmediate(seedSphereObject);
        }
    }
    
    // 公開方法
    public void SetSeedCenter(Vector3 newCenter)
    {
        seedCenter = newCenter;
        
        if (showDebugInfo)
        {
            Debug.Log($"[SetSeedCenter] 新位置: {newCenter}");
        }
    }
    
    public void ClearAllStreamlinesPublic()
    {
        ClearAllStreamlines();
    }
    
    public void RegenerateStreamlines()
    {
        ClearAllStreamlines();
        if (isInitialized)
        {
            GenerateStreamlines();
        }
        else
        {
            Debug.LogWarning("VTK 尚未初始化，無法生成流線");
        }
    }
}