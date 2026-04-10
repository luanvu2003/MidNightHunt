// using UnityEngine;

// public class BearTrap : MonoBehaviour
// {
//     [Header("Thông tin chủ nhân")]
//     public AttackController ownerHunter; // Sẽ được Hunter gán tự động khi đẻ ra

//     private bool isSprung = false;

//     private void OnTriggerEnter(Collider other)
//     {
//         // Dòng này sẽ báo cáo MỌI THỨ đâm vào bẫy (mặt đất, hunter, cục đá...)
//         Debug.Log("🔍 Có vật thể vừa đâm vào bẫy: " + other.gameObject.name + " | Mang Tag: " + other.tag);

//         if (!isSprung && other.CompareTag("Player"))
//         {
//             // BẠN QUÊN BỎ COMMENT DÒNG NÀY RỒI NÈ (Nhớ xóa dấu // đi nhé)
//             isSprung = true;

//             Debug.Log("🐻 PHẬP! Đã bắt được Survivor: " + other.name);

//             if (ownerHunter != null)
//             {
//                 ownerHunter.RecoverTrap();
//             }
//         }
//     }

//     // NÂNG CẤP: Nếu bạn có chức năng Survivor cúi xuống gỡ bẫy, 
//     // bạn chỉ cần gọi hàm này từ script của Survivor
//     public void OnTrapDestroyedBySurvivor()
//     {
//         if (!isSprung)
//         {
//             isSprung = true;
//             Debug.Log("🔧 Bẫy đã bị Survivor tháo gỡ!");

//             // Vẫn trả lại bẫy cho túi đồ của Hunter
//             if (ownerHunter != null)
//             {
//                 ownerHunter.RecoverTrap();
//             }

//             // Hủy cái bẫy trên mặt đất
//             Destroy(gameObject);
//         }
//     }
// }

using UnityEngine;
using Fusion;

public class BearTrap : NetworkBehaviour
{
    [Header("Thông tin chủ nhân")]
    public AttackController ownerHunter;

    [Header("Cài đặt Bẫy")]
    [Tooltip("Thời gian tự động hủy bẫy nếu không ai dẫm (giây)")]
    public float autoDestroyTime = 100f;

    // Đồng bộ cờ trạng thái qua mạng
    [Networked] private NetworkBool isSprung { get; set; }
    
    // Bộ đếm thời gian an toàn trên mạng (TickTimer)
    [Networked] private TickTimer lifeTimer { get; set; }

    public override void Spawned()
    {
        // Khi bẫy vừa được đẻ ra, Server sẽ bắt đầu đếm giờ
        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, autoDestroyTime);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // CHỈ SERVER mới kiểm tra thời gian tồn tại
        if (!Object.HasStateAuthority) return;

        // Nếu hết 100 giây mà bẫy vẫn chưa kích hoạt -> Tự động xóa bẫy
        if (!isSprung && lifeTimer.Expired(Runner))
        {
            Debug.Log("⏳ Bẫy đã hết thời gian tồn tại (100s) và tự phân hủy.");
            Runner.Despawn(Object);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 🚨 CHỈ SERVER mới có quyền kiểm tra và quyết định ai dẫm bẫy
        if (!Object.HasStateAuthority) return;

        if (!isSprung && other.CompareTag("Player"))
        {
            isSprung = true;

            Debug.Log("🐻 PHẬP! Đã bắt được Survivor: " + other.name);

            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }
            
            // TODO: Gọi script của Survivor để giữ chân, trừ máu họ lại tại đây
            // VD: other.GetComponent<SurvivorHealth>().TakeDamage();

            // 🚨 ĐÃ THÊM: Xóa bẫy (Despawn) ngay sau khi Player dẫm trúng
            Runner.Despawn(Object);
        }
    }

    public void OnTrapDestroyedBySurvivor()
    {
        if (!Object.HasStateAuthority) return;

        if (!isSprung)
        {
            isSprung = true;
            Debug.Log("🔧 Bẫy đã bị Survivor tháo gỡ!");

            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }

            Runner.Despawn(Object);
        }
    }
}