// using UnityEngine;

// [RequireComponent(typeof(CharacterController), typeof(Animator))]
// public class HunterMovement : MonoBehaviour
// {
//     [Header("Cài Đặt Tốc Độ")]
//     public float walkStraight = 5f;
//     public float walkBackward = 4f;
//     public float currentSpeedMultiplier = 1f;

//     [Header("Âm Thanh")]
//     public AudioSource audioSource;
//     public AudioClip footstepClip;
//     public AudioClip landingClip; // 🔊 Kéo file âm thanh rơi vào đây

//     [Header("Cài Đặt Rơi")]
//     public float hardLandingThreshold = -12f; // Độ rơi mạnh (Vận tốc Y). Thường từ -10 đến -15.
//     public float landSlowMult = 0.2f;         // Làm chậm còn 20% tốc độ
//     public float landSlowDuration = 1.5f;     // Bị chậm trong 1.5 giây

//     private CharacterController controller;
//     private Animator animator;
//     private float velocityY;
//     private float currentSpeed;
//     private bool wasGrounded; // Biến phụ để kiểm tra trạng thái khung hình trước

//     private readonly int animSpeed = Animator.StringToHash("Speed");

//     private void Awake()
//     {
//         controller = GetComponent<CharacterController>();
//         animator = GetComponent<Animator>();
//         if (audioSource == null) audioSource = GetComponent<AudioSource>();
//     }

//     public void HandleMove(Vector2 input)
//     {
//         if (controller == null || !controller.enabled) return;

//         // --- LOGIC KIỂM TRA TIẾP ĐẤT (LANDING) ---
//         if (!wasGrounded && controller.isGrounded)
//         {
//             // Nếu vận tốc rơi trước khi chạm đất nhỏ hơn ngưỡng (ví dụ -12)
//             if (velocityY < hardLandingThreshold)
//             {
//                 TriggerHardLanding();
//             }
//         }

//         // Lưu trạng thái chạm đất để dùng cho khung hình sau
//         wasGrounded = controller.isGrounded;

//         Vector3 move = (transform.forward * input.y + transform.right * input.x).normalized;

//         float targetSpeed = 0f;
//         if (input.y < 0) targetSpeed = walkBackward * currentSpeedMultiplier;
//         else if (move.magnitude > 0) targetSpeed = walkStraight * currentSpeedMultiplier;

//         // Trọng lực
//         if (controller.isGrounded && velocityY < 0) velocityY = -2f;
//         velocityY += -9.81f * Time.deltaTime;

//         currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f);
//         controller.Move(move * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);

//         // Animator logic
//         float fakeSpeedForAnimator = currentSpeed;
//         if (currentSpeedMultiplier > 0f) fakeSpeedForAnimator = currentSpeed / currentSpeedMultiplier;
//         float animValue = fakeSpeedForAnimator / walkStraight;
//         if (input.y < 0) animValue = -animValue;
//         animator.SetFloat(animSpeed, animValue);
//     }

//     private void TriggerHardLanding()
//     {
//         // 1. Phát âm thanh
//         if (audioSource != null && landingClip != null)
//         {
//             audioSource.PlayOneShot(landingClip);
//         }

//         // 2. Làm chậm Hunter
//         ApplySlow(landSlowMult);

//         // 3. Tự động hồi phục tốc độ sau X giây
//         CancelInvoke(nameof(ResetSlow));
//         Invoke(nameof(ResetSlow), landSlowDuration);

//         Debug.Log("💥 Tiếp đất mạnh! Bị làm chậm.");
//     }

//     public void PlayFootstep()
//     {
//         if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
//         {
//             if (audioSource != null) audioSource.PlayOneShot(footstepClip, 0.6f);
//         }
//     }

//     public void ApplySlow(float mult) => currentSpeedMultiplier = mult;
//     public void ResetSlow() => currentSpeedMultiplier = 1f;
// }

using UnityEngine;
using Fusion;
using System.Collections;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class HunterMovement : NetworkBehaviour
{
    [Header("Cài Đặt Tốc Độ")]
    public float walkStraight = 5f;
    public float walkBackward = 4f;
    [Networked] public float currentSpeedMultiplier { get; set; }

    [Header("Âm Thanh")]
    public AudioSource audioSource;
    public AudioClip footstepClip;
    public AudioClip landingClip;

    [Header("Cài Đặt Rơi")]
    public float hardLandingThreshold = -12f;
    public float landSlowMult = 0.2f;
    public float landSlowDuration = 1.5f;

    private CharacterController controller;
    private Animator animator;
    [Networked] private float velocityY { get; set; }
    [Networked] private float currentSpeed { get; set; }
    [Networked] private NetworkBool wasGrounded { get; set; }

    private readonly int animSpeed = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // 🚨 THÊM ĐOẠN NÀY ĐỂ FIX DỰT
        if (controller != null)
        {
            StartCoroutine(LocalCCReset());
        }

        if (Object.HasStateAuthority) currentSpeedMultiplier = 1f;
    }
    IEnumerator LocalCCReset()
    {
        controller.enabled = false;
        // Chờ 2 nhịp Tick của Fusion để đảm bảo vị trí từ Server đã đổ về máy mình
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        controller.enabled = true;
    }

    public void HandleMove(Vector2 input)
    {
        if (controller == null || !controller.enabled) return;

        if (!wasGrounded && controller.isGrounded)
        {
            if (velocityY < hardLandingThreshold) TriggerHardLanding();
        }

        wasGrounded = controller.isGrounded;

        Vector3 move = (transform.forward * input.y + transform.right * input.x).normalized;

        float targetSpeed = 0f;
        if (input.y < 0) targetSpeed = walkBackward * currentSpeedMultiplier;
        else if (move.magnitude > 0) targetSpeed = walkStraight * currentSpeedMultiplier;

        if (controller.isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += -9.81f * Runner.DeltaTime;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Runner.DeltaTime * 15f);
        controller.Move(move * (currentSpeed * Runner.DeltaTime) + new Vector3(0, velocityY, 0) * Runner.DeltaTime);

        float fakeSpeedForAnimator = currentSpeed;
        if (currentSpeedMultiplier > 0f) fakeSpeedForAnimator = currentSpeed / currentSpeedMultiplier;
        float animValue = fakeSpeedForAnimator / walkStraight;
        if (input.y < 0) animValue = -animValue;
        animator.SetFloat(animSpeed, animValue);
    }

    private void TriggerHardLanding()
    {
        Rpc_PlayLandingSound();
        ApplySlow(landSlowMult);
        Invoke(nameof(ResetSlow), landSlowDuration);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayLandingSound()
    {
        if (audioSource != null && landingClip != null) audioSource.PlayOneShot(landingClip);
    }

    public void PlayFootstep()
    {
        if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(footstepClip, 0.6f);
        }
    }

    public void ApplySlow(float mult) { if (Object.HasStateAuthority) currentSpeedMultiplier = mult; }
    public void ResetSlow() { if (Object.HasStateAuthority) currentSpeedMultiplier = 1f; }
}