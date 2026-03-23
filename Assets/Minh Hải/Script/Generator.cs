using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 100f; // Đổi thành 100 để khớp với maxProgress của SkillCheck
    public float progress = 0f;
    public float interactRadius = 3f;

    [Header("GameObject")]
    public Transform player;
    private bool playerInRange = false;
    private bool isRepaired = false;
    private bool isRepairing = false; // Biến kiểm soát trạng thái sửa máy

    public SkillCheck skillCheck;
    public Slider progressBar;
    public GameObject repairText;
    public ParticleSystem explosionFX;

    [Header("Visual Effects & Audio")]
    public GameObject repairedLight;
    public Animator animator;
    public AudioSource explosionSound;
    public AudioSource repairSound;

    void Start()
    {
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
        skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        // 1. NHẤN E ĐỂ BẮT ĐẦU HOẶC DỪNG SỬA
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            isRepairing = !isRepairing;
            
            if (isRepairing)
            {
                progressBar.gameObject.SetActive(true);
                // Bắt đầu mini-game ngay lập tức khi vừa bấm E
                if (!skillCheck.gameObject.activeSelf)
                    skillCheck.StartNewSkillCheck(this);
            }
            else
            {
                StopRepairing();
            }
        }

        // 2. XỬ LÝ LOGIC KHI ĐANG SỬA
        if (isRepairing && playerInRange)
        {
            UpdateVisuals(true);
            
            // Thanh slider bây giờ chỉ phụ thuộc vào giá trị progress (do SkillCheck cộng vào)
            progressBar.value = progress / repairTime;

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
        else if (isRepairing && !playerInRange) 
        {
            // Tự động dừng nếu người chơi đi quá xa
            StopRepairing();
        }
    }

    void StopRepairing()
    {
        isRepairing = false;
        UpdateVisuals(false);
        skillCheck.gameObject.SetActive(false); // Tắt mini-game khi rời máy
        progressBar.gameObject.SetActive(false);
    }

    void UpdateVisuals(bool running)
    {
        if (animator != null) animator.SetBool("isRunning", running);
        if (repairSound != null)
        {
            if (running && !repairSound.isPlaying) repairSound.Play();
            else if (!running && repairSound.isPlaying) repairSound.Stop();
        }
    }

    void FinishRepair()
    {
        isRepaired = true;
        isRepairing = false;
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
        UpdateVisuals(false);
        if (repairedLight != null) repairedLight.SetActive(true);
        Debug.Log("Máy đã sửa xong!");
    }

    // Giữ nguyên OnTriggerEnter và OnTriggerExit nhưng xóa logic SetActive(false) của progressBar ở Exit 
    // vì đã có hàm StopRepairing xử lý.
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
        }
    }
}