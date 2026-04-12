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
using Fusion; // 1. Thêm thư viện Fusion
using System.Collections;

public class HunterMovement : NetworkBehaviour // 2. Đổi thành NetworkBehaviour
{
    public Animator animator;
    [Header("Cài Đặt Tốc Độ")]
    public float walkStraight = 5f;
    public float walkBackward = 4f;

    // Đồng bộ tốc độ qua mạng để Animator các máy khác chạy khớp nhau
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

    // 3. Các biến vật lý phải là [Networked] để Fusion dự đoán (Prediction)
    [Networked] private float velocityY { get; set; }
    [Networked] private float currentSpeed { get; set; }
    [Networked] private NetworkBool wasGrounded { get; set; }

    // Thay thế Invoke bằng TickTimer của Fusion
    [Networked] private TickTimer slowTimer { get; set; }
    [Networked] public float AnimSpeedValue { get; set; }

    private readonly int animSpeed = Animator.StringToHash("Speed");

    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();

        // 🚨 SỬA Ở ĐÂY: Tìm Animator ở cục con (Visuals)
        animator = GetComponentInChildren<Animator>();

        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        if (controller != null)
        {
            StartCoroutine(LocalCCReset());
        }

        if (Object.HasStateAuthority)
        {
            currentSpeedMultiplier = 1f;
        }
    }

    IEnumerator LocalCCReset()
    {
        controller.enabled = false;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();

        // 🚨 CHỈ BẬT LẠI NẾU BẠN LÀ HOST HOẶC NGƯỜI ĐIỀU KHIỂN HUNTER. 
        // (Tắt vĩnh viễn trên màn hình của người khác để không bị giật)
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            controller.enabled = true;
        }
    }

    // Thêm tham số camYaw vào đây
    public void HandleMove(Vector2 input, float camYaw)
    {

        if (controller == null || !controller.enabled) return;

        // 🚨 FIX X2 SPEED CHO HUNTER
        controller.enabled = false;
        controller.enabled = true;

        transform.rotation = Quaternion.Euler(0, camYaw, 0);
        if (slowTimer.Expired(Runner))
        {
            ResetSlow();
            slowTimer = TickTimer.None;
        }

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

        // 🚨 ĐÃ FIX: Khôn dùng Lerp cho vật lý mạng. Gán thẳng tốc độ để Server và Client đồng bộ chính xác 100%
        currentSpeed = targetSpeed;

        controller.Move(move * (currentSpeed * Runner.DeltaTime) + new Vector3(0, velocityY, 0) * Runner.DeltaTime);

        // 🚨 CHỈ TÍNH TOÁN TARGET SPEED, KHÔNG DÙNG LERP Ở ĐÂY
        float fakeSpeedForAnimator = currentSpeed;
        if (currentSpeedMultiplier > 0f) fakeSpeedForAnimator = currentSpeed / currentSpeedMultiplier;

        float targetAnimValue = fakeSpeedForAnimator / walkStraight;
        if (input.y < 0) targetAnimValue = -targetAnimValue;

        // Gán vào biến mạng
        AnimSpeedValue = targetAnimValue;
    }
    public override void Render()
    {
        if (animator == null) return;

        float currentAnimValue = animator.GetFloat(animSpeed);
        // Nội suy mượt mà dựa trên Time.deltaTime theo khung hình màn hình
        animator.SetFloat(animSpeed, Mathf.Lerp(currentAnimValue, AnimSpeedValue, Time.deltaTime * 15f));
    }

    private void TriggerHardLanding()
    {
        // Chỉ Host gọi RPC để tránh phát âm thanh 2 lần
        if (Object.HasStateAuthority)
        {
            Rpc_PlayLandingSound();
        }

        // 2. Làm chậm Hunter
        ApplySlow(landSlowMult);

        // 3. Đặt đồng hồ đếm ngược thay cho Invoke
        slowTimer = TickTimer.CreateFromSeconds(Runner, landSlowDuration);

        Debug.Log("💥 Tiếp đất mạnh! Bị làm chậm.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayLandingSound()
    {
        // Phát âm thanh rơi mạnh
        if (audioSource != null && landingClip != null)
        {
            audioSource.PlayOneShot(landingClip, 1f * GetVFXVolume());
        }
    }

    public void PlayFootstep()
    {
        // Tiếng bước chân
        if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(footstepClip, 0.6f * GetVFXVolume());
        }
    }

    public void ApplySlow(float mult) { currentSpeedMultiplier = mult; }
    public void ResetSlow() { currentSpeedMultiplier = 1f; }
    // 🚨 HÀM LẤY ÂM LƯỢNG VFX
    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}