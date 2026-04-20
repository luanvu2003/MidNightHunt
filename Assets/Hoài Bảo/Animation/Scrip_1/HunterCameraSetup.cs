using UnityEngine;
using Fusion;
public class HunterCameraSetup : NetworkBehaviour 
{
    [Header("Cài đặt Camera cho riêng Hunter này")]
    public string headBoneName = "Head"; 
    public Vector3 myEyeOffset = new Vector3(0f, 0.1f, 0.2f); 

    public override void Spawned() 
    {
        if (Object.HasInputAuthority)
        {
            Invoke(nameof(CallCameraToMe), 0.1f);
        }
    }
    private void CallCameraToMe()
    {
        FPSCamera mainCam = Camera.main.GetComponent<FPSCamera>();
        
        if (mainCam != null)
        {
            Transform myHead = FindChildByName(transform, headBoneName);

            if (myHead != null)
            {
                mainCam.SetupCameraForHunter(this.transform, myHead, myEyeOffset);
            }
            else
            {
                Debug.LogError(" Lỗi: Không tìm thấy xương đầu tên là " + headBoneName + " trên " + gameObject.name);
            }
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