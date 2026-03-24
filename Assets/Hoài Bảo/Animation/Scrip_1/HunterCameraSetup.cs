using UnityEngine;

public class HunterCameraSetup : MonoBehaviour
{
    [Header("Cài đặt Camera cho riêng Hunter này")]
    public string headBoneName = "Head"; // Tên xương đầu (VD: mixamorig:Head)
    
    // Tùy chỉnh thông số này ở ngoài Inspector cho MỖI CON một kiểu khác nhau
    public Vector3 myEyeOffset = new Vector3(0f, 0.1f, 0.2f); 

    private void Start()
    {
        // Khi Hunter vừa đẻ ra, hẹn 0.1s sau thì gọi Camera tới (để tránh giật lag frame đầu)
        Invoke(nameof(CallCameraToMe), 0.1f);
    }

    private void CallCameraToMe()
    {
        // 1. Tìm Main Camera trong bản đồ
        FPSCamera mainCam = Camera.main.GetComponent<FPSCamera>();
        
        if (mainCam != null)
        {
            // 2. Tự lục tìm xương đầu của chính mình
            Transform myHead = FindChildByName(transform, headBoneName);

            if (myHead != null)
            {
                // 3. Gọi Main Camera tới và nạp thông số cá nhân của mình vào
                mainCam.SetupCameraForHunter(this.transform, myHead, myEyeOffset);
            }
            else
            {
                Debug.LogError("❌ Lỗi: Không tìm thấy xương đầu tên là " + headBoneName + " trên " + gameObject.name);
            }
        }
    }

    // Thuật toán lục tìm xương (Không phân biệt hoa/thường)
    private Transform FindChildByName(Transform parent, string nameToFind)
    {
        Transform[] children = parent.GetComponentsInChildren<Transform>(true);
        string searchName = nameToFind.ToLower();

        foreach (Transform child in children)
        {
            if (child.name.ToLower().Contains(searchName)) 
            {
                return child;
            }
        }
        return null;
    }
}