using UnityEngine;

[RequireComponent(typeof(CharacterController), typeof(Animator))] // Bắt buộc phải có 2 cái này
public class HunterMovement : MonoBehaviour
{
    [Header("Cài Đặt Tốc Độ")]
    public float walkStraight = 5f; // Tốc độ tiến tới
    public float walkBackward = 4f; // Tốc độ lùi lại (thường chậm hơn)
    public float currentSpeedMultiplier = 1f; // Hệ số để AttackController gọi vào ép đi chậm

    [Header("Âm Thanh Bước Chân")]
    public AudioSource audioSource; // Cái loa dưới chân
    public AudioClip footstepClip; // File nhạc tiếng chân

    private CharacterController controller; // Ống trụ vật lý của nhân vật
    private Animator animator; // Xương khớp hoạt ảnh
    private float velocityY; // Vận tốc rơi tự do
    private float currentSpeed; // Vận tốc chạy thực tế sau khi tính toán

    private readonly int animSpeed = Animator.StringToHash("Speed"); // Hash parameter Speed để chạy mượt hơn

    private void Awake()
    {
        // Nạp các component vào biến
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>(); // Tự tìm loa nếu quên gắn
    }

    // Hàm này được InputHandler gọi và truyền vector phím bấm (A,D,W,S) vào
    public void HandleMove(Vector2 input)
    {
        // KIỂM TRA: Nếu CharacterController đang bị tắt (lúc đang bay qua cửa sổ), thì thoát ngay để chống lỗi đỏ Console
        if (controller == null || !controller.enabled) return;

        // Tính hướng đi thực tế dựa vào mặt nhân vật đang quay về đâu
        Vector3 move = (transform.forward * input.y + transform.right * input.x).normalized;

        // Tính tốc độ muốn đạt tới
        float targetSpeed = 0f;
        if (input.y < 0) targetSpeed = walkBackward * currentSpeedMultiplier; // Lùi
        else if (move.magnitude > 0) targetSpeed = walkStraight * currentSpeedMultiplier; // Tiến

        // Trọng lực: Ép nhân vật xuống sàn
        if (controller.isGrounded && velocityY < 0) velocityY = -2f; // Ép nhẹ cho dính sàn
        velocityY += -9.81f * Time.deltaTime; // Hút mạnh xuống theo gia tốc trái đất

        // Làm mượt tốc độ (từ từ tăng/giảm tốc)
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f);

        // Thực thi việc di chuyển ống trụ vật lý (TỐC ĐỘ CHẬM THỰC TẾ)
        controller.Move(move * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);

        // =========================================================
        // 🚨 TUYỆT CHIÊU: ĐÁNH LỪA ANIMATOR (FAKE SPEED)
        // =========================================================
        // Khôi phục lại "tốc độ ảo" để Animator không biết là mình đang bị làm chậm.
        // Ví dụ: Đang bị Slow còn 0.5 -> currentSpeed = 2.5
        // Ta lấy 2.5 / 0.5 = 5.0 (Vận tốc gốc). Animator sẽ nhận số 5.0 và múa chân chạy tít thò lò!
        float fakeSpeedForAnimator = currentSpeed;

        // Kiểm tra an toàn: Tránh lỗi toán học chia cho 0
        if (currentSpeedMultiplier > 0f)
        {
            fakeSpeedForAnimator = currentSpeed / currentSpeedMultiplier;
        }

        // Tính % tốc độ DỰA TRÊN TỐC ĐỘ ẢO
        float animValue = fakeSpeedForAnimator / walkStraight;

        // Nếu người chơi bấm S (đi lùi) thì gửi số âm để Animator kéo ngược Animation
        if (input.y < 0) animValue = -animValue;

        // Gửi con số "ảo" này vào Animator
        animator.SetFloat(animSpeed, animValue);
    }
    // --- THÊM HÀM NÀY XUỐNG DƯỚI CÙNG SCRIPT ---
    public void PlayFootstep()
    {
        // SỬA Ở ĐÂY: Dùng currentSpeed (vận tốc thực) thay vì currentSpeedMultiplier
        // Mathf.Abs giúp đảm bảo dù đi lùi (vận tốc âm) thì nó vẫn kêu
        if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
        {
            if (audioSource != null)
            {
                audioSource.PlayOneShot(footstepClip, 0.6f);
            }
        }
    }

    // Các hàm công khai để AttackController gọi vào khi chém trượt / chém trúng
    public void ApplySlow(float mult) => currentSpeedMultiplier = mult; // Ép chậm
    public void ResetSlow() => currentSpeedMultiplier = 1f; // Hết chậm
}