using UnityEngine; // Khai báo thư viện Unity
using UnityEngine.InputSystem; // Sử dụng hệ thống Input System mới

public class HunterInputHandler : MonoBehaviour
{
    private HunterMovement moveScript; // Tham chiếu đến script quản lý đôi chân (Di chuyển)
    private HunterInteraction interactScript; // Tham chiếu đến script quản lý đôi tay (Tương tác)
    private AttackController attackScript; // Tham chiếu đến script quản lý vũ khí (Tấn công)
    private HunterControllerInput actions; // Bản thiết kế phím bấm từ Input System

    private void Awake()
    {
        // Khi game vừa bật, tự động tìm các script đang gắn trên cùng nhân vật này
        moveScript = GetComponent<HunterMovement>();
        interactScript = GetComponent<HunterInteraction>();
        attackScript = GetComponent<AttackController>();
        
        // Khởi tạo bộ đọc phím
        actions = new HunterControllerInput();
    }

    private void OnEnable() => actions.Enable(); // Bật lắng nghe phím khi object này active
    private void OnDisable() => actions.Disable(); // Tắt lắng nghe phím khi object này bị ẩn

    private void Update()
    {
        // 1. CHỐT CHẶN AN TOÀN: Kiểm tra xem Hunter có đang múa (tương tác) không?
        if (interactScript != null && interactScript.IsDoingAction())
        {
            // Nếu đang bận múa hoặc lướt qua cửa sổ -> Gửi số 0 để ép nhân vật đứng im
            moveScript.HandleMove(Vector2.zero); 
        }
        else
        {
            // 2. DI CHUYỂN: Nếu đang rảnh, đọc phím WASD và gửi dữ liệu sang cho đôi chân
            Vector2 moveInput = actions.HunterControllerS.Move.ReadValue<Vector2>();
            moveScript.HandleMove(moveInput);
        }

        // 3. TƯƠNG TÁC: Đọc phím Space
        if (Keyboard.current.spaceKey.wasPressedThisFrame) 
        {
            interactScript.TryInteract(); // Ra lệnh cho đôi tay thử làm việc (nếu có đồ vật ở gần)
        }
        
        // 4. TẤN CÔNG: Đọc phím chuột trái và phải
        if (attackScript != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame) attackScript.PerformAttackLeft(); // Chém búa
            if (Mouse.current.rightButton.wasPressedThisFrame) attackScript.PerformAttackRight(); // Phi búa
        }
    }
}