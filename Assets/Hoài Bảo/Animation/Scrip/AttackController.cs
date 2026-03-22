using UnityEngine; 
using TMPro; 
using UnityEngine.UI; 

public class AttackController : MonoBehaviour
{
    private HunterMovement movementScript; // Để điều chỉnh tốc độ chạy khi múa
    private HunterInteraction interactionScript; // ĐÃ THÊM: Để đọc xem vai Hunter có đang vác xác không
    private Animator ani; 

    [Header("Âm thanh Chiến đấu")]
    public AudioSource attackSource; 
    public AudioClip clipChemBua; 
    public AudioClip clipPhiBua;  

    [Header("Trạng thái")]
    public bool isAttacking = false; 

    [Header("Hệ thống Ném Búa (Ammo & Cooldown)")]
    public int maxAmmo = 5;               
    private int currentAmmo;              
    public float reloadTime = 5f;         
    private bool isReloading = false;     
    private float reloadTimer = 0f;       

    [Header("UI Búa")]
    public TextMeshProUGUI ammoText;      
    public Image cooldownImage;           

    [Header("Vũ Khí & Tọa Độ Ném")]
    public GameObject hammerPrefab;    
    public Transform throwPoint;       

    [Header("Hiệu ứng Làm chậm (Slow)")]
    [Range(0f, 1f)] 
    public float slowMultiplier = 0.1f; 
    public float slowDuraction = 1.1f;  

    [Header("Vũ khí trên tay (Bật/Tắt)")]
    public GameObject leftHandHammer;  
    public GameObject rightHandHammer; 

    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animThrow = Animator.StringToHash("Phibua");

    private void Awake()
    {
        ani = GetComponent<Animator>(); 
        movementScript = GetComponent<HunterMovement>(); 
        
        // Tìm script Tương tác để lấy biến isCarryingPlayer
        interactionScript = GetComponent<HunterInteraction>(); 
        
        if (attackSource == null) attackSource = GetComponent<AudioSource>();
    }

    void Start()
    {
        currentAmmo = maxAmmo; 
        UpdateAmmoUI(); 
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    void Update()
    {
        HandleReloadSystem(); // Xử lý đồng hồ nạp đạn
    }
    
    // Hàm chém búa (Chuột trái)
    public void PerformAttackLeft()
    {
        if (isAttacking) return; // Đang múa dở thì cấm chém đè
        
        // ✅ TỰ DO CHÉM: Vác người hay không vác người thì vung búa chém vẫn hoạt động bình thường!
        if (attackSource != null && clipChemBua != null) attackSource.PlayOneShot(clipChemBua);
        
        PerformAttack(animAttack); 
    }

    // Hàm phi búa (Chuột phải)
    public void PerformAttackRight()
    {
        // ========================================================
        // 🚨 CHỐT CHẶN NÉM BÚA (ĐÃ NÂNG CẤP)
        // ========================================================
        // Nếu vai ĐANG VÁC NGƯỜI -> Khóa kỹ năng ném búa!
        if (interactionScript != null && interactionScript.isCarryingPlayer)
        {
            Debug.Log("Cấm ném búa: Sát thủ đang bận vác người trên vai!");
            return; // Thoát hàm ngay lập tức
        }

        if (isAttacking || currentAmmo <= 0 || isReloading) return; // Chặn do kẹt hoạt ảnh hoặc hết đạn

        if (attackSource != null && clipPhiBua != null) attackSource.PlayOneShot(clipPhiBua);
        PerformAttack(animThrow); 
    }

    private void PerformAttack(int attackTriggerHash)
    {
        isAttacking = true; 
        ani.SetTrigger(attackTriggerHash); 
        StarSlowEffect();
        Invoke(nameof(ForceResetAttack), 2.5f);
    }

    // Gắn vào frame giữa Anim phi búa
    public void ReleaseHammer()
    {
        if (currentAmmo <= 0) return; 

        if (leftHandHammer != null) leftHandHammer.SetActive(false);
        if (hammerPrefab != null && throwPoint != null) Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);

        currentAmmo--;
        UpdateAmmoUI(); 

        if (currentAmmo <= 0) StartReload();
    }

    // Gắn vào frame cuối của Anim Tấn công
    public void ResetAttack()
    {
        isAttacking = false; 
        if (leftHandHammer != null && currentAmmo > 0) leftHandHammer.SetActive(true);
    }

    private void ForceResetAttack()
    {
        if (isAttacking) ResetAttack(); 
    }

    public void StarSlowEffect()
    {
        if (movementScript != null)
        {
            movementScript.ApplySlow(slowMultiplier); 
            CancelInvoke(nameof(RestoreSpeed)); 
            Invoke(nameof(RestoreSpeed), slowDuraction); 
        }
    }

    private void RestoreSpeed() 
    {
        if (movementScript != null) movementScript.ResetSlow(); 
    }

    private void UpdateAmmoUI()
    {
        if (ammoText != null) ammoText.text = currentAmmo.ToString(); 
    }

    private void StartReload()
    {
        isReloading = true; 
        reloadTimer = reloadTime; 
    }

    private void HandleReloadSystem() 
    {
        if (isReloading) 
        {
            reloadTimer -= Time.deltaTime; 
            if (cooldownImage != null) cooldownImage.fillAmount = reloadTimer / reloadTime;
            if (reloadTimer <= 0)
            {
                isReloading = false; 
                currentAmmo = maxAmmo; 
                UpdateAmmoUI(); 
                if (leftHandHammer != null) leftHandHammer.SetActive(true); 
                if (cooldownImage != null) cooldownImage.fillAmount = 0f; 
            }
        }
    }
}