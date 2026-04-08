// using System.Collections.Generic;
// using UnityEngine;

// public class MeleeHitbox : MonoBehaviour
// {
//     [Header("Cài đặt Sát thương")]
//     public int damageAmount = 1; // Số máu trừ mỗi nhát chém
//     private Collider hitboxCollider;

//     // =========================================================
//     // 📓 CUỐN SỔ ĐEN: Ghi nhớ những ai đã bị chém trong nhát này
//     // =========================================================
//     private List<GameObject> alreadyHitPlayers = new List<GameObject>();

//     private void Awake()
//     {
//         hitboxCollider = GetComponent<Collider>();
//         hitboxCollider.isTrigger = true; // Bắt buộc là Trigger
//         hitboxCollider.enabled = false;  // Tắt đi khi mới vào game
//     }

//     // Hàm này được AttackController gọi khi bắt đầu vung tay
//     public void TurnOnHitbox()
//     {
//         // 🚨 QUAN TRỌNG: Vừa vung nhát mới là phải XÓA SẠCH sổ đen của nhát cũ
//         alreadyHitPlayers.Clear();
//         hitboxCollider.enabled = true;
//     }

//     // Hàm này được AttackController gọi khi vung tay xong
//     public void TurnOffHitbox()
//     {
//         hitboxCollider.enabled = false;
//     }

//     // =========================================================
//     // CẢM BIẾN VA CHẠM: NƠI XỬ LÝ CHỐNG TRỪ MÁU ĐỀ (SPAM DAMAGE)
//     // =========================================================
//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Player"))
//         {
//             if (!alreadyHitPlayers.Contains(other.gameObject))
//             {
//                 alreadyHitPlayers.Add(other.gameObject);

//                 // 1. Tìm script Survivor để trừ máu
//                 // Survivor survivor = other.GetComponent<Survivor>();
//                 // if (survivor != null)
//                 // {
//                 //     survivor.TakeDamage(damageAmount);
//                 // }

//                 // 2. Báo về AttackController để phát âm thanh trúng đòn
//                 // Chúng ta tìm script này trên cha của Hitbox (thường là Hunter)
//                 AttackController controller = GetComponentInParent<AttackController>();
//                 if (controller != null)
//                 {
//                     controller.OnHitSuccess(other.gameObject);
//                 }
//             }
//         }
//     }
// }
using System.Collections.Generic;
using UnityEngine;
using Fusion; // 1. Thêm thư viện Fusion

public class MeleeHitbox : NetworkBehaviour // 2. Kế thừa NetworkBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1; // Số máu trừ mỗi nhát chém
    private Collider hitboxCollider;

    // =========================================================
    // 📓 CUỐN SỔ ĐEN: Không cần [Networked] vì chỉ Server dùng nó
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
        // 3. CHỐT CHẶN MẠNG: CHỈ SERVER MỚI ĐƯỢC PHÉP TÍNH TOÁN VA CHẠM TRỪ MÁU
        // (Nếu máy Client cũng tự chạy Trigger này, nó sẽ trừ máu 2 lần)
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Player"))
        {
            if (!alreadyHitPlayers.Contains(other.gameObject))
            {
                alreadyHitPlayers.Add(other.gameObject);

                // 1. Tìm script Survivor để trừ máu
                // Survivor survivor = other.GetComponent<Survivor>();
                // if (survivor != null)
                // {
                //     survivor.TakeDamage(damageAmount);
                // }

                // 2. Báo về AttackController để phát âm thanh trúng đòn
                // Chúng ta tìm script này trên cha của Hitbox (thường là Hunter)
                AttackController controller = GetComponentInParent<AttackController>();
                if (controller != null)
                {
                    controller.OnHitSuccess(other.gameObject);
                }
            }
        }
    }
}