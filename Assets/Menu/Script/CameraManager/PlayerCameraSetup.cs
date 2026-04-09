using Fusion;
using UnityEngine;

public class PlayerCameraSetup : NetworkBehaviour
{
    [Header("Cấu hình xương (Chỉ dùng cho Hunter)")]
    public string headBoneName = "mixamorig:Head"; 
    public Vector3 myFpsEyeOffset = new Vector3(0f, 0.1f, 0.2f); 

    [Header("Cấu hình góc nhìn (Chỉ dùng cho Survivor)")]
    public Vector3 myTpsOffset = new Vector3(0.5f, 1.5f, -3f); 

    public override void Spawned()
    {
        // 🚨 CHỐT CHẶN MẠNG: Chỉ gọi Camera tới nếu ĐÂY LÀ NHÂN VẬT TRÊN MÁY MÌNH
        // Nếu không có dòng này, Camera sẽ nhảy loạn xạ giữa 4 người chơi
        if (Object.HasInputAuthority)
        {
            Invoke(nameof(CallCameraToMe), 0.1f);
        }
    }

    private void CallCameraToMe()
    {
        GameCameraController mainCam = Camera.main.GetComponent<GameCameraController>();
        if (mainCam == null) return;

        // Đọc thẻ căn cước xem mình là phe nào (dựa vào Index hoặc Role)
        if (RoomPlayer.Local.RoomIndex == 0) // Hoặc RoomPlayer.Local.IsHunter
        {
            // MÌNH LÀ HUNTER -> GỌI SETUP FPS
            Transform myHead = FindChildByName(transform, headBoneName);
            if (myHead != null) mainCam.SetupFPS(this.transform, myHead, myFpsEyeOffset);
        }
        else 
        {
            // MÌNH LÀ SURVIVOR (Index 1, 2, 3) -> GỌI SETUP TPS
            // Tùy vào ID nhân vật (CharacterID 0, 1, 2, 3) mà myTpsOffset có thể khác nhau do bạn setup sẵn ở Prefab
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