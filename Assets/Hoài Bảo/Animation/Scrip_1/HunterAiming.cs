using UnityEngine;

public class HunterAiming : MonoBehaviour
{
    [Header("Xương Cột Sống (Spine/Chest)")]
    // Bạn kéo cục xương ngực (Ví dụ: mixamorig:Spine1 hoặc mixamorig:Spine2) vào đây
    public Transform spineBone; 

    // 🚨 BẮT BUỘC PHẢI DÙNG LATEUPDATE
    // Animation chạy ở Update. Nếu bẻ xương ở Update, Animation sẽ đè lên làm nó thẳng lại.
    // LateUpdate chạy sau cùng, nó sẽ "đè" lên Animation để bẻ cong theo ý mình!
    private void LateUpdate()
    {
        if (spineBone == null || Camera.main == null) return;

        // 1. Lấy góc ngửa/cúi hiện tại của Camera (Trục X)
        float camPitch = Camera.main.transform.eulerAngles.x;

        // Xử lý toán học của Unity: 
        // Khi ngửa mặt lên trời, Unity tính là 270 đến 360 độ. Ta quy đổi nó về số âm (Ví dụ: -45 độ).
        if (camPitch > 180f) camPitch -= 360f;

        // 2. Ép xương sống cộng thêm một góc đúng bằng góc ngửa của Camera
        // Lưu ý: Đa số mô hình Mixamo dùng trục X (local) để gập người tới lui.
        spineBone.localEulerAngles += new Vector3(camPitch, 0f, 0f);
    }
}