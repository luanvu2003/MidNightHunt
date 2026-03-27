using UnityEngine;

public class BearTrap : MonoBehaviour
{
    [Header("Thông tin chủ nhân")]
    public AttackController ownerHunter; // Sẽ được Hunter gán tự động khi đẻ ra

    private bool isSprung = false;

    private void OnTriggerEnter(Collider other)
    {
        // Dòng này sẽ báo cáo MỌI THỨ đâm vào bẫy (mặt đất, hunter, cục đá...)
        Debug.Log("🔍 Có vật thể vừa đâm vào bẫy: " + other.gameObject.name + " | Mang Tag: " + other.tag);

        if (!isSprung && other.CompareTag("Player"))
        {
            // BẠN QUÊN BỎ COMMENT DÒNG NÀY RỒI NÈ (Nhớ xóa dấu // đi nhé)
            isSprung = true;

            Debug.Log("🐻 PHẬP! Đã bắt được Survivor: " + other.name);

            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }
        }
    }

    // NÂNG CẤP: Nếu bạn có chức năng Survivor cúi xuống gỡ bẫy, 
    // bạn chỉ cần gọi hàm này từ script của Survivor
    public void OnTrapDestroyedBySurvivor()
    {
        if (!isSprung)
        {
            isSprung = true;
            Debug.Log("🔧 Bẫy đã bị Survivor tháo gỡ!");

            // Vẫn trả lại bẫy cho túi đồ của Hunter
            if (ownerHunter != null)
            {
                ownerHunter.RecoverTrap();
            }

            // Hủy cái bẫy trên mặt đất
            Destroy(gameObject);
        }
    }
}