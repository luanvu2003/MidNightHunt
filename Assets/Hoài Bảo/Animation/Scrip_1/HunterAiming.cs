using UnityEngine;
using Fusion; 
public class HunterAiming : NetworkBehaviour 
{
    [Header("Xương Cột Sống")]
    public Transform spineBone; 
    [Header("Độ gập người (0 đến 1)")]
    [Range(0f, 1f)]
    public float aimWeight = 0.5f; 
    [Networked] public float networkPitch { get; set; }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasInputAuthority && Camera.main != null)
        {
            float camPitch = Camera.main.transform.eulerAngles.x;
            if (camPitch > 180f) camPitch -= 360f;
            networkPitch = camPitch;
        }
    }
    private void LateUpdate()
    {
        if (spineBone == null) return;
        float finalPitch = networkPitch * aimWeight;
        spineBone.localRotation *= Quaternion.Euler(finalPitch, 0f, 0f);
    }
}