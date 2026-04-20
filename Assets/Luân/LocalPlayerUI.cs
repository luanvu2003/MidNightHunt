using UnityEngine;
using Fusion;

public class LocalPlayerUI : NetworkBehaviour
{
    [Header("Nhip tim")]
    public GameObject HeartPlayerObject;
    public override void Spawned()
    {
        if (!Object.HasInputAuthority)
        {
            if (HeartPlayerObject != null) HeartPlayerObject.SetActive(false);
        }
        else
        {
            if (HeartPlayerObject != null) HeartPlayerObject.SetActive(true);
        }
    }
}