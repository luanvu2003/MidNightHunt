using UnityEngine;
using Fusion;

public class LocalSkillOnlyController : NetworkBehaviour
{
    [Header("Kéo object SkillPlayer vào đây")]
    public GameObject skillPlayerObject;

    public override void Spawned()
    {
        // Kiểm tra xem nhân vật này CÓ PHẢI do máy mình điều khiển không?
        if (!Object.HasInputAuthority)
        {
            // Nếu là nhân vật của người khác -> Tắt rụp cái SkillPlayer đi để khỏi rác màn hình
            if (skillPlayerObject != null) skillPlayerObject.SetActive(false);
        }
        else
        {
            // Nếu là nhân vật của mình -> Bật nó lên
            if (skillPlayerObject != null) skillPlayerObject.SetActive(true);
        }
    }
}