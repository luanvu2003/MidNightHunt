using UnityEngine;
using UnityEngine.InputSystem;

public class FPSCamera : MonoBehaviour
{
    [Header("Cai Dat Camera")]
    public Transform playerBody; 
    
    [SerializeField] private float mouseSensitivity = 15f; 
    public float maxLookAngle = 80f; 

    private float cameraPitch = 0f;
    
    // BIẾN MỚI: Công tắc theo dõi xem chuột đang bị khóa hay thả rông
    private bool isMouseLocked = true; 

    private void Start()
    {
        // Vừa vào game là khóa chuột ngay lập tức
        SetMouseState(true);
    }

    private void Update()
    {
        // =================================================================
        // 1. CÔNG TẮC ẨN/HIỆN CHUỘT BẰNG PHÍM ALT (TRÁI)
        // =================================================================
        if (Keyboard.current.leftAltKey.wasPressedThisFrame)
        {
            // Đảo ngược trạng thái: Đang khóa thành mở, đang mở thành khóa
            isMouseLocked = !isMouseLocked; 
            SetMouseState(isMouseLocked);
        }

        // CHỐT CHẶN QUAN TRỌNG: 
        // Nếu chuột đang MỞ (để rê chuột bấm UI), thì KHÔNG chạy đoạn code xoay Camera bên dưới nữa!
        if (!isMouseLocked) return;

        // =================================================================
        // 2. XOAY CAMERA FPS (Chỉ chạy khi chuột đang bị khóa giam)
        // =================================================================
        Vector2 mouseDelta = Mouse.current.delta.ReadValue();

        // Xoay ngang (Thân)
        float yaw = mouseDelta.x * mouseSensitivity * Time.deltaTime;
        playerBody.Rotate(Vector3.up * yaw);

        // Ngước lên/xuống (Đầu)
        cameraPitch -= mouseDelta.y * mouseSensitivity * Time.deltaTime;
        cameraPitch = Mathf.Clamp(cameraPitch, -maxLookAngle, maxLookAngle);
        transform.localEulerAngles = new Vector3(cameraPitch, 0f, 0f);
    }

    // =================================================================
    // HÀM TIỆN ÍCH: BẬT/TẮT CHUỘT
    // =================================================================
    private void SetMouseState(bool lockMouse)
    {
        if (lockMouse)
        {
            Cursor.lockState = CursorLockMode.Locked; // Giam chuột ở giữa màn hình
            Cursor.visible = false;                   // Tàng hình chuột
        }
        else
        {
            Cursor.lockState = CursorLockMode.None;   // Thả rông chuột
            Cursor.visible = true;                    // Hiện chuột lên
        }
    }

    // Hàm cho UI Slider chọc vào (Giữ nguyên)
    public void SetMouseSensitivity(float newSensitivity)
    {
        mouseSensitivity = newSensitivity;
    }
}