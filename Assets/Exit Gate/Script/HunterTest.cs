using UnityEngine;
using System.Collections;

public class HunterTest : MonoBehaviour
{
    [Header("Status")]
    public bool isStunned = false;
    public float currentStunDuration = 0f;

    // ĐÂY LÀ HÀM QUAN TRỌNG: Pallet sẽ gọi hàm này qua GetComponent
    public void GetStunned(float duration)
    {
        if (isStunned) return; // Nếu đang bị choáng rồi thì thôi (tránh đè stun)

        currentStunDuration = duration;
        StartCoroutine(StunRoutine(duration));
    }

    IEnumerator StunRoutine(float time)
    {
        isStunned = true;
        
        // Hiện thông báo màu ĐỎ rực trên Console để bạn dễ thấy
        Debug.Log($"<color=red><b>[HUNTER]:</b> ĐÃ BỊ ĂN VÁN! Choáng trong {time} giây.</color>");

        // Đổi màu Cube sang đỏ để trực quan hơn (tùy chọn)
        Renderer renderer = GetComponent<Renderer>();
        Color originalColor = Color.white;
        if (renderer != null)
        {
            originalColor = renderer.material.color;
            renderer.material.color = Color.red;
        }

        yield return new WaitForSeconds(time);

        // Hết choáng
        isStunned = false;
        if (renderer != null) renderer.material.color = originalColor;

        Debug.Log("<color=white><b>[HUNTER]:</b> Đã hết choáng, bắt đầu đuổi tiếp!</color>");
    }
}