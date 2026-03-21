using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 10f;
    public float progress = 0f;

    public float interactRadius = 3f;

    [Header("GameObject")]
    public Transform player;

    private bool playerInRange = false;
    private bool isRepaired = false;
    
    // Ghi nhớ trạng thái Minigame đang bị tạm dừng
    private bool isSkillCheckPaused = false; 

    public SkillCheck skillCheck;

    public ParticleSystem explosionFX; // Hiệu ứng
    public AudioSource explosionSound;  // Audio

    float skillTimer = 5f;   // thời gian xuất hiện skill check

    public Slider progressBar;
    public GameObject repairText;

    void Start()
    {
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);

        skillCheck.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        // 1. NẾU SKILL CHECK ĐANG BẬT TRÊN MÀN HÌNH
        if (skillCheck.gameObject.activeSelf) 
        {
            // Nếu người chơi thả phím E HOẶC đi ra xa -> Tạm dừng và ẩn nó đi
            if (!playerInRange || !Input.GetKey(KeyCode.E))
            {
                skillCheck.gameObject.SetActive(false); // Ẩn UI minigame
                isSkillCheckPaused = true;              // Đánh dấu là đang tạm dừng
            }
            return; // Khóa toàn bộ logic sửa máy bên dưới
        }

        // 2. KHI ĐANG GIỮ PHÍM E VÀ Ở TRONG PHẠM VI
        if (playerInRange && Input.GetKey(KeyCode.E))
        {
            // NẾU CÓ SKILL CHECK ĐANG TẠM DỪNG -> MỞ LẠI
            if (isSkillCheckPaused)
            {
                skillCheck.gameObject.SetActive(true); // Bật lại UI (kim ở nguyên vị trí cũ)
                isSkillCheckPaused = false;            // Xóa trạng thái tạm dừng
                return; 
            }
            
            progressBar.gameObject.SetActive(true);

            progress += Time.deltaTime; // Sửa máy -> Tăng tiến độ
            progressBar.value = progress / repairTime;

            // Đếm thời gian skill check
            skillTimer -= Time.deltaTime;

            if (skillTimer <= 0)
            {
                // Gọi hàm StartNewSkillCheck (để reset kim về 0)
                skillCheck.StartNewSkillCheck(this); 

                skillTimer = Random.Range(5f, 10f);
            }

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
        // 3. KHI THẢ PHÍM E (HOẶC CHẠY RA KHỎI PHẠM VI)
        else 
        {
            // Đã xóa phần code tụt tiến độ (decayRate). 
            // Bây giờ khi thả E, tiến độ sẽ được giữ nguyên không tăng không giảm.
            
            // Tùy chọn: Nếu bạn muốn ẩn luôn thanh UI khi thả tay ra (nhưng vẫn giữ điểm) thì bỏ comment dòng dưới:
            // progressBar.gameObject.SetActive(false);
        }
    }

    void FinishRepair()
    {
        isRepaired = true;

        Debug.Log("Generator đã sửa xong!");

        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isRepaired)
        {
            playerInRange = true;
            repairText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            repairText.SetActive(false);
            progressBar.gameObject.SetActive(false);
        }
    }
}