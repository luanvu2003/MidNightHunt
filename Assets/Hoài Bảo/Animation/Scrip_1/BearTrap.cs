using UnityEngine;

public class BearTrap : MonoBehaviour
{
    [Header("Cài đặt Bẫy")]
    public float trapDuration = 3f; // Thời gian Player bị khóa chân (giây)
    public int damage = 1; // Lượng máu trừ đi khi đạp bẫy
    
    [Header("Âm thanh & Hiệu ứng")]
    public AudioClip snapSound; // Tiếng bẫy sập
    private AudioSource audioSource;
    private Animator trapAnimator; // (Tùy chọn) Hoạt ảnh cái bẫy đóng lại
    
    private bool isTriggered = false; // Bẫy đã sập chưa?

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        trapAnimator = GetComponent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        // Lọc: Chỉ sập khi chạm vào Player, và bẫy chưa sập lần nào
        if (!isTriggered && other.CompareTag("Player"))
        {
            TriggerTrap(other.gameObject);
        }
    }

    private void TriggerTrap(GameObject victim)
    {
        isTriggered = true; // Khóa bẫy, không cho sập 2 lần

        // 1. Phát âm thanh cái PHẬP thật to để báo hiệu cho Hunter
        if (snapSound != null && audioSource != null)
        {
            audioSource.PlayOneShot(snapSound);
        }

        // 2. Chạy hoạt ảnh bẫy gấu kẹp lại (nếu bạn có làm Animator cho cái bẫy)
        if (trapAnimator != null) trapAnimator.SetTrigger("Snap");

        // 3. XỬ LÝ NẠN NHÂN (Ép tốc độ Player về 0)
        Debug.Log("Player đã đạp trúng bẫy gấu!");
        
        // GIẢ SỬ: Bạn gọi script di chuyển của Player ra và ép tốc độ nó về 0
        // PlayerMovement playerMove = victim.GetComponent<PlayerMovement>();
        // if (playerMove != null) playerMove.RootPlayer(trapDuration);

        // TODO: Trừ máu Player ở đây

        // 4. Tiêu hủy cái bẫy sau khi nạn nhân thoát ra (hoặc để đó vĩnh viễn tùy luật game của bạn)
        Destroy(gameObject, trapDuration + 1f);
    }
}