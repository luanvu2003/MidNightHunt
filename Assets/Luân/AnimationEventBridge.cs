using UnityEngine;

public class AnimationEventBridge : MonoBehaviour
{
    // Hàm này sẽ bắt Animation Event từ cục Model con
    public void PlayFootstepSound()
    {
        // Gửi tín hiệu gọi hàm này lên TẤT CẢ các script nằm ở Object cha
        // Lệnh này cực kỳ tiện vì không cần quan tâm script cha tên là MrBeast hay IShowSpeed
        SendMessageUpwards("PlayFootstepSound", SendMessageOptions.DontRequireReceiver);
    }
}