using UnityEngine;
using Fusion;
public class PlayerHookReceiver : NetworkBehaviour
{
    [Header("Trạng Thái Bị Vác / Treo")]
    [Networked, OnChangedRender(nameof(OnCarryStateChanged))]
    public NetworkBool isBeingCarried { get; set; }
    public Transform targetFollow;
    public float followSpeed = 8f;
    private CharacterController controller;
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }
    public void GetPickedUpOrHooked(Transform targetPoint)
    {
        if (!Object.HasStateAuthority) return;
        targetFollow = targetPoint;
        isBeingCarried = true;
        if (controller != null) controller.enabled = false;
        Debug.Log("Player: Đang bám chặt vào -> " + targetPoint.name);
    }
    public void ReleaseFromHunter()
    {
        if (!Object.HasStateAuthority) return;

        isBeingCarried = false;
        targetFollow = null;
    }
    public void OnCarryStateChanged()
    {
        if (controller != null)
        {
            bool isHookedLocal = false;
            var s1 = GetComponent<IShowSpeedController_Fusion>();
            if (s1 != null && s1.IsHooked) isHookedLocal = true;
            var s2 = GetComponent<MrBeanController_Fusion>();
            if (s2 != null && s2.IsHooked) isHookedLocal = true;
            var s3 = GetComponent<MrBeastController_Fusion>();
            if (s3 != null && s3.IsHooked) isHookedLocal = true;
            var s4 = GetComponent<NurseController_Fusion>();
            if (s4 != null && s4.IsHooked) isHookedLocal = true;
            if (isBeingCarried || isHookedLocal)
            {
                controller.enabled = false;
            }
            else
            {
                controller.enabled = true;
            }
        }
    }
    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;
        if (isBeingCarried && targetFollow != null)
        {
            float distance = Vector3.Distance(transform.position, targetFollow.position);
            Quaternion safeRotation = targetFollow.root.rotation;
            if (distance < 0.1f)
            {
                transform.position = targetFollow.position;
                transform.rotation = safeRotation; 
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetFollow.position, followSpeed * Runner.DeltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, safeRotation, followSpeed * Runner.DeltaTime); 
            }
        }
    }
}