using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AttackController : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private Animator ani;
    // 1. THÊM BIẾN NÀY LÊN PHẦN [Header]:
    [Header("Hitbox Vũ Khí (Cận Chiến)")]
    public MeleeHitbox meleeWeapon; // Kéo thả cục Hitbox Búa hoặc Vuốt vào đây

    [Header("Âm thanh Chiến đấu")]
    public AudioSource attackSource;
    public AudioClip clipChemBua;
    public AudioClip clipPhiBua; // Hoặc tiếng đặt bẫy

    [Header("Trạng thái")]
    public bool isAttacking = false;

    [Header("Hệ thống Đạn / Bẫy")]
    public int maxAmmo = 5;
    private int currentAmmo;
    public float reloadTime = 5f;
    private bool isReloading = false;
    private float reloadTimer = 0f;

    [Header("UI Búa / Bẫy")]
    public TextMeshProUGUI ammoText;
    public Image cooldownImage;

    [Header("Vũ Khí & Tọa Độ Ném")]
    public GameObject hammerPrefab; // Prefab viên búa hoặc Cục bẫy
    public Transform throwPoint;

    [Header("Hiệu ứng Làm chậm (Slow)")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.1f;
    public float slowDuraction = 1.1f;

    [Header("Vũ khí trên tay (Bật/Tắt)")]
    public GameObject leftHandHammer;
    public GameObject rightHandHammer;

    // =========================================================
    // 🚨 TÍNH NĂNG MỚI: CHẾ ĐỘ ĐẶT BẪY (CHO HUNTER 2)
    // =========================================================
    [Header("Cài Đặt Đặt Bẫy (Chỉ bật cho Hunter 2)")]
    public bool isTrapMode = false;       // Nút gạt bật/tắt chế độ Hunter 2
    public GameObject trapPreview;        // Hình chiếu mờ mờ của cái bẫy
    public float placeRange = 5f;         // Tầm nhìn tối đa để đặt bẫy
    public LayerMask groundLayer;         // Lớp mặt đất (để bẫy không dính lên tường)

    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animThrow = Animator.StringToHash("Phibua"); // Với Hunter 2, anim này là "DatBay"

    private void Awake()
    {
        ani = GetComponent<Animator>();
        movementScript = GetComponent<HunterMovement>();
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
        HandleReloadSystem();

        // 🚨 LOGIC QUÉT MẶT ĐẤT ĐỂ HIỂN THỊ BẪY MỜ
        if (isTrapMode)
        {
            UpdateTrapPreview();
        }
    }

    // Quét tia từ Camera xuống đất để vẽ cái bẫy ảo
    private void UpdateTrapPreview()
    {
        // Nếu hết bẫy hoặc đang múa thì tắt hình chiếu
        if (currentAmmo <= 0 || isAttacking || isReloading || trapPreview == null)
        {
            if (trapPreview != null) trapPreview.SetActive(false);
            return;
        }

        // Bắn tia Raycast từ giữa màn hình Camera hướng tới trước
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        // Nếu tia này chạm vào mặt đất trong phạm vi placeRange
        if (Physics.Raycast(ray, out RaycastHit hit, placeRange, groundLayer))
        {
            trapPreview.SetActive(true);
            trapPreview.transform.position = hit.point; // Dời bẫy ảo tới điểm chạm đất
            // Ép cái bẫy xoay theo hướng mặt của Hunter cho đẹp
            trapPreview.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
        else
        {
            // Nhìn lên trời hoặc nhìn quá xa thì ẩn bẫy mờ đi
            trapPreview.SetActive(false);
        }
    }

    public void PerformAttackLeft()
    {
        if (isAttacking) return;
        if (attackSource != null && clipChemBua != null) attackSource.PlayOneShot(clipChemBua);
        PerformAttack(animAttack);
    }

    public void PerformAttackRight()
    {
        if (interactionScript != null && interactionScript.isCarryingPlayer) return;
        if (isAttacking || currentAmmo <= 0 || isReloading) return;

        // Nếu là Hunter 2: Kiểm tra xem có đang nhìn xuống đất hợp lệ không
        if (isTrapMode)
        {
            // Nếu hình chiếu mờ đang tắt (do nhìn lên trời), thì KHÔNG CHO ĐẶT
            if (trapPreview == null || !trapPreview.activeSelf) return;

            // BÍ QUYẾT: Dời cái throwPoint (điểm đẻ bẫy) ra đúng chỗ hình chiếu ảo đang đứng!
            throwPoint.position = trapPreview.transform.position;
            throwPoint.rotation = trapPreview.transform.rotation;
        }

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

    public void ReleaseHammer()
    {
        if (currentAmmo <= 0) return;

        if (leftHandHammer != null) leftHandHammer.SetActive(false);

        // Đoạn này đẻ ra Búa (nếu là Hunter 1) hoặc đẻ ra Bẫy (nếu là Hunter 2)
        if (hammerPrefab != null && throwPoint != null)
            Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);

        currentAmmo--;
        UpdateAmmoUI();

        if (currentAmmo <= 0) StartReload();
    }

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

    public void UpdateAmmoUI()
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

    // =========================================================
    // HÀM MỞ: ĐỂ CÁI BẪY GỌI VÀO TRẢ LẠI 1 ĐẠN KHI DÍNH NGƯỜI
    // =========================================================
    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        if (currentAmmo > maxAmmo) currentAmmo = maxAmmo; // Không cho lố 5 cái
        UpdateAmmoUI();

        // Nếu túi đang rỗng mà được cộng lại đạn, thì cất cái vòng Cooldown đi
        if (currentAmmo > 0 && isReloading)
        {
            isReloading = false;
            if (cooldownImage != null) cooldownImage.fillAmount = 0f;
            if (leftHandHammer != null) leftHandHammer.SetActive(true);
        }
    }
    // 2. THÊM HÀM NÀY XUỐNG DƯỚI CÙNG (Để gắn Animation Event bật Hitbox)
    public void EnableDamageFrames()
    {
        if (meleeWeapon != null)
        {
            meleeWeapon.TurnOnHitbox();
        }
    }

    // 3. THÊM HÀM NÀY XUỐNG DƯỚI CÙNG (Để gắn Animation Event tắt Hitbox)
    public void DisableDamageFrames()
    {
        if (meleeWeapon != null)
        {
            meleeWeapon.TurnOffHitbox();
        }
    }
}