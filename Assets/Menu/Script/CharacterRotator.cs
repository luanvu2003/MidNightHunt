using UnityEngine;
using UnityEngine.EventSystems; // Cần thư viện này để check xem có đang bấm nhầm vào nút UI không

public class CharacterRotator : MonoBehaviour
{
    [Header("Cài đặt Xoay")]
    public float rotationSpeed = 500f; // Tốc độ xoay (Chỉnh ngoài Inspector)
    public bool reverseDirection = false; // Tick vào nếu thấy xoay bị ngược hướng chuột

    private bool isDragging = false;

    void Update()
    {
        // 1. KHI VỪA NHẤN CHUỘT TRÁI XUỐNG
        if (Input.GetMouseButtonDown(0))
        {
            // 🚨 BÙA QUAN TRỌNG: Kiểm tra xem con trỏ chuột có đang đè lên cái Nút UI nào không?
            // Nếu đang bấm Nút (Ví dụ: Nút Sẵn sàng, Nút chọn tướng) -> KHÔNG cho xoay!
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; 
            }

            isDragging = true;
        }

        // 2. KHI THẢ CHUỘT TRÁI RA
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }

        // 3. KHI ĐANG GIỮ VÀ KÉO CHUỘT
        if (isDragging)
        {
            // Lấy khoảng cách rê chuột theo chiều ngang (Trái - Phải)
            float mouseX = Input.GetAxis("Mouse X");

            // Đảo hướng xoay nếu cần
            float direction = reverseDirection ? 1f : -1f;

            // Xoay Object quanh trục Y (Trục dọc, xoay vòng tròn)
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * direction * Time.deltaTime, Space.World);
        }
    }
}