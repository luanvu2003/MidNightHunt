using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 60f;
    public float progress = 0f;

    public int currentSkillLevel = 1;

    [Header("Penalty Settings")]
    public float stunDuration = 10f;
    private float stunTimer = 0f;
    public float progressPenalty = 5f;
    
    // 🚨 BIẾN MỚI CHO HUNTER ĐẠP MÁY
    public float hunterDamageAmount = 15f; 
    public bool isDamagedByHunter = false; // Cờ đánh dấu máy đã bị đạp chưa

    [Header("Player Settings")]
    public Transform player;
    public MonoBehaviour playerMovementScript;

    private bool playerInRange = false;
    private int zonesOccupied = 0;

    private bool isRepaired = false;
    public bool isRepairing = false;

    [Header("UI & Minigame")]
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
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0 && playerInRange)
            {
                if (repairText != null) repairText.SetActive(true);
            }
        }

        // 1. NHẤN E ĐỂ BẮT ĐẦU HOẶC DỪNG SỬA
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && stunTimer <= 0)
        {
            if (isRepairing)
            {
                if (skillCheck.isChecking) ExplodeGenerator();
                else StopRepairing();
            }
            else
            {
                isRepairing = true;
                
                // 🚨 KHI SURVIVOR QUAY LẠI SỬA: XÓA CỜ "BỊ ĐẠP"
                isDamagedByHunter = false;

                if (playerMovementScript != null) playerMovementScript.enabled = false;

                if (progressBar != null) progressBar.gameObject.SetActive(true);
                if (skillCheck != null && !skillCheck.gameObject.activeSelf)
                    skillCheck.StartNewSkillCheck(this);
            }
        }

        // 2. XỬ LÝ LOGIC KHI ĐANG SỬA
        if (isRepairing && playerInRange)
        {
            UpdateVisuals(true);

            progress += Time.deltaTime;
            if (progressBar != null) progressBar.value = progress / repairTime;

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
        else if (isRepairing && !playerInRange)
        {
            if (skillCheck.isChecking) ExplodeGenerator();
            else StopRepairing();
        }
    }

    public void ApplyStun()
    {
        currentSkillLevel = 1;
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();

        AlertNearbyCrows(); 

        StopRepairing();
        stunTimer = stunDuration;
        if (repairText != null) repairText.SetActive(false);
    }

    public void ExplodeGenerator()
    {
        currentSkillLevel = 1;
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();

        AlertNearbyCrows(); 

        progress = Mathf.Max(0, progress - progressPenalty);
        if (progressBar != null) progressBar.value = progress / repairTime;

        StopRepairing();
    }
    
    // =========================================================
    // 🚨 HÀM MỚI: ĐƯỢC GỌI BỞI HUNTER KHI ĐẠP MÁY
    // =========================================================
    public void DamageByHunter()
    {
        // 1. Trừ 15 giây tiến trình
        progress = Mathf.Max(0, progress - hunterDamageAmount);
        
        // 2. Bật cờ đánh dấu đã đạp (để Hunter không đạp được nữa)
        isDamagedByHunter = true;

        // 3. Hiệu ứng xẹt lửa, âm thanh nổ
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();
        
        AlertNearbyCrows();
        
        // (Tùy chọn) Bắt Survivor văng ra nếu đang ngồi sửa
        if (isRepairing) 
        {
            StopRepairing();
            // Nếu muốn ác hơn thì gọi ApplyStun() ở đây luôn
        }
    }

    // =========================================================
    // 🚨 HÀM KIỂM TRA ĐIỀU KIỆN ĐẠP (CHO HUNTER)
    // =========================================================
    public bool CanBeDamagedByHunter()
    {
        // Hunter CHỈ ĐƯỢC đạp khi:
        // 1. Máy chưa sửa xong
        // 2. Tiến trình lớn hơn 0 (Đã có đứa động vào)
        // 3. Máy chưa bị đạp (hoặc Survivor đã quay lại sửa để xóa cờ)
        return !isRepaired && progress > 0f && !isDamagedByHunter;
    }


    public void StopRepairing()
    {
        isRepairing = false;
        if (playerMovementScript != null) playerMovementScript.enabled = true;
        UpdateVisuals(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
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

        if (GameManager.Instance != null) {
            GameManager.Instance.GeneratorFixed(); 
        }

        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (progressBar != null)
        {
            progressBar.value = 1f;
            progressBar.gameObject.SetActive(false);
        }

        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        UpdateVisuals(false);

        if (repairedLight != null) repairedLight.SetActive(true);

        RepairZone[] zones = GetComponentsInChildren<RepairZone>();
        foreach (RepairZone zone in zones)
        {
            Collider col = zone.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    public void PlayerEnteredZone()
    {
        if (isRepaired) return;
        zonesOccupied++;
        if (zonesOccupied > 0)
        {
            playerInRange = true;
            if (stunTimer <= 0)
            {
                if (repairText != null) repairText.SetActive(true);
            }
        }
    }

    public void PlayerExitedZone()
    {
        zonesOccupied--;
        if (zonesOccupied <= 0)
        {
            zonesOccupied = 0;
            playerInRange = false;
            if (repairText != null) repairText.SetActive(false);
        }
    }

    private void AlertNearbyCrows()
    {
        float noiseRadius = 20f; 
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, noiseRadius);
        foreach (var hitCollider in hitColliders)
        {
            CrowAI crow = hitCollider.GetComponent<CrowAI>();
            if (crow != null)
            {
                crow.OnGeneratorExplosion();
            }
        }
    }
}