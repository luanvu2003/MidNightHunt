using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class AttackController : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private FPSCamera fpsCameraScript; // 🚨 THÊM: Quản lý Camera
    private Animator ani;

    // Biến chốt tọa độ bẫy
    private Vector3 lockedTrapPos;
    private Quaternion lockedTrapRot;

    [Header("Hitbox Vũ Khí (Cận Chiến)")]
    public MeleeHitbox meleeWeapon;

    [Header("Âm thanh khi chém TRÚNG")]
    public AudioClip clipHitSuccess;

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

    [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
    public string uiContainerName = "KhungUI_Hunter1";
    public string ammoTextName = "TxtAmmoHunter1";
    public string cooldownImageName = "ImgCooldownHunter1";

    [Header("UI Búa / Bẫy (Tự động điền)")]
    public GameObject uiContainer;
    public TextMeshProUGUI ammoText;
    public Image cooldownImage;

    [Header("Vũ Khí & Tọa Độ Ném")]
    public GameObject hammerPrefab;
    public Transform throwPoint;

    [Header("Hiệu ứng Làm chậm (Slow)")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.1f;
    public float slowDuraction = 1.1f;

    [Header("Vũ khí tay trái (Bật/Tắt)")]
    public GameObject leftHandHammer;

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
        if (Camera.main != null) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        if (attackSource == null) attackSource = GetComponent<AudioSource>();

        AutoFindUI();
    }

    void Start()
    {
        currentAmmo = maxAmmo;
        UpdateAmmoUI();
        if (cooldownImage != null) cooldownImage.fillAmount = 0f;
    }

    // --- BỘ QUÉT UI ---
    private void AutoFindUI()
    {
        if (uiContainer == null)
        {
            uiContainer = FindUIObjectByName(uiContainerName);
            if (uiContainer != null)
            {
                uiContainer.SetActive(true);
            }
        }
        if (ammoText == null)
        {
            GameObject foundTextObj = FindUIObjectByName(ammoTextName);
            if (foundTextObj != null) ammoText = foundTextObj.GetComponent<TextMeshProUGUI>();
        }
        if (cooldownImage == null)
        {
            GameObject foundCDObj = FindUIObjectByName(cooldownImageName);
            if (foundCDObj != null) cooldownImage = foundCDObj.GetComponent<Image>();
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
                if (child.name.Trim() == objName.Trim()) return child.gameObject;
            }
        }
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
            if (trapPreview != null && trapPreview.activeSelf) trapPreview.SetActive(false);
            return;
        }

        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));

        if (Physics.Raycast(ray, out RaycastHit hit, placeRange, groundLayer))
        {
            if (!trapPreview.activeSelf) trapPreview.SetActive(true);
            trapPreview.transform.position = hit.point;
            trapPreview.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            // Chốt tọa độ liên tục trước khi tấn công
            lockedTrapPos = hit.point;
            lockedTrapRot = trapPreview.transform.rotation;
        }
        else
        {
            trapPreview.SetActive(false);
        }
    }

    // --- XỬ LÝ ATTACK / ĐẶT BẪY ---
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

            // 🚨 Chốt lần cuối và tắt Preview
            lockedTrapPos = trapPreview.transform.position;
            lockedTrapRot = trapPreview.transform.rotation;
            trapPreview.SetActive(false);
        }

        PerformAttack(animThrow);
    }

    private void PerformAttack(int attackTriggerHash)
    {
        isAttacking = true;
        ani.SetTrigger(attackTriggerHash);
        StarSlowEffect();

        // 🚨 NÂNG CẤP: Khóa Camera khi đặt bẫy (Hunter 2)
        if (isTrapMode && fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true;
        }

        Invoke(nameof(ForceResetAttack), 2.5f);
    }

    public void ResetAttack()
    {
        isAttacking = false;

        // 🚨 NÂNG CẤP: Mở khóa Camera sau khi đặt xong
        if (isTrapMode && fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = false;
        }

        if (leftHandHammer != null && currentAmmo > 0) leftHandHammer.SetActive(true);
    }

    private void ForceResetAttack()
    {
        if (isAttacking) ResetAttack();
    }

    // --- HIỆU ỨNG SLOW / KHÓA DI CHUYỂN ---
    public void StarSlowEffect()
    {
        if (movementScript != null)
        {
            // Nếu là Hunter 2 đặt bẫy -> Khóa cứng di chuyển (Tốc độ = 0)
            float currentMult = isTrapMode ? 0f : slowMultiplier;
            movementScript.ApplySlow(currentMult);

            CancelInvoke(nameof(RestoreSpeed));
            Invoke(nameof(RestoreSpeed), slowDuraction);
        }
    }

    private void RestoreSpeed()
    {
        if (movementScript != null) movementScript.ResetSlow();
    }

    // --- HỆ THỐNG ĐẠN ---
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

    // --- ANIMATION EVENTS ---
    public void ReleaseHammer()
    {
        if (isTrapMode) return; // Bảo vệ: Hunter 2 không chạy hàm này

        if (currentAmmo <= 0) return;
        if (leftHandHammer != null) leftHandHammer.SetActive(false);

        if (hammerPrefab != null && throwPoint != null)
            Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);

        currentAmmo--;
        UpdateAmmoUI();
        if (currentAmmo <= 0) StartReload();
    }

    public void SpawnTrapEvent()
    {
        if (!isTrapMode) return; // Bảo vệ: Hunter 1 không chạy hàm này

        if (currentAmmo > 0 && hammerPrefab != null)
        {
            // Đẻ cái bẫy ra và lưu nó vào một biến
            GameObject newTrap = Instantiate(hammerPrefab, lockedTrapPos, lockedTrapRot);

            // 🚨 BƯỚC QUAN TRỌNG: Báo cho cái bẫy biết "Ta là chủ của ngươi"
            BearTrap trapScript = newTrap.GetComponent<BearTrap>();
            if (trapScript != null)
            {
                trapScript.ownerHunter = this; // Truyền chính AttackController này sang cho bẫy
            }

            currentAmmo--;
            UpdateAmmoUI();

            // ĐÃ XÓA dòng StartReload() ở đây. Bẫy sẽ KHÔNG tự hồi nữa!

            if (attackSource != null && clipPhiBua != null)
                attackSource.PlayOneShot(clipPhiBua);
        }
    }

    // --- ÂM THANH & HITBOX ---
    public void PlaySoundSwing()
    {
        if (attackSource != null && clipChemBua != null) attackSource.PlayOneShot(clipChemBua);
    }

    public void PlaySoundRelease()
    {
        if (attackSource != null && clipPhiBua != null) attackSource.PlayOneShot(clipPhiBua);
    }

    public void PlaySoundDatTrap()
    {
        if (isTrapMode && attackSource != null && clipPhiBua != null)
        {
            attackSource.PlayOneShot(clipPhiBua);
        }
    }

    public void EnableDamageFrames() { if (meleeWeapon != null) meleeWeapon.TurnOnHitbox(); }
    public void DisableDamageFrames() { if (meleeWeapon != null) meleeWeapon.TurnOffHitbox(); }

    public void OnHitSuccess(GameObject victim)
    {
        if (attackSource != null && clipHitSuccess != null) attackSource.PlayOneShot(clipHitSuccess);
    }
    // Hàm này sẽ được cái Bẫy gọi khi có người dẫm vào hoặc phá bẫy
    public void RecoverTrap()
    {
        if (!isTrapMode) return;

        if (currentAmmo < maxAmmo)
        {
            currentAmmo++;
            UpdateAmmoUI();
            Debug.Log("♻️ Bẫy đã nổ/bị phá! Hunter 2 đã hồi lại 1 bẫy. Hiện có: " + currentAmmo);
        }
    }
}