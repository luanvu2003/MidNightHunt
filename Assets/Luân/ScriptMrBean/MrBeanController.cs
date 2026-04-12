using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using UnityEngine.UI;
using TMPro;
using System;
using System.Collections.Generic;
using Fusion.Sockets;

[RequireComponent(typeof(CharacterController))]
public class MrBeanController_Fusion : NetworkBehaviour, INetworkRunnerCallbacks, ISurvivor
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
    public InputActionReference interactInput;
    private bool _vaultPressed;
    private bool _skillPressed;

    [Header("Window Vaulting Settings")]
    public string vaultAnimationTrigger = "Vault";
    public float vaultDuration = 1.5f;
    public float vaultDistance = 2.5f;

    [Header("Fast Repair Skill Settings")]
    public float repairSpeedMultiplier = 2f;
    public float skillDuration = 10f;
    public float skillCooldown = 30f;

    [Networked] public NetworkBool IsSkillArmed { get; set; }
    [Networked] public NetworkBool IsSkillActive { get; set; }

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

    [Header("Camera Reference")]
    public Transform mainCamera;

    [Header("PLayer State")]
    [Networked] private TickTimer InvincibilityTimer { get; set; }
    public GameObject PlayerDeadthBox;

    [Header("Revive Settings")]
    public float reviveTime = 90f;
    public string revivingAnimBool = "IsReviving";
    public GameObject revivePrompt;
    public Slider reviveSlider;
    [Header("Tùy Chỉnh Lệch Móc")]
    public Vector3 hookOffset = Vector3.zero;

    [Networked] public NetworkBool IsBeingRevived { get; set; }
    [Networked] public NetworkBool IsReviving { get; set; }
    [Networked] public TickTimer ReviveTimer { get; set; }
    [Networked] public NetworkId TargetReviveId { get; set; }

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

        if (_characterController != null) _characterController.enabled = false;

        if (Object.HasInputAuthority)
        {
            if (mainCamera == null && Camera.main != null) mainCamera = Camera.main.transform;
            InitUI();

            if (moveInput) moveInput.action.Enable();
            if (sprintInput) sprintInput.action.Enable();
            if (walkInput) walkInput.action.Enable();
            if (vaultInput) vaultInput.action.Enable();
            if (skillInput) skillInput.action.Enable();
            if (interactInput) interactInput.action.Enable();

            Runner.AddCallbacks(this);
        }

        if (Object.HasStateAuthority || Object.HasInputAuthority) StartCoroutine(EnableCharacterControllerRoutine());
        if (Runner.IsServer) IsGameStarted = true;
    }

    private System.Collections.IEnumerator EnableCharacterControllerRoutine()
    {
        yield return null;
        if (_characterController != null) _characterController.enabled = true;
    }

    public override void Despawned(NetworkRunner runner, bool hasState) { if (Object.HasInputAuthority) Runner.RemoveCallbacks(this); }

    private void Update()
    {
        if (!Object.HasInputAuthority) return;
        if (vaultInput.action.triggered) _vaultPressed = true;
        if (skillInput.action.triggered) _skillPressed = true;
    }

    private void InitUI()
    {
        if (interactUI) interactUI.SetActive(false);
        if (durationSlider) { durationSlider.gameObject.SetActive(false); durationSlider.maxValue = skillDuration; }
        if (cooldownImage) cooldownImage.gameObject.SetActive(false);
        if (cooldownText) { cooldownText.text = ""; cooldownText.gameObject.SetActive(false); }
        if (hookSlider) { hookSlider.gameObject.SetActive(false); hookSlider.maxValue = 1f; } // Ép về tỷ lệ 0-1
        if (reviveSlider) { reviveSlider.gameObject.SetActive(false); reviveSlider.maxValue = 1f; }
    }

    public override void FixedUpdateNetwork()
    {
        if (!IsGameStarted) return;
        if (CurrentHits > 0 && HitDecayTimer.Expired(Runner)) CurrentHits = 0;
        if (IsHooked && SacrificeTimer.Expired(Runner)) { Runner.Despawn(Object); return; }
        if (IsDowned || IsHooked) return;
        if (IsVaulting) { HandleVaultingMovement(); return; }

        if (GetInput(out MrBeanGameplayInput input))
        {
            HandleSkillInput(input);
            HandleMovement(input);
            HandleWindowInput(input);
            HandleReviveInput(input);
        }

        if (IsSkillActive && SkillDurationTimer.Expired(Runner))
        {
            IsSkillActive = false;
            SkillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillCooldown);
        }

        HandleReviveLogic();
    }

    public override void Render()
    {
        animator.SetBool(downedAnimationBool, IsDowned);
        animator.SetBool(hookedAnimationBool, IsHooked);
        animator.SetBool(revivingAnimBool, IsReviving);
        animator.SetBool("SuaMay", IsRepairingAnim);

        float currentAnimSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, AnimSpeedValue, Time.deltaTime * 15f));

        if (PlayerDeadthBox != null)
        {
            // 🚨 Bật box cứu khi GỤC hoặc BỊ TREO MÓC
            PlayerDeadthBox.SetActive((IsDowned || IsHooked) && !IsBeingRevived);
        }

        if (Object.HasInputAuthority)
        {
            UpdateSkillUI();
            UpdateHookUI();
            bool canShowE = !IsDowned && IsNearDeadBody() && !IsReviving;
            if (revivePrompt) revivePrompt.SetActive(canShowE);
            UpdateReviveProgressUI();
        }
    }

    public void SetRepairAnimation(bool isRepairing) => IsRepairingAnim = isRepairing;

    private bool IsNearDeadBody()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hitColliders) if (hit.CompareTag("Playerchet")) return true;
        return false;
    }

    private void HandleMovement(MrBeanGameplayInput input)
    {
        _characterController.enabled = false;
        _characterController.enabled = true;
        Vector3 direction = CalculateDirection(input.moveDirection, input.camForward, input.camRight);
        if (direction.magnitude > 1f) direction.Normalize();

        float speed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;

        if (_isLocalRepairing || IsRepairingAnim || IsReviving || IsBeingRevived)
        {
            if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
            velocityY += -9.81f * Runner.DeltaTime;
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);
            AnimSpeedValue = 0f;
            return;
        }

        if (input.isWalking) { speed = slowWalkSpeed; targetAnimSpeed = 0.2f; }
        else if (input.isSprinting) { speed = sprintSpeed; targetAnimSpeed = 1f; }
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
        else _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);

        AnimSpeedValue = targetAnimSpeed;
    }

    private void HandleVaultingMovement()
    {
        if (VaultTimer.Expired(Runner))
        {
            IsVaulting = false;
            _characterController.enabled = true;
            return;
        }

        _characterController.enabled = false;
        float t = 1f - (VaultTimer.RemainingTime(Runner).Value / vaultDuration);
        t = Mathf.SmoothStep(0f, 1f, t);
        transform.position = Vector3.Lerp(VaultStartPos, VaultTargetPos, t);
    }

    private void HandleWindowInput(MrBeanGameplayInput input)
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

    private void HandleSkillInput(MrBeanGameplayInput input)
    {
        if (input.isSkill && SkillCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            if (!IsSkillArmed && !IsSkillActive) IsSkillArmed = true;
        }
    }

    private void HandleReviveInput(MrBeanGameplayInput input)
    {
        if (!Object.HasStateAuthority) return;

        if (input.isInteract)
        {
            if (!IsReviving)
            {
                Collider[] hits = Physics.OverlapSphere(transform.position, 2f);
                foreach (var hit in hits)
                {
                    if (hit.CompareTag("Playerchet"))
                    {
                        var obj = hit.GetComponentInParent<NetworkObject>();
                        if (obj != null && obj != this.Object)
                        {
                            IsReviving = true;
                            TargetReviveId = obj.Id;
                            CallStartReviveRPC(obj, reviveTime);
                            break;
                        }
                    }
                }
            }
        }
        else if (IsReviving)
        {
            IsReviving = false;
            if (TargetReviveId.IsValid)
            {
                var obj = Runner.FindObject(TargetReviveId);
                if (obj != null) CallStopReviveRPC(obj);
            }
            TargetReviveId = default;
        }
    }

    private void HandleReviveLogic()
    {
        if (!Object.HasStateAuthority) return;
        if (IsBeingRevived && ReviveTimer.Expired(Runner)) CompleteRevive();

        if (IsReviving && TargetReviveId.IsValid)
        {
            var target = Runner.FindObject(TargetReviveId);
            if (target != null)
            {
                bool stillNeedsRevive = false;

                var speed = target.GetComponent<IShowSpeedController_Fusion>();
                if (speed && (speed.IsDowned || speed.IsHooked)) stillNeedsRevive = true;

                var bean = target.GetComponent<MrBeanController_Fusion>();
                if (bean && (bean.IsDowned || bean.IsHooked)) stillNeedsRevive = true;

                var beast = target.GetComponent<MrBeastController_Fusion>();
                if (beast && (beast.IsDowned || beast.IsHooked)) stillNeedsRevive = true;

                var nurse = target.GetComponent<NurseController_Fusion>();
                if (nurse && (nurse.IsDowned || nurse.IsHooked)) stillNeedsRevive = true;

                // Nếu nạn nhân đã đứng dậy và không còn trên móc -> Ngắt cứu
                if (!stillNeedsRevive) IsReviving = false;
            }
            else IsReviving = false;
        }
    }

    private void CompleteRevive()
    {
        IsDowned = false;
        IsHooked = false; // 🚨 Gỡ nhân vật xuống khỏi móc
        IsBeingRevived = false;
        _characterController.enabled = true;
        PlayerDeadthBox.SetActive(false);
        CurrentHits = 1;
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_StartBeingRevived(float time) { IsBeingRevived = true; ReviveTimer = TickTimer.CreateFromSeconds(Runner, time); }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)] public void RPC_StopBeingRevived() { IsBeingRevived = false; ReviveTimer = TickTimer.None; }

    private void CallStartReviveRPC(NetworkObject obj, float time)
    {
        var speed = obj.GetComponent<IShowSpeedController_Fusion>(); if (speed) { speed.RPC_StartBeingRevived(time); return; }
        var bean = obj.GetComponent<MrBeanController_Fusion>(); if (bean) { bean.RPC_StartBeingRevived(time); return; }
        var beast = obj.GetComponent<MrBeastController_Fusion>(); if (beast) { beast.RPC_StartBeingRevived(time); return; }
        var nurse = obj.GetComponent<NurseController_Fusion>(); if (nurse) { nurse.RPC_StartBeingRevived(time); return; }
    }

    private void CallStopReviveRPC(NetworkObject obj)
    {
        var speed = obj.GetComponent<IShowSpeedController_Fusion>(); if (speed) { speed.RPC_StopBeingRevived(); return; }
        var bean = obj.GetComponent<MrBeanController_Fusion>(); if (bean) { bean.RPC_StopBeingRevived(); return; }
        var beast = obj.GetComponent<MrBeastController_Fusion>(); if (beast) { beast.RPC_StopBeingRevived(); return; }
        var nurse = obj.GetComponent<NurseController_Fusion>(); if (nurse) { nurse.RPC_StopBeingRevived(); return; }
    }

    public NetworkObject Object => base.Object;

    public float GetRepairSpeedMultiplier() => IsSkillActive ? repairSpeedMultiplier : 1f;

    public void OnStartRepair()
    {
        _isLocalRepairing = true;
        if (IsSkillArmed)
        {
            IsSkillArmed = false;
            IsSkillActive = true;
            SkillDurationTimer = TickTimer.CreateFromSeconds(Runner, skillDuration);
        }
    }

    public void OnStopRepair()
    {
        _isLocalRepairing = false;
        if (IsSkillActive)
        {
            IsSkillActive = false;
            SkillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillCooldown);
            SkillDurationTimer = TickTimer.None;
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

        if (CurrentHits == 1) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 10f);
        else if (CurrentHits == 2) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 20f);
        else if (CurrentHits >= 3) { IsDowned = true; _characterController.enabled = false; }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)] private void RPC_PlayHitAnim(NetworkBool isMoving) { animator.SetBool("IsMoving", isMoving); animator.SetTrigger(hitAnimationTrigger); }

    public void GetHooked(Vector3 hookPos, Quaternion hookRot)
    {
        if (IsHooked || !Object.HasStateAuthority) return;
        IsDowned = false; IsHooked = true;
        PlayerHookReceiver hookReceiver = GetComponent<PlayerHookReceiver>();
        if (hookReceiver != null) hookReceiver.ReleaseFromHunter();
        if (_characterController != null) _characterController.enabled = false;
        Vector3 adjustedPos = hookPos + (hookRot * hookOffset);
        transform.position = adjustedPos; transform.rotation = hookRot;
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null) networkTransform.Teleport(adjustedPos, hookRot);
        SacrificeTimer = TickTimer.CreateFromSeconds(Runner, sacrificeTime);
    }

    [Rpc(RpcSources.All, RpcTargets.All)] private void RPC_PlayVaultAnim() => animator.SetTrigger(vaultAnimationTrigger);

    private void UpdateSkillUI()
    {
        bool isArmed = IsSkillArmed;
        bool isActive = IsSkillActive;
        durationSlider.gameObject.SetActive(isArmed || isActive);
        if (isArmed) durationSlider.value = skillDuration;
        else if (isActive) durationSlider.value = SkillDurationTimer.RemainingTime(Runner).Value;

        float? cdLeft = SkillCooldownTimer.RemainingTime(Runner);
        bool onCooldown = cdLeft > 0 && !isArmed && !isActive;

        cooldownImage.gameObject.SetActive(cdLeft > 0 && !isActive && !isArmed);
        if (cdLeft > 0) cooldownImage.fillAmount = cdLeft.Value / skillCooldown;

        cooldownText.gameObject.SetActive(onCooldown);
        if (onCooldown) cooldownText.text = Mathf.Ceil(cdLeft.Value).ToString();
    }

    private void UpdateHookUI()
    {
        hookSlider.gameObject.SetActive(IsHooked);
        if (IsHooked)
        {
            float timeRemaining = SacrificeTimer.RemainingTime(Runner) ?? 0f;
            hookSlider.value = timeRemaining / sacrificeTime; // Chạy từ 1 về 0 cực mượt
        }
    }

    private void UpdateReviveProgressUI()
    {
        if (reviveSlider == null) return;
        bool showingSlider = IsReviving || IsBeingRevived;
        reviveSlider.gameObject.SetActive(showingSlider);
        if (showingSlider)
        {
            float? remainingTime = 0;
            if (IsBeingRevived) remainingTime = ReviveTimer.RemainingTime(Runner);
            else if (IsReviving && TargetReviveId.IsValid)
            {
                var obj = Runner.FindObject(TargetReviveId);
                if (obj != null)
                {
                    var speed = obj.GetComponent<IShowSpeedController_Fusion>(); if (speed) remainingTime = speed.ReviveTimer.RemainingTime(Runner);
                    var bean = obj.GetComponent<MrBeanController_Fusion>(); if (bean) remainingTime = bean.ReviveTimer.RemainingTime(Runner);
                    var beast = obj.GetComponent<MrBeastController_Fusion>(); if (beast) remainingTime = beast.ReviveTimer.RemainingTime(Runner);
                    var nurse = obj.GetComponent<NurseController_Fusion>(); if (nurse) remainingTime = nurse.ReviveTimer.RemainingTime(Runner);
                }
            }
            if (remainingTime.HasValue) reviveSlider.value = 1f - (remainingTime.Value / reviveTime);
        }
    }

    private Vector3 CalculateDirection(Vector2 inputDir, Vector3 camFwd, Vector3 camRight)
    {
        if (camFwd == Vector3.zero && camRight == Vector3.zero) return new Vector3(inputDir.x, 0, inputDir.y).normalized;
        Vector3 forward = Vector3.Scale(camFwd, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Vector3.Scale(camRight, new Vector3(1, 0, 1)).normalized;
        return (forward * inputDir.y + right * inputDir.x);
    }

    private void OnTriggerEnter(Collider other) { if (!Object.HasStateAuthority) return; if (other.CompareTag("Cuaso")) _isNearWindow = true; else if (other.CompareTag("HuntetHit")) TakeHit(); }
    private void OnTriggerExit(Collider other) { if (other.CompareTag("Cuaso")) _isNearWindow = false; }

    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new MrBeanGameplayInput();
        myInput.moveDirection = moveInput.action.ReadValue<Vector2>();
        myInput.isSprinting = sprintInput.action.IsPressed();
        myInput.isWalking = walkInput.action.IsPressed();
        if (mainCamera != null) { myInput.camForward = mainCamera.forward; myInput.camRight = mainCamera.right; }
        myInput.isVaulting = _vaultPressed; myInput.isSkill = _skillPressed; myInput.isInteract = interactInput.action.IsPressed();
        _vaultPressed = false; _skillPressed = false;
        input.Set(myInput);
    }

    // --- CÁC HÀM RUNNER RỖNG ... ---
    public void OnObjectExitAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnObjectEnterAOI(NetworkRunner runner, NetworkObject obj, PlayerRef player) { }
    public void OnPlayerJoined(NetworkRunner runner, PlayerRef player) { }
    public void OnPlayerLeft(NetworkRunner runner, PlayerRef player) { }
    public void OnShutdown(NetworkRunner runner, ShutdownReason shutdownReason) { }
    public void OnDisconnectedFromServer(NetworkRunner runner, NetDisconnectReason reason) { }
    public void OnConnectRequest(NetworkRunner runner, NetworkRunnerCallbackArgs.ConnectRequest request, byte[] token) { }
    public void OnConnectFailed(NetworkRunner runner, NetAddress remoteAddress, NetConnectFailedReason reason) { }
    public void OnUserSimulationMessage(NetworkRunner runner, SimulationMessagePtr message) { }
    public void OnReliableDataReceived(NetworkRunner runner, PlayerRef player, ReliableKey key, ArraySegment<byte> data) { }
    public void OnReliableDataProgress(NetworkRunner runner, PlayerRef player, ReliableKey key, float progress) { }
    public void OnInputMissing(NetworkRunner runner, PlayerRef player, NetworkInput input) { }
    public void OnConnectedToServer(NetworkRunner runner) { }
    public void OnSessionListUpdated(NetworkRunner runner, List<SessionInfo> sessionList) { }
    public void OnCustomAuthenticationResponse(NetworkRunner runner, Dictionary<string, object> data) { }
    public void OnHostMigration(NetworkRunner runner, HostMigrationToken hostMigrationToken) { }
    public void OnSceneLoadDone(NetworkRunner runner) { }
    public void OnSceneLoadStart(NetworkRunner runner) { }
}

public struct MrBeanGameplayInput : INetworkInput
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