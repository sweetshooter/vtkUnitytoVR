using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteAlways]
public class RoomAutoSpawner : MonoBehaviour
{
    /* ====== 你可自行修改的固定參數 ====== */
    static readonly Vector3 ROOT_WORLD_POS = new Vector3(0f, 0f, 2.4f);
    static readonly Vector3 ROOM_EULER     = new Vector3(0f, 180f, 0f);
    const           float   ROOM_SCALE     = 1f;
    const           string  PREFAB_PATH    = "Room/room_vis"; 
    const           string  CHILD_NAME     = "room_vis";      
    /* ==================================== */

    GameObject roomInstance;    // 指向場景中那唯一的房子

    /* ---------- 生命周期 ---------- */

    void OnEnable()
    {
        PutRootToCorrectPlace();   // RuntimeRoom → (0,0,2)
        EnsureRoomExist();         // 有就沿用，沒有就生成
        ApplyFixedTransform();     // 套旋轉＆縮放
    }

    /* ---------- 私有實作 ---------- */

    void PutRootToCorrectPlace()
    {
        transform.position   = ROOT_WORLD_POS;
        transform.rotation   = Quaternion.identity;
        transform.localScale = Vector3.one;
    }

    void EnsureRoomExist()
    {
        // A) 先找現成子物件，避免重複生成
        Transform found = transform.Find(CHILD_NAME);
        if (found)
        {
            roomInstance = found.gameObject;
            return;
        }

        // B) 若真的沒有，再從 Prefab 生成
        GameObject prefab = Resources.Load<GameObject>(PREFAB_PATH);
        if (prefab == null)
        {
            Debug.LogError($"無法載入 Resources/{PREFAB_PATH}");
            return;
        }

#if UNITY_EDITOR
        // 編輯器模式下用 PrefabUtility，避免還沒進 Play 就把修改標記 dirty
        if (!Application.isPlaying)
            roomInstance = (GameObject)PrefabUtility.InstantiatePrefab(prefab, transform);
        else
#endif
        roomInstance = Instantiate(prefab, transform);

        roomInstance.name = CHILD_NAME;          // 去除 (Clone)
        roomInstance.transform.localPosition = Vector3.zero;
    }

    void ApplyFixedTransform()
    {
        if (roomInstance == null) return;
        roomInstance.transform.localRotation = Quaternion.Euler(ROOM_EULER);
        roomInstance.transform.localScale    = Vector3.one * ROOM_SCALE;
    }
}
