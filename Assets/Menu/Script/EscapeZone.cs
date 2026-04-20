using UnityEngine;
using Fusion;

public class EscapeZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (!Runner.IsServer) return; 

        if (other.CompareTag("Player"))
        {
            var survivor = other.GetComponent<ISurvivor>();
            if (survivor != null && survivor.Object != null && survivor.Object.IsValid)
            {
                if (!survivor.GetIsDowned() && !survivor.GetIsHooked())
                {
                    GameMatchManager_Fusion.Instance.RegisterPlayerEscape(survivor.Object);
                }
            }
        }
    }
}