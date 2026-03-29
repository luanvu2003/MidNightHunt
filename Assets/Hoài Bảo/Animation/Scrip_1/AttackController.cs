using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public enum HunterType
{
    Hunter1_NemBua,
    Hunter2_DatTrap,
    Hunter3_OiDoc
}

public class AttackController : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private FPSCamera fpsCameraScript;
    private Animator ani;

    private Vector3 lockedTrapPos;
    private Quaternion lockedTrapRot;

    [Header("== CÀI ĐẶT CHUNG (BẮT BUỘC CHỌN) ==")]
    public HunterType typeOfHunter;

    [Header("Trạng thái (Chỉ xem)")]
    public bool isAttacking = false;
    private bool isSpecialActionLocked = false;

    [Header("Hitbox Vũ Khí (Cận Chiến)")]
    public MeleeHitbox meleeWeapon;

    [Header("Âm thanh & Hiệu ứng")]
    public AudioSource attackSource;
    public AudioClip clipHitSuccess;
    public AudioClip clipChemBua;
    public AudioClip clipReleaseSkill;

    [Header("== UI CHUNG (Tự Động Tìm) ==")]
    public string uiContainerName = "KhungUI_Hunter";
    public string ammoTextName = "TxtAmmo"; 
    public string cooldownImageName = "ImgCooldown"; 

    [HideInInspector] public GameObject uiContainer;
    [HideInInspector] public TextMeshProUGUI ammoText;
    [HideInInspector] public Image cooldownImage;

    [Header("== SETTING HUNTER 1 (Ném Búa) ==")]
    public int maxAmmo = 5;
    private int currentAmmo;
    public float reloadTime = 5f;
    private bool isReloading = false;
    private float reloadTimer = 0f;
    public GameObject hammerPrefab;
    public Transform throwPoint;
    public GameObject leftHandHammer;

    [Header("== SETTING HUNTER 2 (Đặt Bẫy) ==")]
    public int maxTrap = 10;
    private int currentTrap;
    public GameObject trapPrefab;
    public GameObject trapPreview;
    public float placeRange = 5f;
    public LayerMask groundLayer;

    // ==========================================
    // 🚨 NÂNG CẤP: SETTING HUNTER 3
    // ==========================================
    [Header("== SETTING HUNTER 3 (Ói Độc) ==")]
    public GameObject vomitPrefab; 
    
    [Tooltip("Nhập tên của Object làm điểm ói (VD: VomitSpawnPoint)")]
    public string mouthPointName = "VomitSpawnPoint"; 
    [HideInInspector] public Transform mouthPoint; // Đã ẩn đi, tự động tìm

    public float vomitDuration = 2.5f;
    public float skillRechargeTime = 10f; 

    [Header("Tốc độ lúc đang ói")]
    [Range(0f, 1f)]
    public float vomitMoveSpeedMult = 0.5f; 

    [Header("Âm thanh riêng lúc ói")]
    public AudioClip clipVomitStart; 
    public AudioClip clipVomitingLoop; 

    private float currentSkillEnergy = 0f; 
    private bool isVomiting = false;
    private GameObject currentVomitInstance; 

    [Header("Hiệu ứng Làm chậm khi Chém thường")]
    [Range(0f, 1f)]
    public float slowMultiplier = 0.1f;
    public float slowDuraction = 1.1f;

    [HideInInspector] public bool isAimingTrap = false;

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
        AutoFindMouthPoint(); // 🚨 Gọi hàm tự động tìm điểm ói
    }
    
    // 🚨 HÀM MỚI: Tự động tìm Mouth Point bằng Tên
    private void AutoFindMouthPoint()
    {
        if (typeOfHunter != HunterType.Hunter3_OiDoc) return;

        // Quét toàn bộ con cái của Hunter này xem có ai tên giống mouthPointName không
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name.Trim() == mouthPointName.Trim())
            {
                mouthPoint = child;
                Debug.Log("✅ [Hunter 3] Đã tự động tìm thấy điểm Ói: " + mouthPointName);
                return;
            }
        }

        // Nếu không tìm thấy trong body, thử kiếm thẳng ngoài Scene (Phòng trường hợp Camera nằm ngoài)
        GameObject globalMouth = GameObject.Find(mouthPointName);
        if (globalMouth != null)
        {
            mouthPoint = globalMouth.transform;
            Debug.Log("✅ [Hunter 3] Đã tìm thấy điểm Ói ở ngoài Scene: " + mouthPointName);
        }
        else
        {
            Debug.LogError("❌ [Hunter 3 LỖI] KHÔNG THỂ TÌM THẤY object tên là: [" + mouthPointName + "]. Hãy kiểm tra lại tên ngoài Hierarchy!");
        }
    }

    void Start()
    {
        switch (typeOfHunter)
        {
            case HunterType.Hunter1_NemBua:
                currentAmmo = maxAmmo;
                if (cooldownImage != null) cooldownImage.fillAmount = 0f;
                break;
            case HunterType.Hunter2_DatTrap:
                currentTrap = maxTrap;
                break;
            case HunterType.Hunter3_OiDoc:
                currentSkillEnergy = 0f; 
                UpdateSkillUI();
                break;
        }
        UpdateAmmoUI();
    }

    void Update()
    {
        switch (typeOfHunter)
        {
            case HunterType.Hunter1_NemBua: HandleReloadSystem(); break;
            case HunterType.Hunter2_DatTrap: UpdateTrapPreview(); break;
            case HunterType.Hunter3_OiDoc: HandleSkillRecharge(); break;
        }
    }

    // =========================================================
    // HỆ THỐNG TÌM UI TỰ ĐỘNG
    // =========================================================
    private void AutoFindUI()
    {
        if (uiContainer == null)
        {
            uiContainer = FindUIObjectByName(uiContainerName);
            if (uiContainer != null) uiContainer.SetActive(true);
        }

        if (ammoText == null && !string.IsNullOrEmpty(ammoTextName) && ammoTextName != "NONE")
        {
            GameObject textObj = FindUIObjectByName(ammoTextName);
            if (textObj != null) ammoText = textObj.GetComponent<TextMeshProUGUI>();
        }

        if (cooldownImage == null && !string.IsNullOrEmpty(cooldownImageName))
        {
            GameObject cdObj = FindUIObjectByName(cooldownImageName);
            if (cdObj != null) cooldownImage = cdObj.GetComponent<Image>();
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

    // =========================================================
    // 1. LOGIC HUNTER 1 (NÉM BÚA)
    // =========================================================
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

    private void StartReload()
    {
        isReloading = true;
        reloadTimer = reloadTime;
    }

    public void ReleaseHammer()
    {
        if (typeOfHunter != HunterType.Hunter1_NemBua || currentAmmo <= 0) return;

        if (leftHandHammer != null) leftHandHammer.SetActive(false);

        if (hammerPrefab != null && throwPoint != null)
            Instantiate(hammerPrefab, throwPoint.position, throwPoint.rotation);

        currentAmmo--;
        UpdateAmmoUI();

        if (currentAmmo <= 0) StartReload();
    }

    // =========================================================
    // 2. LOGIC HUNTER 2 (ĐẶT BẪY)
    // =========================================================
    private void UpdateTrapPreview()
    {
        if (currentTrap <= 0 || isAttacking || trapPreview == null || !isAimingTrap)
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

            lockedTrapPos = hit.point;
            lockedTrapRot = trapPreview.transform.rotation;
        }
        else
        {
            trapPreview.SetActive(false);
        }
    }

    public void SpawnTrapEvent()
    {
        if (typeOfHunter != HunterType.Hunter2_DatTrap || currentTrap <= 0) return;

        if (trapPrefab != null)
        {
            GameObject newTrap = Instantiate(trapPrefab, lockedTrapPos, lockedTrapRot);
            BearTrap trapScript = newTrap.GetComponent<BearTrap>();
            if (trapScript != null) trapScript.ownerHunter = this;

            currentTrap--;
            UpdateAmmoUI();

            if (attackSource != null && clipReleaseSkill != null)
                attackSource.PlayOneShot(clipReleaseSkill);
        }
    }

    public void RecoverTrap()
    {
        if (typeOfHunter != HunterType.Hunter2_DatTrap) return;
        if (currentTrap < maxTrap)
        {
            currentTrap++;
            UpdateAmmoUI();
        }
    }

    // =========================================================
    // 3. LOGIC HUNTER 3 (ÓI ĐỘC)
    // =========================================================
    private void HandleSkillRecharge()
    {
        if (isAttacking || isVomiting) return;

        if (currentSkillEnergy > 0f)
        {
            currentSkillEnergy -= Time.deltaTime / skillRechargeTime;
            if (currentSkillEnergy < 0f) currentSkillEnergy = 0f;
            UpdateSkillUI();
        }
    }

    private void UpdateSkillUI()
    {
        if (cooldownImage != null)
        {
            cooldownImage.fillAmount = currentSkillEnergy;
        }
    }

    public void StartVomitEvent()
    {
        if (typeOfHunter != HunterType.Hunter3_OiDoc || isVomiting) return;
        if (vomitPrefab == null || mouthPoint == null) return;

        isVomiting = true;

        currentVomitInstance = Instantiate(vomitPrefab, mouthPoint.position, mouthPoint.rotation);
        currentVomitInstance.transform.SetParent(mouthPoint);

        if (attackSource != null)
        {
            if (clipVomitStart != null) attackSource.PlayOneShot(clipVomitStart);
            if (clipVomitingLoop != null) { attackSource.clip = clipVomitingLoop; attackSource.Play(); }
        }

        StartCoroutine(UseSkillCoroutine());
    }

    public void StopVomitEvent()
    {
        if (typeOfHunter != HunterType.Hunter3_OiDoc) return;

        isVomiting = false;

        if (currentVomitInstance != null) Destroy(currentVomitInstance);

        if (attackSource != null && attackSource.clip == clipVomitingLoop) attackSource.Stop();

        RestoreSpeed();
    }

    IEnumerator UseSkillCoroutine()
    {
        float timer = 0f;
        float startEnergy = currentSkillEnergy;

        while (timer < vomitDuration && isVomiting)
        {
            timer += Time.deltaTime;
            currentSkillEnergy = Mathf.Lerp(startEnergy, 1f, timer / vomitDuration);
            UpdateSkillUI();
            yield return null;
        }
        currentSkillEnergy = 1f; 
        UpdateSkillUI();
    }


    // =========================================================
    // XỬ LÝ TẤN CÔNG & LÀM CHẬM 
    // =========================================================
    public void PerformAttackLeft()
    {
        if (isAttacking || isVomiting) return;
        isSpecialActionLocked = false;
        PerformAttack(animAttack);
    }

    public void PerformAttackRight()
    {
        if (isAttacking || isVomiting || isReloading) return;
        if (interactionScript != null && interactionScript.isCarryingPlayer) return;

        switch (typeOfHunter)
        {
            case HunterType.Hunter1_NemBua:
                if (currentAmmo <= 0) return;
                isSpecialActionLocked = false;
                break;

            case HunterType.Hunter2_DatTrap:
                if (currentTrap <= 0 || trapPreview == null || !trapPreview.activeSelf) return;

                lockedTrapPos = trapPreview.transform.position;
                lockedTrapRot = trapPreview.transform.rotation;
                trapPreview.SetActive(false);
                isSpecialActionLocked = true; 
                break;

            case HunterType.Hunter3_OiDoc:
                if (currentSkillEnergy > 0f) return;
                isSpecialActionLocked = false;
                break;
        }

        PerformAttack(animThrow);
    }

    private void PerformAttack(int attackTriggerHash)
    {
        isAttacking = true;
        ani.SetTrigger(attackTriggerHash);

        if (isSpecialActionLocked)
        {
            // Khóa Camera
            if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = true;

            // 🚨 SỬA LỖI H2: Khóa chết tốc độ di chuyển NGAY LẬP TỨC lúc vừa bấm chuột đặt bẫy
            if (typeOfHunter == HunterType.Hunter2_DatTrap && movementScript != null)
            {
                movementScript.ApplySlow(0f);
            }
        }

        Invoke(nameof(ForceResetAttack), isVomiting ? vomitDuration + 0.5f : 2.5f);
    }

    public void StarSlowEffect()
    {
        if (movementScript != null)
        {
            float targetSlowMult = 1f;

            if (typeOfHunter == HunterType.Hunter2_DatTrap && isSpecialActionLocked)
            {
                targetSlowMult = 0f;
            }
            else if (typeOfHunter == HunterType.Hunter3_OiDoc && isVomiting)
            {
                targetSlowMult = vomitMoveSpeedMult;
            }
            else
            {
                targetSlowMult = slowMultiplier;
            }

            // Mặc dù H2 đã bị khóa ở PerformAttack, ta vẫn gán lại 0f ở đây cho chắc chắn
            movementScript.ApplySlow(targetSlowMult);

            if (!isVomiting)
            {
                CancelInvoke(nameof(RestoreSpeed));
                Invoke(nameof(RestoreSpeed), slowDuraction);
            }
        }
    }

    private void RestoreSpeed() { if (movementScript != null) movementScript.ResetSlow(); }

    public void ResetAttack()
    {
        isAttacking = false;
        if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;

        if (typeOfHunter == HunterType.Hunter1_NemBua && leftHandHammer != null && currentAmmo > 0)
        {
            leftHandHammer.SetActive(true);
        }

        // Đảm bảo tốc độ được trả lại nếu bị kẹt
        RestoreSpeed(); 
    }

    private void ForceResetAttack() { if (isAttacking) ResetAttack(); }

    public void UpdateAmmoUI()
    {
        if (ammoText == null) return;
        if (typeOfHunter == HunterType.Hunter1_NemBua) ammoText.text = currentAmmo.ToString();
        else if (typeOfHunter == HunterType.Hunter2_DatTrap) ammoText.text = currentTrap.ToString();
        else if (typeOfHunter == HunterType.Hunter3_OiDoc) ammoText.text = "";
    }

    // =========================================================
    // ÂM THANH & HITBOX (ANIMATION EVENTS)
    // =========================================================
    public void PlaySoundSwing() { if (attackSource != null && clipChemBua != null) attackSource.PlayOneShot(clipChemBua); }
    public void PlaySoundRelease() { if (attackSource != null && clipReleaseSkill != null) attackSource.PlayOneShot(clipReleaseSkill); }
    public void PlaySoundDatTrap() { if (typeOfHunter == HunterType.Hunter2_DatTrap && attackSource != null && clipReleaseSkill != null) attackSource.PlayOneShot(clipReleaseSkill); }

    public void EnableDamageFrames() { if (meleeWeapon != null) meleeWeapon.TurnOnHitbox(); }
    public void DisableDamageFrames() { if (meleeWeapon != null) meleeWeapon.TurnOffHitbox(); }
    public void OnHitSuccess(GameObject victim) { if (attackSource != null && clipHitSuccess != null) attackSource.PlayOneShot(clipHitSuccess); }
}