using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; // Thư viện Fusion
using UnityEngine.UI;
using TMPro;
using System; // 🚨 Đã thêm
using System.Collections.Generic;
using Fusion.Sockets; // 🚨 Đã thêm

[RequireComponent(typeof(CharacterController))]
// 🚨 Đã thêm INetworkRunnerCallbacks vào đây
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

    [Header("Camera Reference")]
    public Transform mainCamera;

    [Header("PLayer State")]
    [Networked] private TickTimer InvincibilityTimer { get; set; }
    public GameObject PlayerDeadthBox;

    [Header("Revive & Unhook Settings")]
    public float reviveTime = 5f;  // Cứu gục dưới đất mất 5 giây
    public float unhookTime = 2f;  // Tháo móc mất 2 giây
    public string revivingAnimBool = "IsReviving";
    public string unhookingAnimBool = "IsUnhooking"; // 🚨 THÊM ANIMATION THÁO MÓC

    private ISurvivor _targetToRevive; // 🚨 Đã đổi thành ISurvivor để cứu được mọi người
    public InputActionReference interactInput;
    public GameObject revivePrompt;
    public Slider reviveSlider;

    [Header("Tùy Chỉnh Lệch Móc")]
    public Vector3 hookOffset = Vector3.zero;

    [Networked] public NetworkBool IsBeingRevived { get; set; }
    [Networked] public NetworkBool IsReviving { get; set; }
    [Networked] public NetworkBool IsUnhooking { get; set; } // 🚨 Đang đứng tháo móc
    [Networked] private TickTimer ReviveTimer { get; set; }

    private CharacterController _characterController;
    private bool _isNearWindow = false;
    private bool _isLocalRepairing = false;

    // --- CÁC BIẾN ĐỒNG BỘ MẠNG (NETWORKED) ---
    [Networked] public NetworkBool IsDowned { get; set; }
    [Networked] public NetworkBool IsHooked { get; set; }
    [Networked] public int CurrentHits { get; set; }
    [Networked] public NetworkBool IsVaulting { get; set; }

    // --- BỘ ĐẾM THỜI GIAN MẠNG (TICKTIMER) ---
    [Networked] private TickTimer HitDecayTimer { get; set; }
    [Networked] private TickTimer SkillDurationTimer { get; set; }
    [Networked] private TickTimer SkillCooldownTimer { get; set; }
    [Networked] private TickTimer VaultTimer { get; set; }
    [Networked] private TickTimer SacrificeTimer { get; set; }

    // Lưu vị trí nhảy để đồng bộ mượt mà
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

        // TẮT NGAY LẬP TỨC ĐỂ TRÁNH XUNG ĐỘT
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

        // 🚨 QUAN TRỌNG NHẤT LÀ ĐOẠN NÀY:
        // Chỉ bật lại CharacterController nếu bạn là Host HOẶC bạn là chủ của nhân vật này.
        // Còn nếu bạn đang nhìn nhân vật của người khác (Proxy), thì CC PHẢI BỊ TẮT!
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            StartCoroutine(EnableCharacterControllerRoutine());
        }

        if (Runner.IsServer) IsGameStarted = true;
    }

    private System.Collections.IEnumerator EnableCharacterControllerRoutine()
    {
        yield return null; // Đợi 1 frame cho vị trí mạng đồng bộ xong
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

        // 🚨 CHUYỂN HÀM CHECK CỨU LÊN ĐÂY!
        // Phải check cứu trước khi code bị ngắt bởi trạng thái gục/treo
        HandleReviveLogic();

        if (CurrentHits > 0 && HitDecayTimer.Expired(Runner)) CurrentHits = 0;
        if (IsHooked && SacrificeTimer.Expired(Runner)) { Runner.Despawn(Object); return; }

        // Code cũ của bạn sẽ ngắt ở đây nếu đang nằm gục
        if (IsDowned || IsHooked) return;

        if (IsVaulting) { HandleVaultingMovement(); return; }

        if (GetInput(out IShowSpeedGameplayInput input)) // (Đổi tên input tùy nhân vật)
        {
            HandleSkillInput(input);
            HandleMovement(input);
            HandleWindowInput(input);
        }
    }

    public override void Render()
    {
        animator.SetBool(downedAnimationBool, IsDowned);
        animator.SetBool(hookedAnimationBool, IsHooked);
        animator.SetBool(revivingAnimBool, IsReviving);     // Anim cứu dưới đất
        animator.SetBool(unhookingAnimBool, IsUnhooking);   // Anim tháo móc

        animator.SetBool("SuaMay", IsRepairingAnim);

        float currentAnimSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, AnimSpeedValue, Time.deltaTime * 15f));

        // 🚨 FIX LỖI TỰ HỦY BOX: Phải luôn hiện Box khi gục HOẶC treo. 
        // TUYỆT ĐỐI KHÔNG TẮT khi đang bị cứu, nếu tắt Trigger sẽ miss và ngắt cứu ngay lập tức!
        if (PlayerDeadthBox != null)
        {
            PlayerDeadthBox.SetActive(IsDowned || IsHooked);
        }

        if (Object.HasInputAuthority)
        {
            UpdateSkillUI();
            UpdateHookUI();

            // Chỉ hiện chữ E khi có xác/người bị treo và bản thân chưa bấm E
            bool canShowE = !IsDowned && !IsHooked && IsNearDeadBody() && !IsReviving && !IsUnhooking;
            if (revivePrompt) revivePrompt.SetActive(canShowE);

            UpdateReviveProgressUI();
        }
    }

    public void SetRepairAnimation(bool isRepairing)
    {
        IsRepairingAnim = isRepairing;
    }

    private bool IsNearDeadBody()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Playerchet")) return true;
        }
        return false;
    }

    private void HandleMovement(IShowSpeedGameplayInput input)
    {
        _characterController.enabled = false;
        _characterController.enabled = true;
        Vector3 direction = CalculateDirection(input.moveDirection, input.camForward, input.camRight);

        // 🚨 CHUẨN HÓA: Chống lỗi đi chéo bị nhân đôi tốc độ (X2 Speed)
        if (direction.magnitude > 1f) direction.Normalize();

        float speed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;
        bool skillActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);
        if (_isLocalRepairing || IsRepairingAnim || IsReviving || IsUnhooking)
        {
            // Vẫn phải giữ trọng lực để nhân vật không rớt xuyên map
            if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
            velocityY += -9.81f * Runner.DeltaTime;
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);

            AnimSpeedValue = 0f; // Ép tắt animation chạy
            return; // 🚨 KẾT THÚC HÀM TẠI ĐÂY, VÔ HIỆU HÓA WASD
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

        // 🚨 THÊM TRỌNG LỰC: Ép nhân vật dính sát đất để Host và Client tính toán chính xác 100%
        if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += -9.81f * Runner.DeltaTime;

        if (direction.magnitude >= 0.1f)
        {
            Vector3 moveVelocity = direction * speed;
            moveVelocity.y = velocityY; // Gắn trọng lực vào

            _characterController.Move(moveVelocity * Runner.DeltaTime);
            transform.rotation = Quaternion.LookRotation(direction);
        }
        else
        {
            // Vẫn phải rớt xuống đất kể cả khi đứng im
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);
        }

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

        // 🚨 FIX LỖI SPAM HIT: Kiểm tra xem đã hết thời gian bất tử chưa
        if (!InvincibilityTimer.ExpiredOrNotRunning(Runner)) return;

        CurrentHits++;

        // Bật trạng thái bất tử trong 1.5 giây sau khi ăn hit (tránh bị 1 chém dính 3 hit)
        // Bạn có thể chỉnh 1.5f thành số khác tùy tốc độ vung rìu của Hunter
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 1.5f);

        bool isMoving = _characterController.velocity.magnitude > 0.1f;
        RPC_PlayHitAnim(isMoving);

        if (CurrentHits == 1) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 10f);
        else if (CurrentHits == 2) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 20f);
        else if (CurrentHits >= 3)
        {
            IsDowned = true;
            _characterController.enabled = false;
            // 🚨 ĐÃ XÓA DÒNG BẬT BOX Ở ĐÂY VÌ ĐỂ ĐÂY CLIENT SẼ KHÔNG THẤY
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim(NetworkBool isMoving)
    {
        animator.SetBool("IsMoving", isMoving);
        animator.SetTrigger(hitAnimationTrigger);
    }

    // 🚨 Thêm Quaternion hookRot vào trong ngoặc
    // 🚨 Thêm Quaternion hookRot vào trong ngoặc
    public void GetHooked(Vector3 hookPos, Quaternion hookRot)
    {
        if (IsHooked || !Object.HasStateAuthority) return;

        IsDowned = false;
        IsHooked = true;

        PlayerHookReceiver hookReceiver = GetComponent<PlayerHookReceiver>();
        if (hookReceiver != null) hookReceiver.ReleaseFromHunter();

        // 🚨 1. Bắt buộc tắt Character Controller trước khi dời đi
        if (_characterController != null) _characterController.enabled = false;

        // 🚨 2. TÍNH TOÁN VỊ TRÍ MỚI (Cộng thêm offset theo hướng của cái móc)
        Vector3 adjustedPos = hookPos + (hookRot * hookOffset);

        transform.position = adjustedPos;
        transform.rotation = hookRot;

        // 🚨 3. Báo cho Fusion biết nhân vật đã dịch chuyển
        var networkTransform = GetComponent<NetworkTransform>();
        if (networkTransform != null)
        {
            networkTransform.Teleport(adjustedPos, hookRot);
        }

        SacrificeTimer = TickTimer.CreateFromSeconds(Runner, sacrificeTime);
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayVaultAnim() => animator.SetTrigger(vaultAnimationTrigger);

    private void UpdateSkillUI()
    {
        bool durationActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);
        durationSlider.gameObject.SetActive(durationActive);
        if (durationActive) durationSlider.value = SkillDurationTimer.RemainingTime(Runner).Value;

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
        if (IsHooked) hookSlider.value = SacrificeTimer.RemainingTime(Runner) ?? 0;
    }

    private Vector3 CalculateDirection(Vector2 inputDir, Vector3 camFwd, Vector3 camRight)
    {
        // Nếu không có camera, đi theo trục thế giới mặc định để không bị kẹt
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
        else if (Object.HasInputAuthority && other.CompareTag("Playerchet"))
        {
            // Đi ra khỏi vùng Trigger -> Tự động hủy cứu
            if (IsReviving || IsUnhooking) RPC_SetReviveState(false, default, false);
            _targetToRevive = null;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        // Nếu chạm vào Trigger của người bị nạn
        if (!IsDowned && !IsHooked && other.CompareTag("Playerchet"))
        {
            // 🚨 Bất kể là ai, cứ là ISurvivor thì lấy
            var target = other.GetComponentInParent<ISurvivor>();

            // Nếu đúng là đang gục hoặc treo, VÀ KHÔNG PHẢI CHÍNH MÌNH (tránh tự cứu mình)
            if (target != null && (target.GetIsDowned() || target.GetIsHooked()) && target.Object.Id != this.Object.Id)
            {
                _targetToRevive = target;
                bool isTargetHooked = target.GetIsHooked();

                if (interactInput.action.IsPressed())
                {
                    if (!IsReviving && !IsUnhooking)
                    {
                        // Gửi cờ lên Server báo: Tôi đang cứu thằng này, nó bị móc hay gục?
                        RPC_SetReviveState(true, target.Object.Id, isTargetHooked);
                    }
                }
                else
                {
                    if (IsReviving || IsUnhooking)
                    {
                        // Nhả E ra -> Hủy cứu
                        RPC_SetReviveState(false, target.Object.Id, isTargetHooked);
                    }
                }
            }
        }
    }

    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetReviveState(NetworkBool start, NetworkId targetId, NetworkBool isUnhookingAction)
    {
        // 1. Gán Anim cho Người Cứu
        if (isUnhookingAction)
        {
            IsUnhooking = start;
            IsReviving = false;
        }
        else
        {
            IsReviving = start;
            IsUnhooking = false;
        }

        // 2. Gán Thời gian và Trạng thái cho Nạn Nhân
        if (targetId.IsValid)
        {
            var targetObj = Runner.FindObject(targetId);
            if (targetObj != null)
            {
                var targetSurvivor = targetObj.GetComponent<ISurvivor>();
                if (targetSurvivor != null)
                {
                    float requiredTime = isUnhookingAction ? unhookTime : reviveTime;
                    targetSurvivor.SetBeingRescued(start, requiredTime);
                }
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    public void RPC_SetBeingRescued(NetworkBool isStarting, float requiredTime)
    {
        IsBeingRevived = isStarting;
        if (isStarting)
            ReviveTimer = TickTimer.CreateFromSeconds(Runner, requiredTime);
        else
            ReviveTimer = TickTimer.None;
    }

    private void HandleReviveLogic()
    {
        // 1. DÀNH CHO NẠN NHÂN (Người đang bị nằm gục/treo)
        if (Object.HasStateAuthority)
        {
            // Nếu tôi đang bị cứu và thanh thời gian trên máy tôi đã chạy xong
            if (IsBeingRevived && ReviveTimer.Expired(Runner))
            {
                CompleteRescueFromOther();
            }
        }

        // 2. DÀNH CHO NGƯỜI ĐI CỨU
        if (Object.HasInputAuthority)
        {
            // Nếu mình đang bấm E cứu/tháo móc
            if ((IsReviving || IsUnhooking) && _targetToRevive != null)
            {
                // Tự động kiểm tra: Nếu nạn nhân đã đứng dậy thành công
                if (!_targetToRevive.GetIsDowned() && !_targetToRevive.GetIsHooked())
                {
                    // Tự động gọi RPC để hủy trạng thái Anim của bản thân
                    RPC_SetReviveState(false, default, false);
                    _targetToRevive = null;
                }
            }
        }
    }

    private void CompleteRevive()
    {
        IsDowned = false;
        IsBeingRevived = false;
        _characterController.enabled = true;
        PlayerDeadthBox.SetActive(false);
        CurrentHits = 1;
    }

    private void UpdateReviveProgressUI()
    {
        if (reviveSlider == null) return;

        bool showingSlider = IsReviving || IsUnhooking || IsBeingRevived;
        reviveSlider.gameObject.SetActive(showingSlider);

        if (showingSlider)
        {
            float? remainingTime = 0;
            float totalTime = IsUnhooking ? unhookTime : reviveTime;

            if (IsBeingRevived)
                remainingTime = ReviveTimer.RemainingTime(Runner);
            else if ((IsReviving || IsUnhooking) && _targetToRevive != null)
                remainingTime = _targetToRevive.GetRescueTimer().RemainingTime(Runner);

            if (remainingTime.HasValue)
            {
                // Công thức tính Slider an toàn tuyệt đối
                float progress = 1f - (remainingTime.Value / Mathf.Max(0.1f, totalTime));
                reviveSlider.value = progress;
            }
        }
    }

    // --- INetworkRunnerCallbacks ---
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new IShowSpeedGameplayInput();

        myInput.moveDirection = moveInput.action.ReadValue<Vector2>();
        myInput.isSprinting = sprintInput.action.IsPressed();
        myInput.isWalking = walkInput.action.IsPressed();

        // 🚨 ĐỌC VÀ GỬI HƯỚNG CAMERA LÊN SERVER
        if (mainCamera != null)
        {
            myInput.camForward = mainCamera.forward;
            myInput.camRight = mainCamera.right;
        }

        myInput.isVaulting = _vaultPressed;
        myInput.isSkill = _skillPressed;
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
    public float GetRepairSpeedMultiplier() => 1f; // Tốc độ cơ bản 1x
    public void OnStartRepair()
    {
        _isLocalRepairing = true; // Bấm E là khóa chân ngay
    }
    public void OnStopRepair()
    {
        _isLocalRepairing = false; // Thả E hoặc nổ máy là mở khóa
    }

    public bool GetIsDowned() => IsDowned;
    public bool GetIsHooked() => IsHooked;
    public TickTimer GetRescueTimer() => ReviveTimer;

    public void SetBeingRescued(bool isStarting, float requiredTime)
    {
        // Yêu cầu nạn nhân tự bật State và đếm giờ của chính họ
        RPC_SetBeingRescued(isStarting, requiredTime);
    }

    public void CompleteRescueFromOther()
    {
        IsDowned = false;
        IsHooked = false;
        IsBeingRevived = false;
        _characterController.enabled = true;

        CurrentHits = 1; // Hồi lại 1 máu khi được cứu
        SacrificeTimer = TickTimer.None; // Xóa án tử hình trên móc
    }
}



public struct IShowSpeedGameplayInput : INetworkInput
{
    public Vector2 moveDirection;
    // 🚨 THÊM 2 BIẾN NÀY ĐỂ GỬI HƯỚNG CAMERA LÊN SERVER
    public Vector3 camForward;
    public Vector3 camRight;

    public NetworkBool isSprinting;
    public NetworkBool isWalking;
    public NetworkBool isVaulting;
    public NetworkBool isSkill;
}