// VTKHeatmapSlice.cs  (C# 7 版，Unity 2018–2021 皆可用)

using UnityEngine;
using System.Collections.Generic;
using Kitware.VTK;

[ExecuteAlways]
[RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
public class VTKHeatmapSlice : MonoBehaviour
{
    double sMinGlobal, sMaxGlobal;   //  新增

    /* ---------- Inspector ---------- */
    [Header("—— VTU ——")]
    public string vtuFileName = "fire_ug.vtu";          // 放在 StreamingAssets

    [Header("—— Slice ——")]
    [Range(10, 400)]
    public int    sliceResolution = 120;
    public string scalarArrayName = "t";

    /* ---------- 追蹤 Transform.X ---------- */
    float prevX = float.NaN;                      // 
    //  上一次的 X 座標

    /* ---------- 5-Color LUT ---------- */
    static readonly Color[] kLUT5 = new Color[5]
    {
        Color.red,  
        Color.yellow,
        Color.green,
        new Color(0f, 0.5f, 1f),
        Color.blue
    };
    static Color MapColor5(float u)
    {
        u = Mathf.Clamp01(u);
        float f = u * (kLUT5.Length - 1);
        int   i = Mathf.FloorToInt(f);
        float w = f - i;
        return Color.Lerp(kLUT5[i], kLUT5[Mathf.Min(i + 1, kLUT5.Length - 1)], w);
    }
    /* -------------------------------- */

    vtkUnstructuredGrid uGrid;        // 讀進來的場
    double[] bounds;                  // 6-elements

    MeshFilter   mf;
    MeshRenderer mr;

    /* ---------- Unity 生命週期 ---------- */
    void OnValidate() { Rebuild(); }
    void Start()      { Rebuild(); }

    void Update()                                //  在編輯模式 & 遊戲模式都會跑
    {
        // 只要 Transform 的 X 改變，就重建
        if (!Mathf.Approximately(transform.position.x, prevX))
        {
            prevX = transform.position.x;
            Rebuild();
        }
    }

    /* ---------- 核心流程 ---------- */
    void Rebuild()
    {
        if (!LoadGrid()) return;

        /* 1. 建平面 (YZ) */
        vtkPlaneSource plane = vtkPlaneSource.New();
        plane.SetXResolution(sliceResolution);
        plane.SetYResolution(sliceResolution);
        UpdatePlanePosition(plane, transform.position.x);

        /* 2. Probe */
        vtkProbeFilter probe = vtkProbeFilter.New();
        probe.SetSourceData(uGrid);
        probe.SetInputConnection(plane.GetOutputPort());
        probe.Update();

        vtkPolyData sampled = vtkPolyData.SafeDownCast(probe.GetOutput());
        if (sampled == null) { Debug.LogError("Probe 輸出不是 vtkPolyData"); return; }

        vtkDataArray scalars = sampled.GetPointData().GetArray(scalarArrayName);
        if (scalars == null)
        {
            Debug.LogError("找不到欄位 '" + scalarArrayName + "'");
            return;
        }

        /* 3. 轉 Unity Mesh */
        Mesh mesh = BuildMeshFromSample(sampled, scalars);

        if (mf == null) mf = GetComponent<MeshFilter>();
        if (mr == null) mr = GetComponent<MeshRenderer>();

        mf.sharedMesh      = mesh;
        mr.sharedMaterial  = new Material(Shader.Find("Sprites/Default"));
    }

    /* ---------- 平面位置 (只改 X) ---------- */
    void UpdatePlanePosition(vtkPlaneSource plane, double x)
    {
        plane.SetOrigin (x, bounds[2], bounds[4]);
        plane.SetPoint1(x, bounds[3], bounds[4]);
        plane.SetPoint2(x, bounds[2], bounds[5]);
        plane.Update();
    }

    /* ---------- 建 Mesh ---------- */
    Mesh BuildMeshFromSample(vtkPolyData poly, vtkDataArray scalars)
    {
        int nx = sliceResolution + 1;
        vtkPoints pts   = poly.GetPoints();
        int nPts        = (int)pts.GetNumberOfPoints();

        Vector3[] vtx  = new Vector3[nPts];
        Color[]   vcol = new Color[nPts];

        /* 取 scalar range */
        double sMin = sMinGlobal;   // 改成全域範圍
        double sMax = sMaxGlobal;
        double inv  = (sMax > sMin) ? 1.0 / (sMax - sMin) : 0.0;

        for (int i = 0; i < nPts; ++i)
        {
            double[] p = pts.GetPoint(i);
            vtx[i] = vtx[i] = new Vector3((float)p[0], (float)p[1], (float)p[2])- transform.position;

            double s = scalars.GetComponent(i, 0);
            float u  = (float)((s - sMin) * inv);
            vcol[i]  = MapColor5(u);
        }

        /* 三角形索引 */
        List<int> tris = new List<int>(sliceResolution * sliceResolution * 6);
        for (int j = 0; j < sliceResolution; ++j)
        {
            for (int i = 0; i < sliceResolution; ++i)
            {
                int id0 =  j      * nx + i;
                int id1 =  j      * nx + i + 1;
                int id2 = (j + 1) * nx + i;
                int id3 = (j + 1) * nx + i + 1;

                tris.Add(id0); tris.Add(id2); tris.Add(id1);
                tris.Add(id1); tris.Add(id2); tris.Add(id3);
            }
        }

        Mesh mesh = new Mesh();
        mesh.indexFormat = (nPts > 65000)
            ? UnityEngine.Rendering.IndexFormat.UInt32
            : UnityEngine.Rendering.IndexFormat.UInt16;

        mesh.vertices  = vtx;
        mesh.colors    = vcol;
        mesh.triangles = tris.ToArray();
        mesh.RecalculateNormals();
        return mesh;
    }

    /* ---------- 讀 VTU ---------- */
    bool LoadGrid()
    {
        string path = System.IO.Path.Combine(Application.streamingAssetsPath, vtuFileName);
        if (!System.IO.File.Exists(path))
        {
            Debug.LogError("找不到 " + path);
            return false;
        }

        vtkXMLUnstructuredGridReader reader = vtkXMLUnstructuredGridReader.New();
        reader.SetFileName(path);
        reader.Update();

        uGrid  = reader.GetOutput();
        bounds = uGrid.GetBounds();     // ActiViz: 回傳 double[6]

        //  計算全域標量範圍
        vtkDataArray globalField = uGrid.GetPointData().GetArray(scalarArrayName);
        double[] globalRange = globalField.GetRange();
        sMinGlobal = globalRange[0];
        sMaxGlobal = globalRange[1];
        prevX = transform.position.x;
        return true;
    }
}
