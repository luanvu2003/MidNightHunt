using Fusion;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("== ĐỊNH DANH NHÂN VẬT ==")]
    public bool isHunterPrefab; // 🚨 TICK TRUE trên Prefab của Hunter, để FALSE trên Survivor

    [Header("Cấu hình xương (Chỉ dùng cho Hunter)")]
    public string headBoneName = "mixamorig:Head"; 
    public Vector3 myFpsEyeOffset = new Vector3(0f, 0.1f, 0.2f); 

    [Header("Cấu hình góc nhìn (Chỉ dùng cho Survivor)")]
    public Vector3 myTpsOffset = new Vector3(0.5f, 1.5f, -3f); 

    public override void Spawned()
    {
        // 🚨 CHỐT CHẶN MẠNG
        if (Object.HasInputAuthority)
        {
            Invoke(nameof(CallCameraToMe), 0.1f);
        }
    }

    private void CallCameraToMe()
    {
        GameCameraController mainCam = Camera.main.GetComponent<GameCameraController>();
        if (mainCam == null) return;

        // Dùng biến bool định danh sẵn trên Prefab thay vì check RoomIndex
        if (isHunterPrefab) 
        {
            // MÌNH LÀ HUNTER -> GỌI SETUP FPS
            Transform myHead = FindChildByName(transform, headBoneName);
            if (myHead != null) 
            {
                mainCam.SetupFPS(this.transform, myHead, myFpsEyeOffset);
            }
            else
            {
                Debug.LogError($"[Camera Setup] Lỗi: Không tìm thấy xương tên '{headBoneName}' trên Hunter!");
            }
        }
        else 
        {
            // MÌNH LÀ SURVIVOR -> GỌI SETUP TPS
            mainCam.SetupTPS(this.transform, myTpsOffset);
        }
    }

    private Transform FindChildByName(Transform parent, string nameToFind)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        string searchName = nameToFind.ToLower();
        foreach (Transform child in children)
        {
            if (child.name.ToLower().Contains(searchName)) return child;
        }
        return null;
    }
}