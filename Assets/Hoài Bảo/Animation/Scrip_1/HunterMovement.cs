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

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class HunterMovement : NetworkBehaviour // 2. Đổi thành NetworkBehaviour
{
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
    private Animator animator;

    // 3. Các biến vật lý phải là [Networked] để Fusion dự đoán (Prediction)
    [Networked] private float velocityY { get; set; }
    [Networked] private float currentSpeed { get; set; }
    [Networked] private NetworkBool wasGrounded { get; set; }

    // Thay thế Invoke bằng TickTimer của Fusion
    [Networked] private TickTimer slowTimer { get; set; }

    private readonly int animSpeed = Animator.StringToHash("Speed");

    public override void Spawned() // Thay Awake/Start bằng Spawned
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();

        // 🚨 FIX DỰT DỰT: Reset vật lý khi vừa đẻ ra
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
        controller.enabled = true;
    }

    public void HandleMove(Vector2 input)
    {
        if (controller == null || !controller.enabled) return;

        // 🚨 FIX LỖI XOAY CAMERA: Ép thân xoay theo Camera ngay trong nhịp đập của mạng
        if (Object.HasInputAuthority && Camera.main != null)
        {
            float camYaw = Camera.main.transform.eulerAngles.y;
            transform.rotation = Quaternion.Euler(0, camYaw, 0);
        }

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

        // Đi hướng nào là do cái transform.forward quyết định (Vừa được xoay ở trên xong)
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
        // Phát âm thanh trên máy của tất cả mọi người
        if (audioSource != null && landingClip != null)
        {
            audioSource.PlayOneShot(landingClip);
        }
    }

    public void PlayFootstep()
    {
        // Footstep chạy bằng Animation Event nên chỉ cần chạy Local (Ai cũng tự thấy Hunter bước chân)
        if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(footstepClip, 0.6f);
        }
    }

    public void ApplySlow(float mult)
    {
        currentSpeedMultiplier = mult;
    }

    public void ResetSlow()
    {
        currentSpeedMultiplier = 1f;
    }
}