using UnityEngine;
using Fusion;

public class LocalPlayerUI : NetworkBehaviour
{
    [Header("Nhip tim")]
    public GameObject HeartPlayerObject;

    public override void Spawned()
    {
        // Kiểm tra xem nhân vật này CÓ PHẢI do máy mình điều khiển không?
        if (!Object.HasInputAuthority)
        {
            // Nếu là nhân vật của người khác -> Tắt rụp cái SkillPlayer đi để khỏi rác màn hình
            if (HeartPlayerObject != null) HeartPlayerObject.SetActive(false);
        }
        else
        {
            // Nếu là nhân vật của mình -> Bật nó lên
            if (HeartPlayerObject != null) HeartPlayerObject.SetActive(true);
        }
    }
}