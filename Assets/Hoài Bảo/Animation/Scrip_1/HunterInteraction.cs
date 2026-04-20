using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using Fusion;

public class HunterInteraction : NetworkBehaviour
{
    [Header("Giao Diện & UI")]
    public Image interactImage;
    public Slider interactionSlider;

    [Header("Tự Động Tìm UI (Nhập đúng tên ngoài Hierarchy)")]
    public string interactImageName = "Imgtt";
    public string sliderUIName = "Slidertt";

    [Header("Cài Đặt Thời Gian Animation")]
    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f;
    public float timeTreoMoc = 3.0f;
    public float timeNhatPlayer = 1.5f;
    public float timeDapVan = 2.0f;

    [Header("Hệ Thống Vác Người")]
    public Transform handPoint;
    public Transform shoulderPoint;
    [Networked] public NetworkBool isCarryingPlayer { get; set; }
    private ChangeDetector _changeDetector;
    private GameObject carriedPlayerObject;

    [Header("Vượt Cửa Sổ")]
    [Tooltip("Khoảng cách phi thân qua cửa sổ. NHỚ CHỈNH ĐỦ XA ĐỂ KHÔNG BỊ KẸT TƯỜNG!")]
    public float vaultDistance = 2.5f;
    [Header("Khoảng cách tương tác an toàn")]
    public float maxInteractDistance = 4.0f;

    [Header("Âm Thanh Tương Tác")]
    public AudioSource interactAudioSource;
    public AudioClip clipDapMay;
    public AudioClip clipDapVan;
    public AudioClip clipTreoCuaso;
    public AudioClip clipTreoMoc;
    [Header("Hệ Thống Aura (Tự Động)")]
    private Material auraMatRed;
    private Material auraMatWhite;
    private GameObject[] allHooks;
    private GameObject[] allGenerators;
    private Collider currentInteractTarget;
    private float currentDuration = 1f;
    [Header("Hệ Thống Choáng (Stun)")]
    [Networked] public TickTimer StunTimer { get; set; }
    public string stunAnimationTrigger = "BeStunned";
    [Networked] private NetworkBool isInteracting { get; set; }
    [Networked] private Vector3 syncedTargetPos { get; set; }
    [Networked] private Quaternion syncedTargetRot { get; set; }
    [Networked] private NetworkBool isVaulting { get; set; }
    [Networked] private float vaultTimer { get; set; }
    [Networked] private Vector3 vStart { get; set; }
    [Networked] private Vector3 vEnd { get; set; }
    [Networked] private float syncedDuration { get; set; }
    [Header("Hệ Thống Vùng Vẫy (Wiggle)")]
    public float struggleTime = 15.0f; // Thời gian tối đa giữ người
    [Networked] public TickTimer StruggleTimer { get; set; }
    private bool isSliderRunning = false;
    private float sliderTimer = 0f;
    private Animator animator;
    private CharacterController controller;
    private FPSCamera fpsCameraScript;
    private NetworkId syncedTargetId;
    public override void Spawned()
    {
        animator = GetComponentInChildren<Animator>();
        controller = GetComponent<CharacterController>();
        if (Camera.main != null && Object.HasInputAuthority) fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        if (interactAudioSource == null) interactAudioSource = GetComponent<AudioSource>();
        if (interactAudioSource != null)
        {
            interactAudioSource.spatialBlend = 1f;
            interactAudioSource.rolloffMode = AudioRolloffMode.Linear;
            interactAudioSource.minDistance = 3f;
            interactAudioSource.maxDistance = 25f;
        }
        if (Object.HasInputAuthority) AutoFindUI();
        auraMatRed = Resources.Load<Material>("Mat_AuraRed");
        auraMatWhite = Resources.Load<Material>("Mat_AuraWhite");
        if (Object.HasInputAuthority)
        {
            allHooks = GameObject.FindGameObjectsWithTag("Moc");
            allGenerators = GameObject.FindGameObjectsWithTag("May");
            ToggleAuraGroup(allGenerators, auraMatRed, true);
        }
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(isCarryingPlayer):
                    UpdateAurasLocally();
                    break;
            }
        }
    }
    private void UpdateAurasLocally()
    {
        if (!Object.HasInputAuthority) return;
        allHooks = GameObject.FindGameObjectsWithTag("Moc");
        allGenerators = GameObject.FindGameObjectsWithTag("May");
        if (isCarryingPlayer)
        {
            ToggleAuraGroup(allGenerators, auraMatRed, false);
            ToggleAuraGroup(allGenerators, auraMatWhite, true);
            ToggleAuraGroup(allHooks, auraMatRed, true);
        }
        else
        {
            ToggleAuraGroup(allGenerators, auraMatWhite, false);
            ToggleAuraGroup(allGenerators, auraMatRed, true);
            ToggleAuraGroup(allHooks, auraMatRed, false);
        }
    }
    private void AutoFindUI()
    {
        if (interactImage == null)
        {
            GameObject foundInteractObj = FindUIObjectByName(interactImageName);
            if (foundInteractObj != null) interactImage = foundInteractObj.GetComponent<Image>();
        }

        if (interactionSlider == null)
        {
            GameObject foundSliderObj = FindUIObjectByName(sliderUIName);
            if (foundSliderObj != null) interactionSlider = foundSliderObj.GetComponent<Slider>();
        }
    }
    private GameObject FindUIObjectByName(string objName)
    {
        Canvas[] canvases = FindObjectsOfType<Canvas>(true);
        foreach (Canvas canvas in canvases)
        {
            Transform[] children = canvas.GetComponentsInChildren<Transform>(true);
            foreach (Transform child in children)
                if (child.name.Trim() == objName.Trim()) return child.gameObject;
        }
        return null;
    }
    public override void FixedUpdateNetwork()
    {
        if (Object.HasStateAuthority && isCarryingPlayer)
        {
            if (StruggleTimer.Expired(Runner))
            {
                Debug.Log("💥 [Hunter] Player vùng vẫy thoát được! Hunter bị choáng 3s.");
                StruggleTimer = TickTimer.None; 
                ApplyStun(3.0f); 
            }
        }
        if (!StunTimer.ExpiredOrNotRunning(Runner)) return;

        if (isVaulting)
        {
            if (controller != null && controller.enabled) controller.enabled = false;

            vaultTimer += Runner.DeltaTime;

            if (vaultTimer >= syncedDuration)
            {
                isVaulting = false;
                transform.position = vEnd;
            }
            else
            {
                float safeDuration = Mathf.Max(0.1f, syncedDuration);
                float t = Mathf.Clamp01(vaultTimer / safeDuration);
                transform.position = Vector3.Lerp(vStart, vEnd, t);
            }
        }
        else
        {
            if (!isInteracting && !isSliderRunning)
            {
                if (controller != null && !controller.enabled)
                {
                    Physics.SyncTransforms();
                    controller.enabled = true;
                }
            }
        }
    }
    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (isSliderRunning && interactImage != null && interactImage.gameObject.activeSelf)
        {
            interactImage.gameObject.SetActive(false);
        }
        if (isSliderRunning && interactionSlider != null)
        {
            if (!interactionSlider.gameObject.activeSelf) interactionSlider.gameObject.SetActive(true);
            sliderTimer += Time.deltaTime;
            float safeDuration = Mathf.Max(0.1f, currentDuration);
            interactionSlider.value = sliderTimer / safeDuration;
            if (sliderTimer >= safeDuration)
            {
                isSliderRunning = false;
                if (!isCarryingPlayer) interactionSlider.gameObject.SetActive(false);
            }
        }
        else if (isCarryingPlayer && interactionSlider != null)
        {
            if (!interactionSlider.gameObject.activeSelf) interactionSlider.gameObject.SetActive(true);

            if (StruggleTimer.IsRunning)
            {
                float remaining = StruggleTimer.RemainingTime(Runner) ?? 0f;
                interactionSlider.value = 1f - (remaining / struggleTime);
            }
        }
        else if (!isCarryingPlayer && !isSliderRunning && interactionSlider != null && interactionSlider.gameObject.activeSelf)
        {
            interactionSlider.gameObject.SetActive(false);
        }
        if (currentInteractTarget != null && !isInteracting && !isSliderRunning)
        {
            float dist = Vector3.Distance(transform.position, currentInteractTarget.transform.position);
            bool isInvalidGen = false;
            if (currentInteractTarget.CompareTag("May"))
            {
                Generator gen = currentInteractTarget.GetComponent<Generator>();
                if (gen != null && !gen.CanBeDamagedByHunter())
                {
                    isInvalidGen = true;
                }
            }
            if (dist > maxInteractDistance || isInvalidGen)
            {
                currentInteractTarget = null;
                if (interactImage != null) interactImage.gameObject.SetActive(false);
            }
        }
    }
    public void ApplyStun(float duration)
    {
        if (!Object.HasStateAuthority) return;
        StunTimer = TickTimer.CreateFromSeconds(Runner, duration);
        Rpc_PlayStunEffects();
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayStunEffects()
    {
        if (animator != null) animator.SetTrigger(stunAnimationTrigger);
        if (isCarryingPlayer) DropPlayerLogic();
    }
    private void DropPlayerLogic()
    {
        if (carriedPlayerObject != null)
        {
            PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
            if (receiver != null) receiver.ReleaseFromHunter();
            if (Object.HasStateAuthority)
            {
                var s1 = carriedPlayerObject.GetComponent<IShowSpeedController_Fusion>();
                if (s1 != null) s1.EscapeFromHunter();
                var s2 = carriedPlayerObject.GetComponent<MrBeanController_Fusion>();
                if (s2 != null) s2.EscapeFromHunter();
                var s3 = carriedPlayerObject.GetComponent<MrBeastController_Fusion>();
                if (s3 != null) s3.EscapeFromHunter();
                var s4 = carriedPlayerObject.GetComponent<NurseController_Fusion>();
                if (s4 != null) s4.EscapeFromHunter();
            }
        }
        isCarryingPlayer = false;
        carriedPlayerObject = null;
        if (Object.HasStateAuthority) StruggleTimer = TickTimer.None;
        Debug.Log("💥 Hunter bị choáng và làm rơi Survivor! Survivor chạy thoát với 2 Hit.");
        if (Object.HasInputAuthority)
        {
            ToggleAuraGroup(allGenerators, auraMatWhite, false);
            ToggleAuraGroup(allGenerators, auraMatRed, true);
            ToggleAuraGroup(allHooks, auraMatRed, false);
        }
    }
    public bool IsDoingAction()
    {
        bool isStunned = !StunTimer.ExpiredOrNotRunning(Runner);
        return isInteracting || isVaulting || isSliderRunning || isStunned;
    }

    public void TryInteract()
    {
        if (isInteracting || isSliderRunning)
        {
            Debug.LogWarning("❌ [Hunter] Đang tương tác, không nhận lệnh!");
            return;
        }
        if (currentInteractTarget == null) return;
        float distToTarget = Vector3.Distance(transform.position, currentInteractTarget.transform.position);
        if (distToTarget > maxInteractDistance)
        {
            Debug.Log("❌ [Hunter] Đã đi quá xa mục tiêu, tự động hủy!");
            currentInteractTarget = null;
            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
            return;
        }
        string tag = currentInteractTarget.tag;
        if (isCarryingPlayer && tag != "Moc") return;
        if (tag == "Moc" && !isCarryingPlayer) return;
        if (tag == "May")
        {
            Generator gen = currentInteractTarget.GetComponent<Generator>();
            if (gen == null || !gen.CanBeDamagedByHunter())
            {
                Debug.Log("❌ [Hunter] Máy chưa có tiến trình hoặc đã bị đạp rồi, không thể đạp spam!");
                currentInteractTarget = null;
                if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
                return;
            }
        }
        if (tag == "Playerchet")
        {
            if (!currentInteractTarget.gameObject.activeInHierarchy) return;
            bool canPickUp = false;
            var s1 = currentInteractTarget.GetComponentInParent<IShowSpeedController_Fusion>();
            if (s1 != null && s1.IsDowned && !s1.IsHooked) canPickUp = true;
            var s2 = currentInteractTarget.GetComponentInParent<MrBeanController_Fusion>();
            if (s2 != null && s2.IsDowned && !s2.IsHooked) canPickUp = true;
            var s3 = currentInteractTarget.GetComponentInParent<MrBeastController_Fusion>();
            if (s3 != null && s3.IsDowned && !s3.IsHooked) canPickUp = true;
            var s4 = currentInteractTarget.GetComponentInParent<NurseController_Fusion>();
            if (s4 != null && s4.IsDowned && !s4.IsHooked) canPickUp = true;
            if (!canPickUp)
            {
                Debug.Log("❌ [Hunter] Mục tiêu không ở trạng thái gục hoặc đã bị treo!");
                currentInteractTarget = null;
                return;
            }
            currentDuration = timeNhatPlayer;
        }
        else if (tag == "May") currentDuration = timeDapMay;
        else if (tag == "Moc") currentDuration = timeTreoMoc;
        else if (tag == "Cuaso") currentDuration = timeTreoCUASO;
        else if (tag == "VanDaNga") currentDuration = timeDapVan; 
        if (Object.HasInputAuthority)
        {
            if (interactImage != null) interactImage.gameObject.SetActive(false);
            if (interactionSlider != null)
            {
                interactionSlider.gameObject.SetActive(true);
                interactionSlider.value = 0f;
                sliderTimer = 0f;
                isSliderRunning = true;
            }
            if (controller != null) controller.enabled = false;
        }
        NetworkObject netObj = currentInteractTarget.GetComponentInParent<NetworkObject>();
        NetworkId idToSend = netObj != null ? netObj.Id : default;
        Vector3 exactTargetPos = currentInteractTarget.transform.position;
        Quaternion exactTargetRot = currentInteractTarget.transform.rotation;
        if (tag == "Moc")
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            if (hookPoint != null)
            {
                exactTargetPos = hookPoint.position;
                exactTargetRot = hookPoint.rotation;
            }
        }
        Rpc_RequestInteraction(tag, exactTargetPos, exactTargetRot, idToSend);
    }
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void Rpc_RequestInteraction(string tag, Vector3 targetPosition, Quaternion targetRotation, NetworkId targetId)
    {
        isInteracting = true;
        syncedTargetId = targetId;
        syncedTargetPos = targetPosition;
        syncedTargetRot = targetRotation;
        Rpc_PlayInteractionEffects(tag, targetPosition);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayInteractionEffects(string tag, Vector3 targetPosition)
    {
        Vector3 lookPos = targetPosition;
        lookPos.y = transform.position.y;
        transform.LookAt(lookPos);

        if (Object.HasInputAuthority && fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true;
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y);
        }
        if (tag == "May") animator.SetTrigger("Dapmay");
        else if (tag == "VanDaNga") animator.SetTrigger("Dapmay"); 
        else if (tag == "Moc") animator.SetTrigger("Treomoc");
        else if (tag == "Playerchet")
        {
            animator.SetTrigger("Nhacplayer");
            NetworkObject targetObj = Runner.FindObject(syncedTargetId);
            if (targetObj != null) carriedPlayerObject = targetObj.gameObject;
        }
        else if (tag == "Cuaso")
        {
            animator.SetTrigger("Treocuaso");
            if (Object.HasStateAuthority)
            {
                isVaulting = true;
                vStart = transform.position;
                vEnd = transform.position + transform.forward * vaultDistance;
                vaultTimer = 0f;
                syncedDuration = timeTreoCUASO;
            }
        }
    }
    public void AttachPlayerToHand()
    {
        if (carriedPlayerObject != null && handPoint != null)
        {
            Transform interactionTrigger = carriedPlayerObject.transform.Find("Playerchet");
            if (interactionTrigger == null) interactionTrigger = FindChildWithTag(carriedPlayerObject, "Playerchet");
            if (interactionTrigger != null) interactionTrigger.gameObject.SetActive(false);
            if (Object.HasStateAuthority)
            {
                PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
                if (receiver != null) receiver.GetPickedUpOrHooked(handPoint);
            }
            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
            currentInteractTarget = null;
        }
    }
    public void AttachPlayerToShoulder()
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            if (Object.HasStateAuthority)
            {
                PlayerHookReceiver receiver = carriedPlayerObject.GetComponent<PlayerHookReceiver>();
                if (receiver != null) receiver.GetPickedUpOrHooked(shoulderPoint);
                isCarryingPlayer = true;
                StruggleTimer = TickTimer.CreateFromSeconds(Runner, struggleTime);
            }

            if (Object.HasInputAuthority)
            {
                ToggleAuraGroup(allGenerators, auraMatRed, false);
                ToggleAuraGroup(allGenerators, auraMatWhite, true);
                ToggleAuraGroup(allHooks, auraMatRed, true);
            }
        }
    }
    public void HookPlayerToHook()
    {
        if (carriedPlayerObject != null && currentInteractTarget != null)
        {
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            Vector3 finalPos = hookPoint ? hookPoint.position : currentInteractTarget.transform.position;
            Quaternion finalRot = hookPoint ? hookPoint.rotation : currentInteractTarget.transform.rotation;
            if (Object.HasStateAuthority)
            {
                var s1 = carriedPlayerObject.GetComponent<IShowSpeedController_Fusion>();
                if (s1 != null) s1.GetHooked(finalPos, finalRot);
                var s2 = carriedPlayerObject.GetComponent<MrBeanController_Fusion>();
                if (s2 != null) s2.GetHooked(finalPos, finalRot);
                var s3 = carriedPlayerObject.GetComponent<MrBeastController_Fusion>();
                if (s3 != null) s3.GetHooked(finalPos, finalRot);
                var s4 = carriedPlayerObject.GetComponent<NurseController_Fusion>();
                if (s4 != null) s4.GetHooked(finalPos, finalRot);
                isCarryingPlayer = false;
                StruggleTimer = TickTimer.None;
            }
            if (Object.HasInputAuthority)
            {
                ToggleAuraGroup(allGenerators, auraMatWhite, false);
                ToggleAuraGroup(allGenerators, auraMatRed, true);
                ToggleAuraGroup(allHooks, auraMatRed, false);

            }
            if (currentInteractTarget != null) currentInteractTarget.tag = "Untagged";
            carriedPlayerObject = null;
            currentInteractTarget = null;
        }
    }
    private Transform FindChildWithTag(GameObject parent, string tag)
    {
        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
            if (child.CompareTag(tag)) return child;
        return null;
    }
    public void FinishInteraction()
    {
        if (Object.HasStateAuthority)
        {
            isInteracting = false;
        }
        if (Object.HasInputAuthority)
        {
            if (fpsCameraScript != null) fpsCameraScript.isCameraLockedForAnim = false;

            isSliderRunning = false;
            if (interactionSlider != null) { interactionSlider.value = 1f; interactionSlider.gameObject.SetActive(false); }
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (isInteracting || isSliderRunning) return;
        if (other.transform.root == transform.root) return;
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Playerchet") || other.CompareTag("Cuaso") || other.CompareTag("VanDaNga"))
        {
            if (isCarryingPlayer && !other.CompareTag("Moc")) return;
            if (other.CompareTag("Moc") && !isCarryingPlayer) return;
            if (other.CompareTag("Cuaso"))
            {
                Vector3 dirToWindow = (other.transform.position - transform.position).normalized;
                float dot = Vector3.Dot(transform.forward, dirToWindow);
                if (dot < 0.3f) return;
            }
            if (other.CompareTag("May"))
            {
                Generator gen = other.GetComponent<Generator>();
                if (gen == null || !gen.CanBeDamagedByHunter()) return;
            }
            if (other.CompareTag("Playerchet"))
            {
                bool showUI = false;
                var s1 = other.GetComponentInParent<IShowSpeedController_Fusion>();
                if (s1 != null && s1.IsDowned && !s1.IsHooked) showUI = true;
                var s2 = other.GetComponentInParent<MrBeanController_Fusion>();
                if (s2 != null && s2.IsDowned && !s2.IsHooked) showUI = true;
                var s3 = other.GetComponentInParent<MrBeastController_Fusion>();
                if (s3 != null && s3.IsDowned && !s3.IsHooked) showUI = true;
                var s4 = other.GetComponentInParent<NurseController_Fusion>();
                if (s4 != null && s4.IsDowned && !s4.IsHooked) showUI = true;
                if (!showUI) return;
            }
            currentInteractTarget = other;
            if (Object.HasInputAuthority)
            {
                if (interactImage == null || interactionSlider == null) AutoFindUI();
                if (interactImage != null) interactImage.gameObject.SetActive(true);
                if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (isInteracting && !other.CompareTag("Cuaso")) return;
        if (currentInteractTarget == other)
        {
            currentInteractTarget = null;
            if (Object.HasInputAuthority && interactImage != null) interactImage.gameObject.SetActive(false);
        }
    }
    private void ToggleAuraGroup(GameObject[] objects, Material targetMat, bool turnOn)
    {
        if (targetMat == null) return;
        foreach (GameObject obj in objects)
        {
            if (obj == null) continue;
            bool shouldTurnOn = turnOn;
            if (obj.CompareTag("May"))
            {
                Generator gen = obj.GetComponent<Generator>();
                if (gen != null && gen.IsRepaired)
                {
                    shouldTurnOn = false; 
                }
            }
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                if (r is ParticleSystemRenderer) continue;
                Material[] currentMats = r.materials;
                bool hasAura = false;
                foreach (Material m in currentMats) { if (m.name.Contains(targetMat.name)) hasAura = true; }
                if (shouldTurnOn && !hasAura)
                {
                    Material[] newMats = new Material[currentMats.Length + 1];
                    for (int i = 0; i < currentMats.Length; i++) newMats[i] = currentMats[i];
                    newMats[currentMats.Length] = targetMat;
                    r.materials = newMats;
                }
                else if (!shouldTurnOn && hasAura)
                {
                    List<Material> cleanedMats = new List<Material>();
                    foreach (Material m in currentMats) { if (!m.name.Contains(targetMat.name)) cleanedMats.Add(m); }
                    r.materials = cleanedMats.ToArray();
                }
            }
        }
    }
    public void EventDapMay()
    {
        if (Object.HasInputAuthority && interactAudioSource != null && clipDapMay != null)
            interactAudioSource.PlayOneShot(clipDapMay, 1f * GetVFXVolume());
        if (Object.HasStateAuthority)
        {
            NetworkObject targetObj = Runner.FindObject(syncedTargetId);
            if (targetObj != null)
            {
                Generator gen = targetObj.GetComponent<Generator>();
                if (gen != null) gen.DamageByHunterServer();
            }
        }
    }
    public void EventDapVan()
    {
        if (Object.HasInputAuthority && interactAudioSource != null && clipDapVan != null)
            interactAudioSource.PlayOneShot(clipDapVan, 1f * GetVFXVolume());
        if (Object.HasStateAuthority)
        {
            NetworkObject targetObj = Runner.FindObject(syncedTargetId);
            if (targetObj != null)
            {
                var pallet = targetObj.GetComponent<PalletInteraction>();
                if (pallet != null) pallet.Rpc_DestroyPallet();
            }
        }
        HunterMovement movement = GetComponent<HunterMovement>();
        if (movement != null)
        {
            movement.ApplySlow(0.1f);
            movement.Invoke("ResetSlow", 0.5f);
        }
    }
    public void EventVault()
    {
        if (Object.HasInputAuthority && isVaulting && interactAudioSource != null && clipTreoCuaso != null)
            interactAudioSource.PlayOneShot(clipTreoCuaso, 1f * GetVFXVolume());
    }
    public void EventTreoMoc()
    {
        if (isCarryingPlayer)
        {
            if (Object.HasInputAuthority && interactAudioSource != null && clipTreoMoc != null)
                interactAudioSource.PlayOneShot(clipTreoMoc, 1f * GetVFXVolume());
            HookPlayerToHook();
        }
    }
    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}