using UnityEngine;
using System.Collections.Generic;

public class VomitDamage : MonoBehaviour
{
    [Header("Cài đặt Sát thương Độc")]
    public int poisonDamage = 1;
    
    [Header("Chống sát thương dồn dập (Spam)")]
    public float damageCooldown = 0.5f; // Cứ 0.5 giây mới bị trừ máu 1 lần
    
    // Cuốn sổ đen ghi lại thời gian lần cuối nạn nhân bị trúng độc
    private Dictionary<GameObject, float> lastHitTimes = new Dictionary<GameObject, float>();

    // 🚨 HÀM MA THUẬT: Tự động chạy khi có BẤT KỲ 1 hạt Particle nào đập trúng vật thể
    private void OnParticleCollision(GameObject other)
    {
        // 1. Chỉ quan tâm nếu hạt nôn văng trúng Survivor (Tag "Player")
        if (other.CompareTag("Player"))
        {
            float currentTime = Time.time;

            // 2. Kiểm tra xem nạn nhân này đã hết thời gian miễn nhiễm tạm thời chưa?
            // (Vì 1 giây có hàng trăm hạt nôn bay vào mặt, nếu không chặn lại thì Survivor chết trong 0.1 giây)
            if (!lastHitTimes.ContainsKey(other) || currentTime - lastHitTimes[other] >= damageCooldown)
            {
                // Cập nhật lại thời gian dính đòn mới nhất
                lastHitTimes[other] = currentTime;

                Debug.Log("🤮 Hạt độc đã văng trúng mặt: " + other.name);

                // =========================================================
                // 3. GỌI HÀM TRỪ MÁU TỪ SCRIPT CỦA SURVIVOR Ở ĐÂY
                // =========================================================
                // SurvivorHealth survivor = other.GetComponent<SurvivorHealth>();
                // if (survivor != null)
                // {
                //     survivor.TakeDamage(poisonDamage);
                // }
            }
        }
    }
}