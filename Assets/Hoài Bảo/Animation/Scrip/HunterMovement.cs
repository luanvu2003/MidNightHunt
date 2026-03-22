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
        
        // Thực thi việc di chuyển ống trụ vật lý
        controller.Move(move * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);

        // Gửi thông số vận tốc cho Animator để múa chân
        float animValue = currentSpeed / walkStraight;
        if (input.y < 0) animValue = -animValue; // Đi lùi thì gửi số âm
        animator.SetFloat(animSpeed, animValue);

        // Phát âm thanh tiếng bước chân nếu đang ở trên đất và có đi lại
        if (controller.isGrounded && currentSpeed > 0.5f && footstepClip != null && !audioSource.isPlaying)
        {
            audioSource.PlayOneShot(footstepClip); // Phát 1 lần file âm thanh
        }
    }

    // Các hàm công khai để AttackController gọi vào khi chém trượt / chém trúng
    public void ApplySlow(float mult) => currentSpeedMultiplier = mult; // Ép chậm
    public void ResetSlow() => currentSpeedMultiplier = 1f; // Hết chậm
}