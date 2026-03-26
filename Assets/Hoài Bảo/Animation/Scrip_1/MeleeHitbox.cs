using System.Collections.Generic;
using UnityEngine;

public class MeleeHitbox : MonoBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1; // Sát thương 1 nhát chém
    private Collider hitboxCollider;

    // ĐÂY LÀ BÍ QUYẾT: Cuốn "Sổ đen" ghi nhớ những Player đã ăn đòn trong 1 nhát chém
    private List<GameObject> alreadyHitPlayers = new List<GameObject>();

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true; // Bắt buộc phải là Trigger
        hitboxCollider.enabled = false;  // Tắt hitbox đi khi mới vào game
    }

    // Hàm này sẽ được AttackController gọi khi Hunter bắt đầu vung tay
    public void TurnOnHitbox()
    {
        alreadyHitPlayers.Clear(); // Xóa sạch sổ đen của nhát chém trước
        hitboxCollider.enabled = true; // Bật vùng sát thương lên
    }

    // Hàm này sẽ được gọi khi Hunter vung tay xong
    public void TurnOffHitbox()
    {
        hitboxCollider.enabled = false; // Tắt vùng sát thương
    }

    // Radar cảm biến va chạm
    private void OnTriggerEnter(Collider other)
    {
        // 1. Nếu va trúng cái gì đó có Tag là "Player"
        if (other.CompareTag("Player"))
        {
            // 2. Kiểm tra xem thằng Player này đã bị chém trong nhát này chưa?
            if (!alreadyHitPlayers.Contains(other.gameObject))
            {
                // 3. Nếu chưa -> Ghi tên nó vào sổ đen ngay lập tức
                alreadyHitPlayers.Add(other.gameObject);

                // 4. THỰC HIỆN TRỪ MÁU Ở ĐÂY
                Debug.Log("💥 Đã chém trúng và trừ máu: " + other.gameObject.name);
                
                // (Sau này bạn có Script máu của Player thì gọi vào đây)
                // Ví dụ: other.GetComponent<PlayerHealth>().TakeDamage(damageAmount);
            }
        }
    }
}