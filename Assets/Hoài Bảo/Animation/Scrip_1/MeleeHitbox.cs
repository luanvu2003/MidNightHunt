using System.Collections.Generic;
using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1; // Số máu trừ mỗi nhát chém
    private Collider hitboxCollider;

    // =========================================================
    // 📓 CUỐN SỔ ĐEN: Ghi nhớ những ai đã bị chém trong nhát này
    // =========================================================
    private List<GameObject> alreadyHitPlayers = new List<GameObject>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true; // Bắt buộc là Trigger
        hitboxCollider.enabled = false;  // Tắt đi khi mới vào game
    }

    // Hàm này được AttackController gọi khi bắt đầu vung tay
    public void TurnOnHitbox()
    {
        // 🚨 QUAN TRỌNG: Vừa vung nhát mới là phải XÓA SẠCH sổ đen của nhát cũ
        alreadyHitPlayers.Clear(); 
        hitboxCollider.enabled = true; 
    }

    // Hàm này được AttackController gọi khi vung tay xong
    public void TurnOffHitbox()
    {
        hitboxCollider.enabled = false; 
    }

    // =========================================================
    // CẢM BIẾN VA CHẠM: NƠI XỬ LÝ CHỐNG TRỪ MÁU ĐỀ (SPAM DAMAGE)
    // =========================================================
    private void OnTriggerEnter(Collider other)
    {
        // 1. Nếu cái chạm vào là Player
        if (other.CompareTag("Player"))
        {
            // 2. Kiểm tra xem tên nó đã có trong Sổ Đen chưa?
            // Nếu chưa có (nghĩa là nhát này nó chưa bị chém):
            if (!alreadyHitPlayers.Contains(other.gameObject))
            {
                // 3. THÊM NGAY VÀO SỔ ĐEN ĐỂ KHÓA LẠI
                alreadyHitPlayers.Add(other.gameObject);

                // 4. THỰC HIỆN TRỪ MÁU
                Debug.Log("💥 BÙM! Đã chém trúng và trừ 1 máu của: " + other.gameObject.name);
                
                // --- ĐOẠN GỌI SANG SCRIPT MÁU CỦA PLAYER (Bạn mở comment khi có script Player nhé) ---
                // Survivor survivorStats = other.GetComponent<Survivor>();
                // if (survivorStats != null)
                // {
                //     survivorStats.TakeDamage(damageAmount);
                // }
            }
        }
    }
}