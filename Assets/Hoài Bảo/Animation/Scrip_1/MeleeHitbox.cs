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
using Fusion;

public class MeleeHitbox : NetworkBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1;
    private Collider hitboxCollider;

    // Lưu trữ: Nhát chém số mấy đã đánh trúng Player nào
    // Key: ID của Player (NetworkInstanceId), Value: ID nhát chém cuối cùng trúng họ
    private Dictionary<NetworkId, int> victimHitHistory = new Dictionary<NetworkId, int>();

    private AttackController ownerController;

    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
        ownerController = GetComponentInParent<AttackController>();
    }

    public void TurnOnHitbox()
    {
        // Không cần Clear danh sách nữa vì chúng ta quản lý theo ID nhát chém
        hitboxCollider.enabled = true;
    }

    public void TurnOffHitbox()
    {
        hitboxCollider.enabled = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        // 1. Chỉ Server mới xử lý trừ máu
        if (!Object.HasStateAuthority) return;

        // 2. Kiểm tra nếu chạm vào Player
        if (other.CompareTag("Player"))
        {
            NetworkObject victimNetObj = other.GetComponent<NetworkObject>();
            if (victimNetObj == null) return;

            int currentAttackId = ownerController.attackCounter;
            NetworkId victimId = victimNetObj.Id;

            // 3. KIỂM TRA CHỐNG DỒN SÁT THƯƠNG:
            // Nếu Player này chưa bao giờ bị đánh, hoặc Nhát chém hiện tại có ID lớn hơn nhát chém cũ đã trúng họ
            if (!victimHitHistory.ContainsKey(victimId) || victimHitHistory[victimId] < currentAttackId)
            {
                // Ghi đè ID nhát chém mới nhất vào "sổ đen" của nạn nhân này
                victimHitHistory[victimId] = currentAttackId;

                Debug.Log($"<color=red>[HIT]</color> Nhát chém #{currentAttackId} trúng {other.name}");

                // --- XỬ LÝ TRỪ MÁU TẠI ĐÂY ---
                // Example: victimNetObj.GetComponent<SurvivorHealth>().TakeDamage(damageAmount);

                // Báo về Controller để phát âm thanh/hiệu ứng trúng đòn
                ownerController.OnHitSuccess(other.gameObject);
            }
            else
            {
                // Nhát chém này đã tính sát thương cho Player này rồi, bỏ qua không cho dồn sát thương
                Debug.Log($"<color=yellow>[STAY]</color> Nhát chém #{currentAttackId} đang chạm {other.name} nhưng đã tính sát thương trước đó.");
            }
        }
    }
}