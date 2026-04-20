using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using Fusion;
public enum HunterType
{
    Hunter1_NemBua,
    Hunter2_DatTrap,
    Hunter3_OiDoc
}
public class AttackController : NetworkBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private FPSCamera fpsCameraScript;
    private Animator ani;
    private Vector3 lockedTrapPos;
    private Quaternion lockedTrapRot;
    [Header("== CÀI ĐẶT CHUNG (BẮT BUỘC CHỌN) ==")]
    public HunterType typeOfHunter;
    [Header("Trạng thái (Đã được đồng bộ mạng)")]
    [Networked] public NetworkBool isAttacking { get; set; }
    [Networked] private NetworkBool isSpecialActionLocked { get; set; }
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
    public string cooldownTextName = "TxtCooldownTime";
    [Header("Tên UI Image Từng Hunter")]
    public string imgHunter1Name = "ImgHunter1";
    public string imgHunter2Name = "ImgHunter2";
    public string imgHunter3Name = "ImgHunter3";
    [HideInInspector] public GameObject uiContainer;
    [HideInInspector] public TextMeshProUGUI ammoText;
    [HideInInspector] public Image cooldownImage;
    [HideInInspector] public TextMeshProUGUI cooldownText;
    [HideInInspector] public GameObject imgHunter1;
    [HideInInspector] public GameObject imgHunter2;
    [HideInInspector] public GameObject imgHunter3;

    [Header("== SETTING HUNTER 1 (Ném Búa) ==")]
    public int maxAmmo = 5;
    [Networked, OnChangedRender(nameof(UpdateAmmoUI))] public int currentAmmo { get; set; }
    public float reloadTime = 5f;
    [Networked] private NetworkBool isReloading { get; set; }
    [Networked] private TickTimer reloadTimer { get; set; }
    public GameObject hammerPrefab;
    public Transform throwPoint;
    public GameObject leftHandHammer;

    [Header("== SETTING HUNTER 2 (Đặt Bẫy) ==")]
    public int maxTrap = 10;
    [Networked, OnChangedRender(nameof(UpdateAmmoUI))] public int currentTrap { get; set; }
    public GameObject trapPrefab;
    [Tooltip("Kéo PREFAB của bẫy mờ (Trap Fake) vào đây")]
    public GameObject trapPreviewPrefab;
    private GameObject trapPreviewInstance;
    public float placeRange = 6.5f;
    public LayerMask groundLayer;
    [Networked] public NetworkBool isAimingTrap { get; set; }
    [Header("== SETTING HUNTER 3 (Ói Độc) ==")]
    public GameObject vomitPrefab;
    public string mouthPointName = "VomitSpawnPoint";
    [HideInInspector] public Transform mouthPoint;
    public float vomitDuration = 3f;
    public float skillRechargeTime = 10f;
    [Range(0f, 1f)] public float vomitMoveSpeedMult = 0.5f;
    public AudioClip clipVomitStart;
    public AudioClip clipVomitingLoop;
    [Networked] private float currentSkillEnergy { get; set; }
    [Networked] private NetworkBool isVomiting { get; set; }
    private GameObject currentVomitInstance;

    [Header("Hiệu ứng Làm chậm khi Chém thường")]
    [Range(0f, 1f)] public float slowMultiplier = 0.1f;
    public float slowDuraction = 1.1f;
    private readonly int animAttack = Animator.StringToHash("Attack");
    private readonly int animThrow = Animator.StringToHash("Phibua");
    [Networked] public int attackCounter { get; set; }
    private void Awake()
    {
        ani = GetComponentInChildren<Animator>();
        movementScript = GetComponent<HunterMovement>();
        interactionScript = GetComponent<HunterInteraction>();
        if (Camera.main != null) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        if (attackSource == null) attackSource = GetComponent<AudioSource>();
        if (attackSource != null)
        {
            attackSource.spatialBlend = 1f;
            attackSource.rolloffMode = AudioRolloffMode.Linear;
            attackSource.minDistance = 3f;
            attackSource.maxDistance = 30f;
        }
        AutoFindUI();
        AutoFindMouthPoint();
    }
    private void AutoFindMouthPoint()
    {
        if (typeOfHunter != HunterType.Hunter3_OiDoc) return;
        Transform[] allChildren = GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name.Trim() == mouthPointName.Trim())
            {
                mouthPoint = child;
                return;
            }
        }
        GameObject globalMouth = GameObject.Find(mouthPointName);
        if (globalMouth != null) mouthPoint = globalMouth.transform;
    }
    public override void Spawned()
    {
        if (Object.HasInputAuthority)
        {
            if (imgHunter1 != null) imgHunter1.SetActive(false);
            if (imgHunter2 != null) imgHunter2.SetActive(false);
            if (imgHunter3 != null) imgHunter3.SetActive(false);
            if (cooldownImage != null && typeOfHunter != HunterType.Hunter3_OiDoc)
            {
                cooldownImage.gameObject.SetActive(false);
                cooldownImage.fillAmount = 0f;
            }
            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(false);
                cooldownText.text = "";
            }
            switch (typeOfHunter)
            {
                case HunterType.Hunter1_NemBua:
                    if (imgHunter1 != null) imgHunter1.SetActive(true);
                    break;
                case HunterType.Hunter2_DatTrap:
                    if (imgHunter2 != null) imgHunter2.SetActive(true);
                    if (trapPreviewPrefab != null)
                    {
                        trapPreviewInstance = Instantiate(trapPreviewPrefab);
                        trapPreviewInstance.SetActive(false);
                        foreach (Collider col in trapPreviewInstance.GetComponentsInChildren<Collider>())
                            col.enabled = false;
                    }
                    break;
                case HunterType.Hunter3_OiDoc:
                    UpdateSkillUI();
                    if (imgHunter3 != null) imgHunter3.SetActive(true);
                    if (ammoText != null) ammoText.gameObject.SetActive(false);
                    break;
            }
        }
        if (Object.HasStateAuthority)
        {
            isReloading = false;
            switch (typeOfHunter)
            {
                case HunterType.Hunter1_NemBua: currentAmmo = maxAmmo; break;
                case HunterType.Hunter2_DatTrap: currentTrap = maxTrap; break;
                case HunterType.Hunter3_OiDoc: currentSkillEnergy = 0f; break;
            }
        }
        UpdateAmmoUI();
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (trapPreviewInstance != null) Destroy(trapPreviewInstance);
    }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority)
        {
            if (typeOfHunter == HunterType.Hunter1_NemBua || typeOfHunter == HunterType.Hunter2_DatTrap)
                HandleReloadSystem();

            if (typeOfHunter == HunterType.Hunter3_OiDoc)
                HandleSkillRecharge();
        }
    }
    void Update()
    {
        if (!Object.HasInputAuthority) return;

        if (typeOfHunter == HunterType.Hunter1_NemBua || typeOfHunter == HunterType.Hunter2_DatTrap)
        {
            if (isReloading)
            {
                float remaining = reloadTimer.RemainingTime(Runner) ?? 0f;

                if (cooldownImage != null)
                {
                    if (!cooldownImage.gameObject.activeSelf) cooldownImage.gameObject.SetActive(true);
                    cooldownImage.fillAmount = remaining / reloadTime;
                }

                if (ammoText != null && ammoText.gameObject.activeSelf)
                {
                    ammoText.gameObject.SetActive(false);
                }

                if (cooldownText != null)
                {
                    if (!cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(true);
                    cooldownText.text = Mathf.CeilToInt(remaining).ToString();
                }
            }
            else
            {
                if (cooldownImage != null && cooldownImage.gameObject.activeSelf) cooldownImage.gameObject.SetActive(false);
                if (cooldownText != null && cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(false);
                if (ammoText != null && !ammoText.gameObject.activeSelf)
                {
                    ammoText.gameObject.SetActive(true);
                    UpdateAmmoUI();
                }
            }
        }
        else if (typeOfHunter == HunterType.Hunter3_OiDoc)
        {
            UpdateSkillUI();
        }

        if (typeOfHunter == HunterType.Hunter2_DatTrap)
        {
            UpdateTrapPreview();
        }
    }
    private void AutoFindUI()
    {
        if (uiContainer == null)
        {
            uiContainer = FindUIObjectByName(uiContainerName);
            if (uiContainer != null) uiContainer.SetActive(true);
        }
        if (ammoText == null && !string.IsNullOrEmpty(ammoTextName))
            ammoText = FindUIObjectByName(ammoTextName)?.GetComponent<TextMeshProUGUI>();
        if (cooldownImage == null && !string.IsNullOrEmpty(cooldownImageName))
            cooldownImage = FindUIObjectByName(cooldownImageName)?.GetComponent<Image>();
        if (cooldownText == null && !string.IsNullOrEmpty(cooldownTextName))
            cooldownText = FindUIObjectByName(cooldownTextName)?.GetComponent<TextMeshProUGUI>();
        if (imgHunter1 == null) imgHunter1 = FindUIObjectByName(imgHunter1Name);
        if (imgHunter2 == null) imgHunter2 = FindUIObjectByName(imgHunter2Name);
        if (imgHunter3 == null) imgHunter3 = FindUIObjectByName(imgHunter3Name);
    }
    private GameObject FindUIObjectByName(string objName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
            foreach (Transform child in canvas.GetComponentsInChildren<Transform>(true))
                if (child.name.Trim() == objName.Trim()) return child.gameObject;
        return null;
    }
    private void HandleReloadSystem()
    {
        if (isReloading)
        {
            if (reloadTimer.Expired(Runner))
            {
                isReloading = false;
                if (typeOfHunter == HunterType.Hunter1_NemBua) currentAmmo = maxAmmo;
                if (typeOfHunter == HunterType.Hunter2_DatTrap) currentTrap = maxTrap;
                Rpc_FinishReload();
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_FinishReload()
    {
        if (leftHandHammer != null) leftHandHammer.SetActive(true);
        if (Object.HasInputAuthority)
        {
            if (cooldownImage != null)
            {
                cooldownImage.fillAmount = 0f;
                cooldownImage.gameObject.SetActive(false);
            }
            if (cooldownText != null) cooldownText.gameObject.SetActive(false);
            if (ammoText != null)
            {
                ammoText.gameObject.SetActive(true);
                UpdateAmmoUI();
            }
        }
    }
    private void StartReload()
    {
        isReloading = true;
        reloadTimer = TickTimer.CreateFromSeconds(Runner, reloadTime);
    }
    public void ReleaseHammer()
    {
        if (!Object.HasStateAuthority || typeOfHunter != HunterType.Hunter1_NemBua || currentAmmo <= 0) return;
        Rpc_ToggleHandHammer(false);
        if (hammerPrefab != null && throwPoint != null)
            Runner.Spawn(hammerPrefab, throwPoint.position, throwPoint.rotation, Object.InputAuthority);
        currentAmmo--;
        if (currentAmmo <= 0) StartReload();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ToggleHandHammer(bool isVisible) { if (leftHandHammer != null) leftHandHammer.SetActive(isVisible); }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    public void Rpc_SetAimingTrap(bool aiming) { isAimingTrap = aiming; }    private void UpdateTrapPreview()
    {
        if (currentTrap <= 0 || isAttacking || trapPreviewInstance == null || !isAimingTrap || isReloading)
        {
            if (trapPreviewInstance != null && trapPreviewInstance.activeSelf) trapPreviewInstance.SetActive(false);
            return;
        }
        Ray ray = Camera.main.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0));
        if (Physics.Raycast(ray, out RaycastHit hit, placeRange, groundLayer))
        {
            if (!trapPreviewInstance.activeSelf) trapPreviewInstance.SetActive(true);
            trapPreviewInstance.transform.position = hit.point + Vector3.up * 0.02f;
            trapPreviewInstance.transform.rotation = Quaternion.Euler(0, transform.eulerAngles.y, 0);

            lockedTrapPos = hit.point;
            lockedTrapRot = trapPreviewInstance.transform.rotation;
        }
        else
        {
            trapPreviewInstance.SetActive(false);
        }
    }
    public void SpawnTrapEvent()
    {
        if (!Object.HasStateAuthority || typeOfHunter != HunterType.Hunter2_DatTrap || currentTrap <= 0) return;
        if (trapPrefab != null)
        {
            NetworkObject newTrap = Runner.Spawn(trapPrefab, lockedTrapPos, lockedTrapRot, Object.InputAuthority);
            BearTrap trapScript = newTrap.GetComponent<BearTrap>();
            if (trapScript != null) trapScript.ownerHunter = this;
            currentTrap--;
            Rpc_PlayReleaseSound();
            if (currentTrap <= 0) StartReload();
        }
    }
    public void RecoverTrap()
    {
        if (Object.HasStateAuthority && typeOfHunter == HunterType.Hunter2_DatTrap && currentTrap < maxTrap)
            currentTrap++;
    }
    private void HandleSkillRecharge()
    {
        if (isAttacking || isVomiting) return;

        if (currentSkillEnergy > 0f)
        {
            currentSkillEnergy -= Runner.DeltaTime / skillRechargeTime;
            if (currentSkillEnergy < 0f) currentSkillEnergy = 0f;
        }
    }
    private void UpdateSkillUI()
    {
        if (!Object.HasInputAuthority) return;
        if (currentSkillEnergy > 0f)
        {
            if (cooldownImage != null)
            {
                if (!cooldownImage.gameObject.activeSelf) cooldownImage.gameObject.SetActive(true);
                cooldownImage.fillAmount = currentSkillEnergy;
            }
            if (cooldownText != null)
            {
                if (!cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(true);
                if (isVomiting)
                {
                    cooldownText.text = "";
                }
                else
                {
                    float timeRemaining = currentSkillEnergy * skillRechargeTime;
                    cooldownText.text = Mathf.CeilToInt(timeRemaining).ToString();
                }
            }
        }
        else
        {
            if (cooldownImage != null && cooldownImage.gameObject.activeSelf) cooldownImage.gameObject.SetActive(false);
            if (cooldownText != null && cooldownText.gameObject.activeSelf) cooldownText.gameObject.SetActive(false);
        }
    }
    public void StartVomitEvent()
    {
        if (!Object.HasStateAuthority || typeOfHunter != HunterType.Hunter3_OiDoc || isVomiting) return;
        if (vomitPrefab == null || mouthPoint == null) return;
        isVomiting = true;
        currentSkillEnergy = 1f;
        Rpc_ToggleVomitVisuals(true);
        CancelInvoke(nameof(StopVomitEvent));
        Invoke(nameof(StopVomitEvent), vomitDuration);
    }
    public void StopVomitEvent()
    {
        if (!Object.HasStateAuthority || typeOfHunter != HunterType.Hunter3_OiDoc) return;
        isVomiting = false;
        Rpc_ToggleVomitVisuals(false);
        RestoreSpeed();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_ToggleVomitVisuals(bool isOn)
    {
        if (isOn)
        {
            if (mouthPoint != null && vomitPrefab != null)
            {
                currentVomitInstance = Instantiate(vomitPrefab, mouthPoint.position, mouthPoint.rotation);
                currentVomitInstance.transform.SetParent(mouthPoint);
            }
            if (Object.HasInputAuthority && attackSource != null)
            {
                if (clipVomitStart != null) attackSource.PlayOneShot(clipVomitStart, 1f * GetVFXVolume());
                if (clipVomitingLoop != null)
                {
                    attackSource.clip = clipVomitingLoop;
                    attackSource.volume = GetVFXVolume();
                    attackSource.Play();
                }
            }
            if (attackSource != null)
            {
                if (clipVomitStart != null) attackSource.PlayOneShot(clipVomitStart, 1f * GetVFXVolume());
                if (clipVomitingLoop != null)
                {
                    attackSource.clip = clipVomitingLoop;
                    attackSource.volume = GetVFXVolume();
                    attackSource.Play();
                }
            }
        }
        else 
        {
            if (currentVomitInstance != null) Destroy(currentVomitInstance);
            if (attackSource != null && attackSource.clip == clipVomitingLoop) attackSource.Stop();
        }
        IEnumerator UseSkillCoroutine()
        {
            float timer = 0f;
            float startEnergy = currentSkillEnergy;
            while (timer < vomitDuration && isVomiting)
            {
                timer += Runner.DeltaTime;
                currentSkillEnergy = Mathf.Lerp(startEnergy, 1f, timer / vomitDuration);
                yield return null;
            }
            currentSkillEnergy = 1f;
        }
    }
    public void PerformAttackLeft()
    {
        if (isAttacking || isVomiting) return;
        isSpecialActionLocked = false;
        Rpc_RequestAttack(animAttack, isSpecialActionLocked, Vector3.zero, Quaternion.identity);
    }

    public void PerformAttackRight()
    {
        if (isAttacking || isVomiting || isReloading) return;
        if (interactionScript != null && interactionScript.isCarryingPlayer) return;

        Vector3 sendPos = Vector3.zero;
        Quaternion sendRot = Quaternion.identity;

        switch (typeOfHunter)
        {
            case HunterType.Hunter1_NemBua:
                if (currentAmmo <= 0) return;
                isSpecialActionLocked = false;
                break;
            case HunterType.Hunter2_DatTrap:
                if (currentTrap <= 0 || trapPreviewInstance == null || !trapPreviewInstance.activeSelf) return;
                sendPos = trapPreviewInstance.transform.position;
                sendRot = trapPreviewInstance.transform.rotation;
                trapPreviewInstance.SetActive(false);
                isSpecialActionLocked = true;
                break;
            case HunterType.Hunter3_OiDoc:
                if (currentSkillEnergy > 0f) return;
                isSpecialActionLocked = false;
                break;
        }
        Rpc_RequestAttack(animThrow, isSpecialActionLocked, sendPos, sendRot);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestAttack(int attackTriggerHash, bool isLocked, Vector3 trapPos, Quaternion trapRot)
    {
        isAttacking = true;
        isSpecialActionLocked = isLocked;
        attackCounter++;
        lockedTrapPos = trapPos;
        lockedTrapRot = trapRot;
        Rpc_PlayAttackAnim(attackTriggerHash, isLocked);
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayAttackAnim(int attackTriggerHash, bool isLocked)
    {
        ani.SetTrigger(attackTriggerHash);
        if (isLocked)
        {
            if (Object.HasInputAuthority && fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = true;
            if (typeOfHunter == HunterType.Hunter2_DatTrap && movementScript != null) movementScript.ApplySlow(0f);
        }
        Invoke(nameof(ForceResetAttack), isVomiting ? vomitDuration + 0.5f : 2.5f);
    }
    public void StarSlowEffect()
    {
        if (movementScript != null)
        {
            float targetSlowMult = 1f;
            if (typeOfHunter == HunterType.Hunter2_DatTrap && isSpecialActionLocked) targetSlowMult = 0f;
            else if (typeOfHunter == HunterType.Hunter3_OiDoc && isVomiting) targetSlowMult = vomitMoveSpeedMult;
            else targetSlowMult = slowMultiplier;
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
        if (Object.HasStateAuthority) isAttacking = false;
        if (Object.HasInputAuthority && fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;
        if (typeOfHunter == HunterType.Hunter1_NemBua && leftHandHammer != null && currentAmmo > 0) leftHandHammer.SetActive(true);
        RestoreSpeed();
    }
    private void ForceResetAttack() { if (isAttacking) ResetAttack(); }
    public void UpdateAmmoUI()
    {
        if (ammoText == null || !Object.HasInputAuthority) return;
        if (typeOfHunter == HunterType.Hunter1_NemBua)
        {
            if (!ammoText.gameObject.activeSelf && !isReloading) ammoText.gameObject.SetActive(true);
            ammoText.text = currentAmmo.ToString();
        }
        else if (typeOfHunter == HunterType.Hunter2_DatTrap)
        {
            if (!ammoText.gameObject.activeSelf && !isReloading) ammoText.gameObject.SetActive(true);
            ammoText.text = currentTrap.ToString();
        }
        else if (typeOfHunter == HunterType.Hunter3_OiDoc)
        {
            ammoText.text = "";
            ammoText.gameObject.SetActive(false);
        }
    }
    public void PlaySoundSwing()
    {
        if (Object.HasInputAuthority && attackSource != null && clipChemBua != null)
            attackSource.PlayOneShot(clipChemBua, 1f * GetVFXVolume());
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void Rpc_PlayReleaseSound()
    {
        if (Object.HasInputAuthority && attackSource != null && clipReleaseSkill != null)
            attackSource.PlayOneShot(clipReleaseSkill, 1f * GetVFXVolume());
    }
    public void PlaySoundRelease() { if (Object.HasStateAuthority) Rpc_PlayReleaseSound(); }
    public void PlaySoundDatTrap() { if (typeOfHunter == HunterType.Hunter2_DatTrap && Object.HasStateAuthority) Rpc_PlayReleaseSound(); }
    public void EnableDamageFrames() { if (Object.HasStateAuthority && meleeWeapon != null) meleeWeapon.TurnOnHitbox(); }
    public void DisableDamageFrames() { if (Object.HasStateAuthority && meleeWeapon != null) meleeWeapon.TurnOffHitbox(); }
    public void OnHitSuccess(GameObject victim) { if (Object.HasStateAuthority) Rpc_PlayHitSuccessSound(); }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayHitSuccessSound()
    {
        if (Object.HasInputAuthority && attackSource != null && clipHitSuccess != null)
            attackSource.PlayOneShot(clipHitSuccess, 1f * GetVFXVolume());
    }
    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}