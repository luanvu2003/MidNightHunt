using UnityEngine;
using UnityEngine.InputSystem;

public class AttackController : MonoBehaviour
{
    private HunterController hunterMovement;
    private Animator ani;

    [Header("Trạng thái")]
    public bool isAttacking = false;

    [Header("Vũ Khí & Tọa Độ Ném")]
    public GameObject hammerPrefab;    // Viên đạn búa bay
    public Transform throwPoint;       // Vị trí đẻ ra búa (Tạo 1 cục rỗng trước Camera)
    [Header("Slow")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.1f; // tốc độ giảm bnh
    public float slowDuraction = 1.1f; // chậm trong bao lâu


    // Phân biệt rõ 2 tay
    public GameObject leftHandHammer;  // Búa ném (Nằm ở xương tay trái)
    public GameObject rightHandHammer; // Búa chém (Nằm ở xương tay phải)

    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animThrow = Animator.StringToHash("Phibua");

    private void Awake()
    {
        ani = GetComponent<Animator>();
        hunterMovement = GetComponent<HunterController>();
    }

    void Update()
    {
        if (isAttacking) return;

        // CHUỘT TRÁI: Chém búa phải
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {

            PerformAttack(animAttack);
        }

        // CHUỘT PHẢI: Phi búa trái
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {

            PerformAttack(animThrow);
        }
    }

    // =========================================================
    // HÀM RA LỆNH MÚA & KÍCH HOẠT HẸN GIỜ BẢO VỆ
    // =========================================================
    private void PerformAttack(int attackTriggerHash)
    {
        isAttacking = true;
        ani.SetTrigger(attackTriggerHash);

        // BÍ KÍP: Hẹn giờ 1.5 giây sau TỰ ĐỘNG mở khóa, 
        // phòng trường hợp Animation Event bị lỗi/bị nuốt mất.
        Invoke(nameof(ForceResetAttack), 1.5f);
    }

    // =========================================================
    // EVENT: Gọi ở giữa lúc vung tay (Chỉ dành cho Phi búa)
    // =========================================================
    public void ReleaseHammer()
    {
        // 1. Giấu cây búa trên tay TRÁI đi 
        if (leftHandHammer != null) leftHandHammer.SetActive(false);

        // 2. Đẻ ra viên đạn búa bay đi
        if (hammerPrefab != null && throwPoint != null)
        {
            Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);
        }
    }

    // =========================================================
    // EVENT: Gọi ở cuối lúc múa xong (Dành cho cả Chém và Phi)
    // =========================================================
    public void ResetAttack()
    {
        isAttacking = false;

        // Hiện lại cây búa tay TRÁI (như kiểu móc búa mới ra)
        if (leftHandHammer != null) leftHandHammer.SetActive(true);
    }

    // =========================================================
    // HÀM DỰ PHÒNG (FAILSAFE)
    // =========================================================
    private void ForceResetAttack()
    {
        // Nếu qua 1.5s mà sếp (Animation Event) chưa chịu mở khóa, thì thằng lính này tự động đập ổ khóa luôn!
        if (isAttacking)
        {
            Debug.LogWarning("Failsafe kích hoạt: Tự động mở khóa Attack do Animation bị kẹt!");
            ResetAttack();
        }
    }
    private void RestoreSpeed()
    {
        if(hunterMovement != null)
        {
            hunterMovement.ResetSlow();
        }
    }
    public void StarSlowEffect()
    {
        if(hunterMovement != null)
        {
            hunterMovement.ApplySlow(slowMultiplier); // đi chậm lại
            CancelInvoke(nameof(RestoreSpeed)); // xóa lệnh hẹn giờ cũ
            Invoke(nameof(RestoreSpeed), slowDuraction); // hẹn giờ di chuyển bth
        }
    }
}