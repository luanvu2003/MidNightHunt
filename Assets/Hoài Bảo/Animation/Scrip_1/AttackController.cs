using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AttackController : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private Animator ani;

    [Header("Hitbox Vũ Khí (Cận Chiến)")]
    public MeleeHitbox meleeWeapon;

    [Header("Âm thanh khi chém TRÚNG")]
    public AudioClip clipHitSuccess; // on hit player sound

    [Header("Âm thanh Chiến đấu")]
    public AudioSource attackSource;
    public AudioClip clipChemBua;
    public AudioClip clipPhiBua;

    [Header("Trạng thái")]
    public bool isAttacking = false;

    [Header("Hệ thống Đạn / Bẫy")]
    public int maxAmmo = 5;
    private int currentAmmo;
    public float reloadTime = 5f;
    private bool isReloading = false;
    private float reloadTimer = 0f;

    // =========================================================
    // 🚨 BỘ QUÉT UI NÂNG CẤP (BẬT NGUYÊN CỤC CHA)
    // =========================================================
    [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
    public string uiContainerName = "KhungUI_Hunter1"; // Tên của GameObject chứa cả Text và Image
    public string ammoTextName = "TxtAmmoHunter1";
    public string cooldownImageName = "ImgCooldownHunter1";

    [Header("UI Búa / Bẫy (Tự động điền, không cần kéo)")]
    public GameObject uiContainer; // Biến lưu trữ cục Cha
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

    [Header("Cài Đặt Đặt Bẫy (Chỉ bật cho Hunter 2)")]
    public bool isTrapMode = false;
    public GameObject trapPreview;
    public float placeRange = 5f;
    public LayerMask groundLayer;

    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animThrow = Animator.StringToHash("Phibua");

    private void Awake()
    {
        ani = GetComponent<Animator>();
        movementScript = GetComponent<HunterMovement>();
        interactionScript = GetComponent<HunterInteraction>();
        if (attackSource == null) attackSource = GetComponent<AudioSource>();

        // 🚨 GỌI LỆNH TÌM VÀ BẬT UI NGAY KHI VỪA SINH RA
        AutoFindUI();
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    // =========================================================
    // BỘ MÁY QUÉT CANVAS TÌM UI VÀ BẬT CỤC CHA
    // =========================================================
    private void AutoFindUI()
    {
        // 1. TÌM VÀ BẬT CỤC CHA (CHỨA TOÀN BỘ GIAO DIỆN CỦA HUNTER NÀY)
        if (uiContainer == null)
        {
            uiContainer = FindUIObjectByName(uiContainerName);
            if (uiContainer != null)
            {
                uiContainer.SetActive(true); // 🟢 BẬT SÁNG NGUYÊN CỤC LÊN
                Debug.Log("✅ [Attack] Đã BẬT thành công bộ UI: " + uiContainerName);
            }
        }

        // 2. MÓC CÁI TEXT VÀO
        if (ammoText == null)
        {
            GameObject foundTextObj = FindUIObjectByName(ammoTextName);
            if (foundTextObj != null)
            {
                ammoText = foundTextObj.GetComponent<TextMeshProUGUI>();
                // Không cần SetActive nữa vì Cục Cha đã bật rồi
            }
        }

        // 3. MÓC CÁI VÒNG COOLDOWN VÀO
        if (cooldownImage == null)
        {
            GameObject foundCDObj = FindUIObjectByName(cooldownImageName);
            if (foundCDObj != null)
            {
                cooldownImage = foundCDObj.GetComponent<Image>();
            }
        }
    }

    private GameObject FindUIObjectByName(string objName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
            {
                if (child.name.Trim() == objName.Trim())
                {
                    return child.gameObject;
                }
            }
        }
        Debug.LogWarning("❌ [Attack] Không tìm thấy UI tên là: " + objName);
        return null;
    }

    void Update()
    {
        HandleReloadSystem();

        if (isTrapMode)
        {
            UpdateTrapPreview();
        }
    }

    private void UpdateTrapPreview()
    {
        if (currentAmmo <= 0 || isAttacking || isReloading || trapPreview == null)
        {
            if (trapPreview != null) trapPreview.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, placeRange, groundLayer))
        {
            trapPreview.SetActive(true);
            trapPreview.transform.position = hit.point;
            trapPreview.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);
        }
        else
        {
            trapPreview.SetActive(false);
        }
    }

    public void PerformAttackLeft()
    {
        if (isAttacking) return;
        PerformAttack(animAttack);
    }

    public void PerformAttackRight()
    {
        if (interactionScript != null && interactionScript.isCarryingPlayer) return;
        if (isAttacking || currentAmmo <= 0 || isReloading) return;

        if (isTrapMode)
        {
            if (trapPreview == null || !trapPreview.activeSelf) return;

            throwPoint.position = trapPreview.transform.position;
            throwPoint.rotation = trapPreview.transform.rotation;
        }
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

    public void AddAmmo(int amount)
    {
        currentAmmo += amount;
        if (currentAmmo > maxAmmo) currentAmmo = maxAmmo;
        UpdateAmmoUI();

        if (currentAmmo > 0 && isReloading)
        {
            isReloading = false;
            if (cooldownImage != null) cooldownImage.fillAmount = 0f;
            if (leftHandHammer != null) leftHandHammer.SetActive(true);
        }
    }

    public void EnableDamageFrames()
    {
        if (meleeWeapon != null)
        {
            meleeWeapon.TurnOnHitbox();
        }
    }

    public void DisableDamageFrames()
    {
        if (meleeWeapon != null)
        {
            meleeWeapon.TurnOffHitbox();
        }
    }
    // --- THÊM CÁC HÀM NÀY XUỐNG CUỐI SCRIPT ---

    // Gắn vào frame bắt đầu vung tay (đối với Hunter 1 chém búa)
    public void PlaySoundSwing()
    {
        if (attackSource != null && clipChemBua != null)
        {
            attackSource.PlayOneShot(clipChemBua);
        }
    }

    // Gắn vào frame bung tay ném búa hoặc đập tay đặt bẫy (Hunter 1 & 2)
    public void PlaySoundRelease()
    {
        if (attackSource != null && clipPhiBua != null)
        {
            attackSource.PlayOneShot(clipPhiBua);
        }
    }
    // 1. HÀM ĐẶT BẪY (Gắn vào Animation Event lúc tay đập xuống đất)
    public void SpawnTrapEvent()
    {
        // Kiểm tra nếu là Hunter 2 và còn bẫy
        if (isTrapMode && currentAmmo > 0)
        {
            if (hammerPrefab != null && throwPoint != null)
            {
                // Đẻ bẫy thật ra tại vị trí đã chốt
                Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);

                currentAmmo--;
                UpdateAmmoUI();
                if (currentAmmo <= 0) StartReload();

                // Phát âm thanh đặt bẫy thành công
                if (attackSource != null && clipPhiBua != null)
                    attackSource.PlayOneShot(clipPhiBua);
            }
        }
    }

    // 2. HÀM XỬ LÝ KHI CHÉM TRÚNG (Được gọi từ MeleeHitbox)
    public void OnHitSuccess(GameObject victim)
    {
        // Phát âm thanh trúng đòn
        if (attackSource != null && clipHitSuccess != null)
        {
            attackSource.PlayOneShot(clipHitSuccess);
        }

        // Hiệu ứng rung màn hình hoặc Blood Effect có thể thêm ở đây
        Debug.Log("Hunter đã chém trúng " + victim.name);
    }
    // Gắn vào Animation Event lúc tay Hunter bắt đầu đập bẫy xuống đất
    public void PlaySoundDatTrap()
    {
        // Kiểm tra nếu đúng là Hunter 2 (isTrapMode) và có loa, có file nhạc
        if (isTrapMode && attackSource != null && clipPhiBua != null)
        {
            // Phát tiếng "Cạch" hoặc tiếng đặt kim loại
            attackSource.PlayOneShot(clipPhiBua);
            Debug.Log("🔊 Hunter 2: Đã phát âm thanh đặt bẫy!");
        }
    }
}