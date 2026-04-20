using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; 
using UnityEngine.UI;
using TMPro;
using System; 
using System.Collections.Generic;
using Fusion.Sockets; 
[RequireComponent(typeof(CharacterController))]
public class IShowSpeedController_Fusion : NetworkBehaviour, INetworkRunnerCallbacks, ISurvivor
{
    [Header("Animator Settings")]
    public Animator animator;
    [Header("Movement Settings")]
    public float slowWalkSpeed = 2f;
    public float mediumRunSpeed = 5f;
    public float sprintSpeed = 8f;
    public float rotationSpeed = 10f;
    [Header("Input Settings")]
    public InputActionReference moveInput;
    public InputActionReference sprintInput;
    public InputActionReference walkInput;
    public InputActionReference vaultInput;
    public InputActionReference skillInput;
    private bool _vaultPressed;
    private bool _skillPressed;
    [Header("Window Vaulting Settings")]
    public string vaultAnimationTrigger = "Vault";
    public float vaultDuration = 1.5f;
    public float vaultDistance = 2.5f;
    [Header("Speed Skill Settings")]
    public float skillSpeedBonus = 3f;
    public float skillDuration = 10f;
    public float skillCooldown = 30f;
    [Header("Health & States")]
    public string hitAnimationTrigger = "AnHit";
    public string downedAnimationBool = "BiGuc";
    public string hookedAnimationBool = "BiTreo";
    public float sacrificeTime = 90f;
    [Header("UI References (Local Only)")]
    public GameObject interactUI;
    public Slider durationSlider;
    public Image cooldownImage;
    public TextMeshProUGUI cooldownText;
    public Slider hookSlider;
    [Header("== HEARTBEAT UI ==")]
    public TextMeshProUGUI heartbeatBpmText;
    public RectTransform heartIcon;
    public RectTransform ecgPen;          
    public float normalBPM = 70f;
    public float maxBPM = 180f;
    [Header("ECG Settings")]
    public float ecgSpeed = 200f;            
    public float ecgWidth = 400f;           
    private float _heartPhase = 0f;        
    private float _bpmUpdateTimer = 0f;     
    private float _displayedBPM = 70f;       
    [Header("== UI DOTTED TRAIL (Thay thế Trail Renderer) ==")]
    public GameObject dotPrefab;
    private List<Image> _spawnedDots = new List<Image>();
    private Vector2 _lastPenPos = Vector2.zero;
    [Header("X-Ray Material Settings")]
    public Material xrayMaterial; 
    private Dictionary<Renderer, Material[]> _originalMaterials = new Dictionary<Renderer, Material[]>();
    private bool _isXRayActive = false;
    [Header("Camera Reference")]
    public Transform mainCamera;
    [Header("PLayer State")]
    [Networked] private TickTimer InvincibilityTimer { get; set; }
    [Header("Player Death Boxes")]
    public List<GameObject> playerDeathBoxes; 
    [Header("Revive & Unhook Settings")]
    public float reviveTime = 5f;  
    public float unhookTime = 2f;  
    public string revivingAnimBool = "IsReviving";
    public string unhookingAnimBool = "IsUnhooking"; 
    private bool _isCancelRpcSent = false; 
    private bool _isStartRpcSent = false;
    private bool _isInteractHolding = false;
    private ISurvivor _targetToRevive; 
    public InputActionReference interactInput;
    public GameObject revivePrompt;
    public Slider reviveSlider;
    [Header("Tùy Chỉnh Lệch Móc")]
    public Vector3 hookOffset = Vector3.zero;
    [Networked] public NetworkBool IsBeingRevived { get; set; }
    [Networked] public NetworkBool IsBeingUnhooked { get; set; } 
    [Networked] public int ReviverCount { get; set; }
    [Networked] public NetworkId TargetReviveId { get; set; }
    [Networked] public float BonusRescueSpeed { get; set; }
    [Networked] public NetworkBool IsReviving { get; set; }
    [Networked] public NetworkBool IsUnhooking { get; set; } 
    [Networked] public float ReviveProgress { get; set; } 
    [Networked] public float UnhookProgress { get; set; } 
    [Header("== SOUND EFFECTS (VFX) ==")]
    public AudioSource playerAudioSource; 
    public AudioSource heartbeatSource;   

    public AudioClip[] footstepSounds; 
    public AudioClip hitSound;         
    public AudioClip vaultSound;       
    public AudioClip deathSound;       
    [Header("== HEARTBEAT SETTINGS ==")]
    public float terrorRadius = 30f; 
    public float maxHeartbeatPitch = 1.8f; 
    private CharacterController _characterController;
    private bool _isNearWindow = false;
    private bool _isLocalRepairing = false;
    [Networked] public NetworkBool IsDowned { get; set; }
    [Networked] public NetworkBool IsHooked { get; set; }
    [Networked] public int CurrentHits { get; set; }
    [Networked] public NetworkBool IsVaulting { get; set; }
    [Networked] private TickTimer HitDecayTimer { get; set; }
    [Networked] private TickTimer SkillDurationTimer { get; set; }
    [Networked] private TickTimer SkillCooldownTimer { get; set; }
    [Networked] private TickTimer VaultTimer { get; set; }
    [Networked] private TickTimer SacrificeTimer { get; set; }
    [Networked] private Vector3 VaultStartPos { get; set; }
    [Networked] private Vector3 VaultTargetPos { get; set; }
    [Networked] public NetworkBool IsGameStarted { get; set; } = false;
    [Networked] public float AnimSpeedValue { get; set; }
    [Networked] private float velocityY { get; set; }
    [Networked] public NetworkBool IsRepairingAnim { get; set; }
    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        SkinnedMeshRenderer[] renderers = GetComponentsInChildren<SkinnedMeshRenderer>(true);
        foreach (var r in renderers)
        {
            _originalMaterials[r] = r.sharedMaterials;
        }
        if (_characterController != null) _characterController.enabled = false;
        if (Object.HasInputAuthority)
        {
            if (mainCamera == null)
            {
                if (Camera.main != null) mainCamera = Camera.main.transform;
                else Debug.LogWarning("Không tìm thấy Camera.main! Di chuyển sẽ dùng World Space mặc định.");
            }
            InitUI();
            if (moveInput) moveInput.action.Enable();
            if (sprintInput) sprintInput.action.Enable();
            if (walkInput) walkInput.action.Enable();
            if (vaultInput) vaultInput.action.Enable();
            if (skillInput) skillInput.action.Enable();
            if (interactInput) interactInput.action.Enable();
            Runner.AddCallbacks(this);
        }
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            StartCoroutine(EnableCharacterControllerRoutine());
        }
        if (Runner.IsServer) IsGameStarted = true;
    }
    private System.Collections.IEnumerator EnableCharacterControllerRoutine()
    {
        yield return null; 
        if (_characterController != null) _characterController.enabled = true;
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (Object.HasInputAuthority) Runner.RemoveCallbacks(this);
    }
    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (vaultInput.action.triggered) _vaultPressed = true;
        if (skillInput.action.triggered) _skillPressed = true;
        _isInteractHolding = interactInput.action.IsPressed();
    }
    private void InitUI()
    {
        if (interactUI) interactUI.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (cooldownImage) cooldownImage.gameObject.SetActive(false);
        if (cooldownText) { cooldownText.text = ""; cooldownText.gameObject.SetActive(false); }
        if (hookSlider) hookSlider.gameObject.SetActive(false);
    }
    public override void FixedUpdateNetwork()
    {
        if (!IsGameStarted) return;
        if (GetInput(out IShowSpeedGameplayInput input))
        {
            HandleReviveLogic(input);
            if (CurrentHits > 0 && HitDecayTimer.Expired(Runner)) CurrentHits = 0;
            if (IsHooked && SacrificeTimer.Expired(Runner))
            {
                if (deathSound != null) PlayDetachedSound(deathSound, transform.position);
                if (Object.HasStateAuthority)
                {
                    GameMatchManager_Fusion.Instance?.RegisterPlayerDeath(Object.InputAuthority);
                }
                Runner.Despawn(Object);
                return;
            }
            if (IsDowned || IsHooked) return;
            if (IsVaulting) { HandleVaultingMovement(); return; }
            HandleSkillInput(input);
            HandleMovement(input);
            HandleWindowInput(input);
        }
        else
        {
            HandleReviveLogic(default);
        }
    }
    public override void Render()
    {
        animator.SetBool(downedAnimationBool, IsDowned);
        animator.SetBool(hookedAnimationBool, IsHooked);
        animator.SetBool(revivingAnimBool, IsReviving);    
        animator.SetBool(unhookingAnimBool, IsUnhooking);  
        animator.SetBool("SuaMay", IsRepairingAnim);
        float currentAnimSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, AnimSpeedValue, Time.deltaTime * 15f));
        if (!Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            if (_characterController != null && _characterController.enabled)
            {
                _characterController.enabled = false;
            }
        }
        if (playerDeathBoxes != null && playerDeathBoxes.Count > 0)
        {
            foreach (var box in playerDeathBoxes)
            {
                if (box != null) box.SetActive(IsDowned || IsHooked);
            }
        }
        bool shouldShowXRay = (IsDowned || IsHooked) && !Object.HasInputAuthority;
        SetXRayActive(shouldShowXRay);

        if (Object.HasInputAuthority)
        {
            UpdateSkillUI();
            UpdateHookUI();
            bool canShowE = !IsDowned && !IsHooked && IsNearDeadBody() && !IsReviving && !IsUnhooking;
            if (revivePrompt) revivePrompt.SetActive(canShowE);
            UpdateReviveProgressUI();
            UpdateHeartbeat();
        }
        if (!IsDowned && !IsHooked && transform.parent != null)
        {
            transform.SetParent(null);
        }
    }
    public void SetRepairAnimation(bool isRepairing)
    {
        IsRepairingAnim = isRepairing;
    }
    private bool IsNearDeadBody()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.2f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("CuuPlayer")) return true;
        }
        return false;
    }
    private void HandleMovement(IShowSpeedGameplayInput input)
    {
        _characterController.enabled = false;
        _characterController.enabled = true;
        Vector3 direction = CalculateDirection(input.moveDirection, input.camForward, input.camRight);
        if (direction.magnitude > 1f) direction.Normalize();
        float speed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;
        bool skillActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);
        if (_isLocalRepairing || IsRepairingAnim || IsReviving || IsUnhooking)
        {
            if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
            velocityY += -9.81f * Runner.DeltaTime;
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);
            AnimSpeedValue = 0f; 
            return; 
        }
        if (skillActive)
        {
            speed = sprintSpeed + skillSpeedBonus;
            targetAnimSpeed = 1f;
        }
        else
        {
            if (input.isWalking) { speed = slowWalkSpeed; targetAnimSpeed = 0.2f; }
            else if (input.isSprinting) { speed = sprintSpeed; targetAnimSpeed = 1f; }
        }
        if (direction.magnitude == 0) targetAnimSpeed = 0f;
        if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += -9.81f * Runner.DeltaTime;
        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveVelocity = direction * speed;
            moveVelocity.y = velocityY; 
            _characterController.Move(moveVelocity * Runner.DeltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);
        }

        AnimSpeedValue = targetAnimSpeed;
    }
    private void HandleVaultingMovement()
    {
        if (VaultTimer.Expired(Runner))
        {
            IsVaulting = false;
            if (Object.HasStateAuthority || Object.HasInputAuthority)
            {
                if (_characterController != null) _characterController.enabled = true;
            }
            return;
        }
        if (_characterController != null) _characterController.enabled = false;
        float t = 1f - (VaultTimer.RemainingTime(Runner).Value / vaultDuration);
        t = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(VaultStartPos, VaultTargetPos, t);
    }
    private void HandleWindowInput(IShowSpeedGameplayInput input)
    {
        if (_isNearWindow && input.isVaulting)
        {
            IsVaulting = true;
            _isNearWindow = false;
            VaultStartPos = transform.position;
            VaultTargetPos = transform.position + (transform.forward * vaultDistance);
            VaultTimer = TickTimer.CreateFromSeconds(Runner, vaultDuration);
            RPC_PlayVaultAnim();
        }
    }
    private void HandleSkillInput(IShowSpeedGameplayInput input)
    {
        if (input.isSkill && SkillCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            SkillDurationTimer = TickTimer.CreateFromSeconds(Runner, skillDuration);
            SkillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillDuration + skillCooldown);
        }
    }
    public void TakeHit()
    {
        if (IsDowned || IsHooked || !Object.HasStateAuthority) return;
        if (!InvincibilityTimer.ExpiredOrNotRunning(Runner)) return;
        CurrentHits++;
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);
        bool isMoving = _characterController.velocity.magnitude > 0.1f;
        RPC_PlayHitAnim(isMoving);
        if (CurrentHits == 1) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 35f);
        else if (CurrentHits == 2) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 90f);
        else if (CurrentHits >= 3)
        {
            IsDowned = true;
            _characterController.enabled = false;
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim(NetworkBool isMoving)
    {
        animator.SetBool("IsMoving", isMoving);
        animator.SetTrigger(hitAnimationTrigger);
        if (playerAudioSource != null && hitSound != null)
        {
            playerAudioSource.PlayOneShot(hitSound, 1f * GetVFXVolume());
        }
    }
    public void GetHooked(Vector3 hookPos, Quaternion hookRot)
    {
        if (IsHooked || !Object.HasStateAuthority) return;
        IsDowned = false;
        IsHooked = true;
        PlayerHookReceiver hookReceiver = GetComponent<PlayerHookReceiver>();
        if (hookReceiver != null) hookReceiver.ReleaseFromHunter();
        if (_characterController != null) _characterController.enabled = false;
        Vector3 adjustedPos = hookPos + (hookRot * hookOffset);
        transform.position = adjustedPos;
        transform.rotation = hookRot;
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Teleport(adjustedPos, hookRot);
        }

        SacrificeTimer = TickTimer.CreateFromSeconds(Runner, sacrificeTime);
    }
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayVaultAnim()
    {
        animator.SetTrigger(vaultAnimationTrigger);
        if (playerAudioSource != null && vaultSound != null)
        {
            playerAudioSource.PlayOneShot(vaultSound, 0.8f * GetVFXVolume());
        }
    }
    private void UpdateSkillUI()
    {
        bool durationActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);
        durationSlider.gameObject.SetActive(durationActive);
        if (durationActive) durationSlider.value = SkillDurationTimer.RemainingTime(Runner).Value / skillDuration;
        float? cdLeft = SkillCooldownTimer.RemainingTime(Runner);
        bool onCooldown = cdLeft > 0 && SkillDurationTimer.ExpiredOrNotRunning(Runner);
        cooldownImage.gameObject.SetActive(cdLeft > 0);
        if (cdLeft > 0) cooldownImage.fillAmount = cdLeft.Value / skillCooldown;
        cooldownText.gameObject.SetActive(onCooldown);
        if (onCooldown) cooldownText.text = Mathf.Ceil(cdLeft.Value).ToString();
    }
    private void UpdateHookUI()
    {
        hookSlider.gameObject.SetActive(IsHooked);
        if (IsHooked)
        {
            float timeLeft = SacrificeTimer.RemainingTime(Runner) ?? 0f;
            hookSlider.value = timeLeft / sacrificeTime;
        }
    }
    private Vector3 CalculateDirection(Vector2 inputDir, Vector3 camFwd, Vector3 camRight)
    {
        if (camFwd == Vector3.zero && camRight == Vector3.zero)
        {
            return new Vector3(inputDir.x, 0, inputDir.y).normalized;
        }
        Vector3 forward = Vector3.Scale(camFwd, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Vector3.Scale(camRight, new Vector3(1, 0, 1)).normalized;
        return (forward * inputDir.y + right * inputDir.x);
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Cuaso")) _isNearWindow = true;
        else if (other.CompareTag("HuntetHit")) TakeHit();
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cuaso")) _isNearWindow = false;
        else if (Object.HasInputAuthority && other.CompareTag("CuuPlayer"))
        {
            if (IsReviving || IsUnhooking) RPC_SetReviveState(false, default, false);
            _targetToRevive = null;
        }
    }
    private void OnTriggerStay(Collider other)
    {
        if (!Object.HasInputAuthority) return;
        if (!IsDowned && !IsHooked && other.CompareTag("CuuPlayer"))
        {
            var target = other.GetComponentInParent<ISurvivor>();
            if (target != null && (target.GetIsDowned() || target.GetIsHooked()) && target.Object.Id != this.Object.Id)
            {
                bool isTargetHooked = target.GetIsHooked();
                if (isTargetHooked && target.GetIsBeingUnhooked() && !IsUnhooking)
                {
                    if (revivePrompt) revivePrompt.SetActive(false); 
                    return; 
                }
                _targetToRevive = target;
                if (_isInteractHolding)
                {
                    if (!IsReviving && !IsUnhooking && !_isStartRpcSent)
                    {
                        RPC_SetReviveState(true, target.Object.Id, isTargetHooked);
                        _isStartRpcSent = true;
                        _isCancelRpcSent = false;
                    }
                }
            }
        }
    }
    private void CompleteRevive()
    {
        IsDowned = false;
        IsBeingRevived = false;
        _characterController.enabled = true;
        if (playerDeathBoxes != null)
        {
            foreach (var box in playerDeathBoxes)
            {
                if (box != null) box.SetActive(false);
            }
        }
        SetXRayActive(false);
        CurrentHits = 1;
    }
    private void UpdateReviveProgressUI()
    {
        if (reviveSlider == null) return;
        bool showingSlider = IsReviving || IsUnhooking || IsBeingRevived || IsBeingUnhooked;
        reviveSlider.gameObject.SetActive(showingSlider);
        if (showingSlider)
        {
            float progress = 0f;
            if (IsBeingRevived || IsBeingUnhooked)
                progress = GetRescueProgressRatio();
            else if (_targetToRevive != null && _targetToRevive.Object != null && _targetToRevive.Object.IsValid)
                progress = _targetToRevive.GetRescueProgressRatio();
            else
                _targetToRevive = null;

            reviveSlider.value = Mathf.Clamp01(progress);
        }
    }    private float GetVFXVolume()
    {
        return (AudioManager.Instance != null) ? AudioManager.Instance.vfxVolume : 1f;
    }
    public void PlayFootstepSound()
    {
        if (footstepSounds.Length == 0 || playerAudioSource == null) return;
        if (_characterController.velocity.magnitude > 0.1f)
        {
            AudioClip clip = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
            playerAudioSource.PlayOneShot(clip, 0.6f * GetVFXVolume());
        }
    }
    private void UpdateHeartbeat()
    {
        if (!Object.HasInputAuthority) return;
        GameObject[] hunters = GameObject.FindGameObjectsWithTag("Hunter");
        float minDistance = float.MaxValue;
        foreach (var h in hunters)
        {
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist < minDistance) minDistance = dist;
        }
        float intensity = 0f; 
        if (minDistance <= terrorRadius)
        {
            intensity = 1f - (minDistance / terrorRadius);
            if (heartbeatSource != null)
            {
                heartbeatSource.volume = intensity * GetVFXVolume();
                heartbeatSource.pitch = Mathf.Lerp(1f, maxHeartbeatPitch, intensity);
                if (!heartbeatSource.isPlaying) heartbeatSource.Play();
            }
        }
        else
        {
            if (heartbeatSource != null) heartbeatSource.volume = 0f;
        }
        UpdateHeartbeatUI(intensity);
    }
    private void UpdateHeartbeatUI(float intensity)
    {
        float targetBPM = Mathf.Lerp(normalBPM, maxBPM, intensity);
        _bpmUpdateTimer -= Time.deltaTime;
        if (_bpmUpdateTimer <= 0f)
        {
            _displayedBPM = targetBPM + UnityEngine.Random.Range(-4f, 4f);
            if (heartbeatBpmText != null)
            {
                heartbeatBpmText.text = Mathf.RoundToInt(_displayedBPM).ToString();
                heartbeatBpmText.color = Color.Lerp(Color.white, Color.red, intensity);
            }
            _bpmUpdateTimer = 0.5f;
        }
        float bps = targetBPM / 60f;
        _heartPhase += Time.deltaTime * bps;
        if (_heartPhase > 1f) _heartPhase -= 1f;
        if (ecgPen != null)
        {
            float halfWidth = ecgWidth / 2f;
            float currentX = ecgPen.anchoredPosition.x + (ecgSpeed * Time.deltaTime);
            if (currentX > halfWidth) currentX = -halfWidth;
            float yOffset = 0f;
            float rSpikeHeight = 50f + (intensity * 40f);
            if (_heartPhase > 0.10f && _heartPhase < 0.15f) yOffset = 10f;
            else if (_heartPhase > 0.20f && _heartPhase < 0.22f) yOffset = -15f;
            else if (_heartPhase >= 0.22f && _heartPhase < 0.26f) yOffset = rSpikeHeight;
            else if (_heartPhase >= 0.26f && _heartPhase < 0.30f) yOffset = -25f;
            else if (_heartPhase > 0.45f && _heartPhase < 0.55f) yOffset = 15f;
            ecgPen.anchoredPosition = new Vector2(currentX, Mathf.Lerp(ecgPen.anchoredPosition.y, yOffset, Time.deltaTime * 30f));
            Vector2 currPos = ecgPen.anchoredPosition;
            if (_lastPenPos != Vector2.zero && Vector2.Distance(currPos, _lastPenPos) < halfWidth)
            {
                if (dotPrefab != null)
                {
                    GameObject dot = Instantiate(dotPrefab, ecgPen.parent);
                    RectTransform dotRect = dot.GetComponent<RectTransform>();
                    dotRect.anchoredPosition = (currPos + _lastPenPos) / 2f;
                    float dist = Vector2.Distance(currPos, _lastPenPos);
                    dotRect.sizeDelta = new Vector2(dist + 0.5f, 5f);
                    Vector2 dir = currPos - _lastPenPos;
                    float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
                    dotRect.localRotation = Quaternion.Euler(0, 0, angle);
                    dotRect.SetAsFirstSibling();
                    Image dotImg = dot.GetComponent<Image>();
                    dotImg.color = new Color(
                        Mathf.Lerp(0f, 1f, intensity), 
                        Mathf.Lerp(1f, 0f, intensity),
                        0f,
                        1f 
                    );
                    _spawnedDots.Add(dotImg);
                }
            }
            _lastPenPos = currPos;
            float shrinkSpeed = 5f / (ecgWidth / ecgSpeed); 
            for (int i = _spawnedDots.Count - 1; i >= 0; i--)
            {
                Image img = _spawnedDots[i];
                if (img == null) { _spawnedDots.RemoveAt(i); continue; }
                img.rectTransform.sizeDelta -= new Vector2(0, Time.deltaTime * shrinkSpeed);
                if (img.rectTransform.sizeDelta.y <= 0f)
                {
                    Destroy(img.gameObject);
                    _spawnedDots.RemoveAt(i);
                }
            }
        }
        if (heartIcon != null)
        {
            float scale = 1f;
            if (_heartPhase >= 0.22f && _heartPhase < 0.30f) scale = 1.3f + (intensity * 0.2f);
            else if (_heartPhase > 0.45f && _heartPhase < 0.55f) scale = 1.1f;
            heartIcon.localScale = Vector3.Lerp(heartIcon.localScale, new Vector3(scale, scale, 1f), Time.deltaTime * 15f);
            Image img = heartIcon.GetComponent<Image>();
            if (img != null) img.color = Color.Lerp(Color.white, Color.red, intensity);
        }
    }
    private void SetXRayActive(bool active)
    {
        if (_isXRayActive == active) return; 
        _isXRayActive = active;
        foreach (var kvp in _originalMaterials)
        {
            Renderer r = kvp.Key;
            if (r == null) continue;
            if (active && xrayMaterial != null)
            {
                Material[] currentMats = kvp.Value; 
                Material[] newMats = new Material[currentMats.Length + 1];
                for (int i = 0; i < currentMats.Length; i++)
                    newMats[i] = currentMats[i];
                newMats[currentMats.Length] = xrayMaterial; 
                r.materials = newMats; 
            }
            else
            {
                r.materials = kvp.Value;
            }
        }
    }
    private void PlayDetachedSound(AudioClip clip, Vector3 pos)
    {
        GameObject audioObj = new GameObject("DeathScreamAudio");
        audioObj.transform.position = pos;
        AudioSource aSource = audioObj.AddComponent<AudioSource>();
        aSource.spatialBlend = 1f; // 3D
        aSource.minDistance = 2f;
        aSource.maxDistance = 25f;
        aSource.rolloffMode = AudioRolloffMode.Linear;
        aSource.clip = clip;
        aSource.volume = 1f * GetVFXVolume();

        aSource.Play();
        Destroy(audioObj, clip.length + 0.1f); 
    }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new IShowSpeedGameplayInput();
        myInput.moveDirection = moveInput.action.ReadValue<Vector2>();
        myInput.isSprinting = sprintInput.action.IsPressed();
        myInput.isWalking = walkInput.action.IsPressed();

        if (mainCamera != null)
        {
            myInput.camForward = mainCamera.forward;
            myInput.camRight = mainCamera.right;
        }
        myInput.isVaulting = _vaultPressed;
        myInput.isSkill = _skillPressed;
        myInput.isInteract = interactInput.action.IsPressed();
        _vaultPressed = false;
        _skillPressed = false;
        input.Set(myInput);
    }
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player)
    {

    }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player)
    {

    }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player)
    {

    }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason)
    {

    }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason)
    {

    }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token)
    {

    }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason)
    {

    }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message)
    {

    }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data)
    {

    }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress)
    {

    }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input)
    {

    }
    public void OnConnectedToServer(NetworkRunner runner)
    {

    }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList)
    {

    }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data)
    {

    }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken)
    {

    }
    public void OnSceneLoadDone(NetworkRunner runner)
    {

    }
    public void OnSceneLoadStart(NetworkRunner runner)
    {

    }
    public NetworkObject Object => base.Object;
    public float GetRepairSpeedMultiplier() => 1f; 
    public void OnStartRepair()
    {
        _isLocalRepairing = true; 
    }
    public void OnStopRepair()
    {
        _isLocalRepairing = false; 
    }
    public bool GetIsDowned() => Object != null && Object.IsValid && IsDowned;
    public bool GetIsHooked() => Object != null && Object.IsValid && IsHooked;
    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetReviveState(NetworkBool start, NetworkId targetId, NetworkBool isUnhookingAction)
    {
        if (isUnhookingAction) { IsUnhooking = start; IsReviving = false; }
        else { IsReviving = start; IsUnhooking = false; }
        TargetReviveId = start ? targetId : default;
        if (targetId.IsValid)
        {
            var targetObj = Runner.FindObject(targetId);
            if (targetObj != null && targetObj.TryGetComponent(out ISurvivor targetSurvivor))
            {
                targetSurvivor.SetBeingRescued(start, isUnhookingAction, 1f); // Cố định 1x Speed
            }
        }
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetBeingRescued(NetworkBool isStarting, NetworkBool isUnhooking, float speed)
    {
        if (isUnhooking)
        {
            IsBeingUnhooked = isStarting;
            BonusRescueSpeed = isStarting ? speed : 0f;
        }
        else
        {
            if (isStarting) { ReviverCount++; BonusRescueSpeed += (speed - 1f); }
            else { ReviverCount--; BonusRescueSpeed -= (speed - 1f); }
            if (ReviverCount < 0) ReviverCount = 0;
            if (BonusRescueSpeed < 0) BonusRescueSpeed = 0f;
            IsBeingRevived = (ReviverCount > 0);
        }
    }
    public void SetBeingRescued(bool isStarting, bool isUnhooking, float rescuerSpeed)
    {
        RPC_SetBeingRescued(isStarting, isUnhooking, rescuerSpeed);
    }
    public bool GetIsBeingUnhooked() => Object != null && Object.IsValid && IsBeingUnhooked;
    public float GetRescueProgressRatio()
    {
        if (Object == null || !Object.IsValid) return 0f;
        if (IsHooked) return UnhookProgress / unhookTime;
        if (IsDowned) return ReviveProgress / reviveTime;
        return 0f;
    }
    private void HandleReviveLogic(IShowSpeedGameplayInput input)
    {
        if (Object.HasStateAuthority)
        {
            if ((IsReviving || IsUnhooking) && TargetReviveId.IsValid)
            {
                var targetObj = Runner.FindObject(TargetReviveId);
                if (targetObj == null || (targetObj.TryGetComponent(out ISurvivor tSurv) && !tSurv.GetIsDowned() && !tSurv.GetIsHooked()))
                {
                    IsReviving = false; IsUnhooking = false; TargetReviveId = default;
                }
            }
            if (IsBeingUnhooked)
            {
                UnhookProgress += Runner.DeltaTime * BonusRescueSpeed;
                if (UnhookProgress >= unhookTime) CompleteRescueFromOther();
            }
            else if (ReviverCount > 0)
            {
                float totalSpeed = ReviverCount + BonusRescueSpeed;
                ReviveProgress += Runner.DeltaTime * totalSpeed;
                if (ReviveProgress >= reviveTime) CompleteRescueFromOther();
            }
            else { UnhookProgress = 0f; }
        }
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            bool isPressingE = input.isInteract;
            if (IsReviving || IsUnhooking)
            {
                _isStartRpcSent = false;
                if (!isPressingE && !_isCancelRpcSent)
                {
                    NetworkId targetId = _targetToRevive != null && _targetToRevive.Object != null ? _targetToRevive.Object.Id : TargetReviveId;
                    RPC_SetReviveState(false, targetId, IsUnhooking);
                    _targetToRevive = null;
                    _isCancelRpcSent = true;
                }
            }
            else
            {
                _isCancelRpcSent = false;
                if (isPressingE && !_isStartRpcSent && !IsDowned && !IsHooked)
                {
                    Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
                    foreach (var hit in hits)
                    {
                        if (hit.CompareTag("CuuPlayer"))
                        {
                            var target = hit.GetComponentInParent<ISurvivor>();
                            if (target != null && (target.GetIsDowned() || target.GetIsHooked()) && target.Object.Id != this.Object.Id)
                            {
                                bool isTargetHooked = target.GetIsHooked();
                                if (isTargetHooked && target.GetIsBeingUnhooked()) continue; 
                                _targetToRevive = target;
                                RPC_SetReviveState(true, target.Object.Id, isTargetHooked);
                                _isStartRpcSent = true;
                                break; 
                            }
                        }
                    }
                }
                if (!isPressingE) _isStartRpcSent = false;
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetHookAtPosition(Vector3 rescuePosition)
    {
        Collider[] hits = Physics.OverlapSphere(rescuePosition, 2f);
        foreach (var hit in hits)
        {
            if (hit.CompareTag("Untagged") || hit.CompareTag("Moc"))
            {
                if (hit.transform.Find("HookPoint") != null)
                {
                    hit.tag = "Moc"; 
                    break;
                }
            }
        }
    }
    public void CompleteRescueFromOther()
    {
        if (!Object.HasStateAuthority) return;
        RPC_ResetHookAtPosition(transform.position);
        Vector3 dropPos = transform.root.position - transform.root.forward * 1.5f;
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) netTransform.Teleport(dropPos, transform.rotation);
        IsDowned = false;
        IsHooked = false;
        IsBeingRevived = false;
        IsBeingUnhooked = false;
        ReviverCount = 0;
        BonusRescueSpeed = 0f;
        ReviveProgress = 0f;
        UnhookProgress = 0f;
        CurrentHits = 1;
        SacrificeTimer = TickTimer.None;
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 3f);
    }
    public void EscapeFromHunter()
    {
        if (!Object.HasStateAuthority) return;
        if (_characterController != null) _characterController.enabled = false;
        transform.SetParent(null);
        Vector3 dropPos = transform.position;
        Transform rootT = transform.root;
        if (rootT != null && rootT != transform)
        {
            dropPos = rootT.position + rootT.forward * 1.5f;
        }
        else
        {
            dropPos = transform.position + transform.forward * 1.5f;
        }
        dropPos.y += 0.5f; 
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(dropPos, transform.rotation);
        }
        IsDowned = false;
        IsHooked = false;
        IsBeingRevived = false;
        CurrentHits = 2;
        HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 20f);
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 3f);
        if (_characterController != null) _characterController.enabled = true;
    }
    public float GetSacrificeTimer() => SacrificeTimer.RemainingTime(Runner) ?? 0f;
}
public struct IShowSpeedGameplayInput : INetworkInput
{
    public Vector2 moveDirection;
    public Vector3 camForward;
    public Vector3 camRight;
    public NetworkBool isSprinting;
    public NetworkBool isWalking;
    public NetworkBool isVaulting;
    public NetworkBool isSkill;
    public NetworkBool isInteract;
}