using UnityEngine;
using Fusion;

public class LocalSkillOnlyController : NetworkBehaviour
{
    [Header("Kéo object SkillPlayer vào đây")]
    public GameObject skillPlayerObject;
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            if (skillPlayerObject != null) skillPlayerObject.SetActive(false);
        }
        else
        {
            if (skillPlayerObject != null) skillPlayerObject.SetActive(true);
        }
    }
}