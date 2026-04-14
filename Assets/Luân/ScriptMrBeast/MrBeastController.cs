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
public class MrBeastController_Fusion : NetworkBehaviour, INetworkRunnerCallbacks, ISurvivor
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

    [Header("Wallhack Skill Settings")]
    public float scanDistance = 15f;
    [Range(0, 360)] public float scanAngle = 90f;
    public float skillDuration = 5f;
    public float skillCooldown = 30f;
    public LayerMask hunterLayer;
    public Material xrayMaterial;


    // 🚨 QUẢN LÝ THEO DANH SÁCH: Lưu trữ con Hunter (Key) và cái Bóng đỏ đang gắn trên nó (Value)
    private Dictionary<GameObject, GameObject> _activeSilhouettes = new Dictionary<GameObject, GameObject>();
    private List<GameObject> _scannedHunters = new List<GameObject>();



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
    private bool _isCancelRpcSent = false; // Cờ khóa chống spam mạng
    private bool _isStartRpcSent = false;
    private bool _isInteractHolding = false;

    private ISurvivor _targetToRevive; // 🚨 Đã đổi thành ISurvivor để cứu được mọi người
    public InputActionReference interactInput;
    public GameObject revivePrompt;
    public Slider reviveSlider;

    [Header("Tùy Chỉnh Lệch Móc")]
    public Vector3 hookOffset = Vector3.zero;

    [Networked] public NetworkBool IsBeingRevived { get; set; }
    [Networked] public NetworkBool IsBeingUnhooked { get; set; } // 🚨 Mới: Cờ khóa báo hiệu đang có người tháo móc
    [Networked] public int ReviverCount { get; set; }
    [Networked] public NetworkId TargetReviveId { get; set; }
    [Networked] public float BonusRescueSpeed { get; set; }
    [Networked] public NetworkBool IsReviving { get; set; }
    [Networked] public NetworkBool IsUnhooking { get; set; } // 🚨 Đang đứng tháo móc
    [Networked] public float ReviveProgress { get; set; } // Tiến trình cứu gục (có lưu)
    [Networked] public float UnhookProgress { get; set; } // Tiến trình tháo móc (không lưu)
    [Header("== SOUND EFFECTS (VFX) ==")]
    public AudioSource playerAudioSource; // Nguồn phát 3D cho Bước chân, Nhảy, Bị đánh
    public AudioSource heartbeatSource;   // Nguồn phát 2D (Tim đập trong đầu, chỉ Local nghe)

    public AudioClip[] footstepSounds; // Danh sách tiếng bước chân (để random cho chân thật)
    public AudioClip hitSound;         // Tiếng hoảng sợ khi bị chém
    public AudioClip vaultSound;       // Tiếng trèo cửa sổ
    public AudioClip deathSound;       // Tiếng hét thất thanh khi bay màu

    [Header("== HEARTBEAT SETTINGS ==")]
    public float terrorRadius = 30f; // Khoảng cách bắt đầu nghe tiếng tim
    public float maxHeartbeatPitch = 1.8f; // Nhịp tim đập nhanh tối đa khi quái ở sát bên

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

        // 🚨 1. LẤY INPUT NGAY TỪ ĐẦU
        if (GetInput(out MrBeastGameplayInput input))
        {
            // 🚨 2. TRUYỀN INPUT VÀO HÀM CỨU ĐỂ KIỂM TRA PHÍM "E"
            HandleReviveLogic(input);

            if (CurrentHits > 0 && HitDecayTimer.Expired(Runner)) CurrentHits = 0;
            if (IsHooked && SacrificeTimer.Expired(Runner))
            {
                if (deathSound != null) PlayDetachedSound(deathSound, transform.position);
                Runner.Despawn(Object);
                return;
            }

            // 🚨 3. NẾU ĐANG GỤC HOẶC TREO: Dừng code ở đây (Cấm chạy bộ, trèo cửa sổ)
            if (IsDowned || IsHooked) return;

            // 🚨 4. CÁC HÀNH ĐỘNG BÌNH THƯỜNG
            if (IsVaulting) { HandleVaultingMovement(); return; }

            HandleSkillInput(input);
            HandleMovement(input);
            HandleWindowInput(input);
        }
        else
        {
            // 🚨 BẢO VỆ MẠNG: Nếu mất kết nối/đứt packet, truyền default (nhả phím E)
            HandleReviveLogic(default);
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

        if (!Object.HasInputAuthority && !Object.HasStateAuthority)
        {
            if (_characterController != null && _characterController.enabled)
            {
                // Bắt buộc tắt Character Controller trên màn hình của người khác.
                // Nếu không tắt, Physics của Unity sẽ chặn luồng dữ liệu mạng làm nhân vật đứng im.
                _characterController.enabled = false;
            }
        }

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
            HandleWallhackVisuals();
            UpdateHeartbeat();
        }
        if (!IsDowned && !IsHooked && transform.parent != null)
        {
            transform.SetParent(null);
        }
    }

    private void HandleWallhackVisuals()
    {
        bool skillActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);

        if (!skillActive)
        {
            ClearAllHighlights();
            return;
        }

        // Quét 360 độ bằng Layer
        Collider[] hits = Physics.OverlapSphere(transform.position, scanDistance, hunterLayer);
        List<GameObject> currentlyVisibleHunters = new List<GameObject>();

        foreach (var hit in hits)
        {
            GameObject hunterObj = hit.gameObject;
            currentlyVisibleHunters.Add(hunterObj);

            // Nếu con Hunter này MỚI bước vào vùng quét
            if (!_scannedHunters.Contains(hunterObj))
            {
                _scannedHunters.Add(hunterObj);
                SetXRayOnHunter(hunterObj, true); // Khoác áo X-Ray lên
            }
        }

        // Nếu Hunter chạy ra khỏi vùng quét -> Lột áo X-Ray ra
        for (int i = _scannedHunters.Count - 1; i >= 0; i--)
        {
            GameObject hunter = _scannedHunters[i];
            if (hunter == null || !currentlyVisibleHunters.Contains(hunter))
            {
                SetXRayOnHunter(hunter, false);
                _scannedHunters.RemoveAt(i);
            }
        }
    }

    // Hàm ma thuật: Tự động len lỏi vào từng bộ phận của quái để đắp Material
    private void SetXRayOnHunter(GameObject hunter, bool isOn)
    {
        if (hunter == null || xrayMaterial == null) return;

        // Lấy TOÀN BỘ các bộ phận hiển thị (cả tĩnh lẫn động) trên người con quái
        Renderer[] renderers = hunter.GetComponentsInChildren<Renderer>();

        foreach (Renderer r in renderers)
        {
            // Lấy danh sách Material hiện tại của bộ phận này
            List<Material> matList = new List<Material>(r.materials);

            if (isOn)
            {
                // Kiểm tra xem đã có X-Ray chưa để tránh đắp chồng lên nhau nhiều lần
                bool hasXray = false;
                foreach (var m in matList)
                {
                    // Tên material khi chạy sẽ tự thêm chữ (Instance) nên dùng Contains
                    if (m.name.Contains(xrayMaterial.name)) hasXray = true;
                }

                // Nếu chưa có thì thêm nó vào cuối danh sách
                if (!hasXray)
                {
                    matList.Add(xrayMaterial);
                    r.materials = matList.ToArray();
                }
            }
            else
            {
                // Khi tắt X-Ray: Xóa toàn bộ những Material nào có tên là xrayMaterial
                matList.RemoveAll(m => m.name.Contains(xrayMaterial.name));
                r.materials = matList.ToArray();
            }
        }
    }

    private void ClearAllHighlights()
    {
        // Tắt skill thì dọn dẹp sạch sẽ
        foreach (var hunter in _scannedHunters)
        {
            SetXRayOnHunter(hunter, false);
        }
    }

    public void SetRepairAnimation(bool isRepairing)
    {
        IsRepairingAnim = isRepairing;
    }

    private bool IsNearDeadBody()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 0.5f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Playerchet")) return true;
        }
        return false;
    }

    private void HandleMovement(MrBeastGameplayInput input)
    {
        _characterController.enabled = false;
        _characterController.enabled = true;
        Vector3 direction = CalculateDirection(input.moveDirection, input.camForward, input.camRight);

        if (direction.magnitude > 1f) direction.Normalize();

        float speed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;
        if (_isLocalRepairing || IsRepairingAnim)
        {
            // Vẫn phải giữ trọng lực để nhân vật không rớt xuyên map
            if (_characterController.isGrounded && velocityY < 0) velocityY = -2f;
            velocityY += -9.81f * Runner.DeltaTime;
            _characterController.Move(new Vector3(0, velocityY, 0) * Runner.DeltaTime);

            AnimSpeedValue = 0f; // Ép tắt animation chạy
            return; // 🚨 KẾT THÚC HÀM TẠI ĐÂY, VÔ HIỆU HÓA WASD
        }

        // BỎ logic skillActive ở đây vì skill không còn tăng tốc nữa
        if (input.isWalking) { speed = slowWalkSpeed; targetAnimSpeed = 0.2f; }
        else if (input.isSprinting) { speed = sprintSpeed; targetAnimSpeed = 1f; }

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

            // 🚨 SỬA LẠI: Tuyệt đối không cho Proxy tự bật CC lên
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

    private void HandleWindowInput(MrBeastGameplayInput input)
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

    private void HandleSkillInput(MrBeastGameplayInput input)
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

        // 🚨 THÊM ĐOẠN NÀY
        if (playerAudioSource != null && hitSound != null)
        {
            playerAudioSource.PlayOneShot(hitSound, 1f * GetVFXVolume());
        }
    }

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
    private void RPC_PlayVaultAnim()
    {
        animator.SetTrigger(vaultAnimationTrigger);

        // 🚨 THÊM ĐOẠN NÀY
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
            // Lấy thời gian còn lại
            float timeLeft = SacrificeTimer.RemainingTime(Runner) ?? 0f;

            // Chia cho sacrificeTime (90f) để ra tỷ lệ 0 -> 1
            hookSlider.value = timeLeft / sacrificeTime;
        }
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

        if (!IsDowned && !IsHooked && other.CompareTag("Playerchet"))
        {
            var target = other.GetComponentInParent<ISurvivor>();

            if (target != null && (target.GetIsDowned() || target.GetIsHooked()) && target.Object.Id != this.Object.Id)
            {
                bool isTargetHooked = target.GetIsHooked();

                // 🚨 LUẬT MÓC: Nếu nạn nhân đang bị treo VÀ đã có người tháo -> Chặn đứng người thứ 2!
                if (isTargetHooked && target.GetIsBeingUnhooked() && !IsUnhooking)
                {
                    if (revivePrompt) revivePrompt.SetActive(false); // Tắt UI chữ E
                    return; // Văng ra ngay lập tức, không cho tương tác
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
        PlayerDeadthBox.SetActive(false);
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
    }

    // Lấy âm lượng VFX từ Setting
    private float GetVFXVolume()
    {
        return (AudioManager.Instance != null) ? AudioManager.Instance.vfxVolume : 1f;
    }

    // Hàm gọi từ Animation Event để phát tiếng bước chân
    public void PlayFootstepSound()
    {
        if (footstepSounds.Length == 0 || playerAudioSource == null) return;

        // Tránh lỗi đứng im nhưng animation vẫn lướt tạo ra tiếng
        if (_characterController.velocity.magnitude > 0.1f)
        {
            AudioClip clip = footstepSounds[UnityEngine.Random.Range(0, footstepSounds.Length)];
            // Phát âm thanh ngẫu nhiên với âm lượng VFX
            playerAudioSource.PlayOneShot(clip, 0.6f * GetVFXVolume());
        }
    }

    // Hệ thống nhịp tim đập nhanh dần
    private void UpdateHeartbeat()
    {
        // Chỉ người chơi điều khiển nhân vật này mới nghe tiếng tim đập của chính họ
        if (!Object.HasInputAuthority || heartbeatSource == null) return;

        GameObject[] hunters = GameObject.FindGameObjectsWithTag("Hunter");
        float minDistance = float.MaxValue;

        // Tìm quái gần nhất
        foreach (var h in hunters)
        {
            float dist = Vector3.Distance(transform.position, h.transform.position);
            if (dist < minDistance) minDistance = dist;
        }

        if (minDistance <= terrorRadius)
        {
            // Tính toán cường độ: 0 (ở rìa 30m) -> 1 (ở sát bên 0m)
            float intensity = 1f - (minDistance / terrorRadius);

            // Càng gần âm lượng càng to, nhịp (pitch) đập càng nhanh
            heartbeatSource.volume = intensity * GetVFXVolume();
            heartbeatSource.pitch = Mathf.Lerp(1f, maxHeartbeatPitch, intensity);

            if (!heartbeatSource.isPlaying) heartbeatSource.Play();
        }
        else
        {
            // Quái ở xa -> Tắt tiếng tim
            heartbeatSource.volume = 0f;
        }
    }

    // Hàm phát âm thanh tách rời (Dùng cho tiếng hét chết)
    // Vì khi chết Object bị Despawn xóa ngay lập tức, nếu phát trên Object sẽ bị tắt ngang
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
        Destroy(audioObj, clip.length + 0.1f); // Tự hủy rác sau khi hét xong
    }

    // 🚨 HIỂN THỊ VÙNG QUÉT TRONG SCENE VIEW
    private void OnDrawGizmosSelected()
    {
        // Chọn màu cho vòng tròn (Màu vàng)
        Gizmos.color = Color.yellow;

        // Vẽ một hình cầu dạng lưới (WireSphere) xung quanh nhân vật bằng với bán kính quét
        Gizmos.DrawWireSphere(transform.position, scanDistance);

        // (Tùy chọn) Thêm một lớp màu trong suốt ở bên trong cho dễ nhìn
        Gizmos.color = new Color(1f, 1f, 0f, 0.2f); // Vàng trong suốt 20%
        Gizmos.DrawSphere(transform.position, scanDistance);
    }

    // --- INetworkRunnerCallbacks ---
    public void OnInput(NetworkRunner runner, NetworkInput input)
    {
        var myInput = new MrBeastGameplayInput();

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
    public float GetRepairSpeedMultiplier() => 1f; // Tốc độ cơ bản 1x
    public void OnStartRepair()
    {
        _isLocalRepairing = true; // Bấm E là khóa chân ngay
    }
    public void OnStopRepair()
    {
        _isLocalRepairing = false; // Thả E hoặc nổ máy là mở khóa
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
        // 🚨 CHỐNG LỖI CRASH: Không tính toán nếu object chưa Spawn xong hoặc đã bị hủy
        if (Object == null || !Object.IsValid) return 0f;

        if (IsHooked) return UnhookProgress / unhookTime;
        if (IsDowned) return ReviveProgress / reviveTime;
        return 0f;
    }

    // 🚨 HÀM MỚI ĐÃ NHẬN INPUT
    private void HandleReviveLogic(MrBeastGameplayInput input)
    {
        // ==========================================
        // 1. DÀNH CHO SERVER: Xử lý tiến trình và ngắt tự động
        // ==========================================
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

        // ==========================================
        // 2. DÀNH CHO CLIENT & HOST: Xử lý Bấm/Nhả phím E bằng OverlapSphere
        // ==========================================
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            bool isPressingE = input.isInteract;

            // NẾU ĐANG CỨU: Kiểm tra xem có nhả phím E ra không
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
            // NẾU CHƯA CỨU: Quét vùng xung quanh xem có ai để cứu không khi giữ E
            else
            {
                _isCancelRpcSent = false;

                if (isPressingE && !_isStartRpcSent && !IsDowned && !IsHooked)
                {
                    // Tự động quét tìm nạn nhân gần nhất thay vì chờ OnTriggerStay
                    Collider[] hits = Physics.OverlapSphere(transform.position, 0.5f);
                    foreach (var hit in hits)
                    {
                        if (hit.CompareTag("Playerchet"))
                        {
                            var target = hit.GetComponentInParent<ISurvivor>();
                            if (target != null && (target.GetIsDowned() || target.GetIsHooked()) && target.Object.Id != this.Object.Id)
                            {
                                bool isTargetHooked = target.GetIsHooked();
                                if (isTargetHooked && target.GetIsBeingUnhooked()) continue; // Chặn người thứ 2

                                _targetToRevive = target;
                                RPC_SetReviveState(true, target.Object.Id, isTargetHooked);
                                _isStartRpcSent = true;
                                break; // Tìm thấy 1 người là cứu luôn, không quét tiếp
                            }
                        }
                    }
                }

                if (!isPressingE) _isStartRpcSent = false;
            }
        }
    }

    // 🚨 1. THÊM RPC NÀY ĐỂ BÁO CHO QUÁI BIẾT LÀ MÓC ĐÃ TRỐNG
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    public void RPC_ResetHookAtPosition(Vector3 rescuePosition)
    {
        // Quét tìm cái móc xung quanh vị trí vừa được cứu (bán kính 2 mét)
        Collider[] hits = Physics.OverlapSphere(rescuePosition, 2f);
        foreach (var hit in hits)
        {
            // Quái đã đổi tag của móc thành "Untagged" lúc nãy
            if (hit.CompareTag("Untagged") || hit.CompareTag("Moc"))
            {
                // Nhận diện chính xác đây là cái móc nhờ child "HookPoint"
                if (hit.transform.Find("HookPoint") != null)
                {
                    hit.tag = "Moc"; // 🚨 QUAN TRỌNG NHẤT: Trả lại Tag "Moc" để Quái tương tác được
                    break;
                }
            }
        }
    }

    // =========================================================
    // HÀM 1: XUỐNG MÓC (Mượt 100% bằng logic Revive)
    // =========================================================
    public void CompleteRescueFromOther()
    {
        // 🚨 CHỈ SERVER MỚI ĐƯỢC XỬ LÝ
        if (!Object.HasStateAuthority) return;

        // Mở khóa móc
        RPC_ResetHookAtPosition(transform.position);

        // Dịch chuyển nhẹ ra trước trên Server
        Vector3 dropPos = transform.root.position - transform.root.forward * 1.5f;
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null) netTransform.Teleport(dropPos, transform.rotation);

        // 🚨 PHÉP MÀU NẰM Ở ĐÂY: Reset state giống hệt lúc Revive
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
        // 🚨 CHỈ SERVER MỚI ĐƯỢC XỬ LÝ
        if (!Object.HasStateAuthority) return;

        // 1. TẮT CHARACTER CONTROLLER TRƯỚC ĐỂ KHÔNG BỊ KẸT
        if (_characterController != null) _characterController.enabled = false;

        // 2. NGẮT KẾT NỐI VỚI HUNTER
        transform.SetParent(null);

        // 3. TÍNH TOÁN VỊ TRÍ RỚT AN TOÀN
        // Lấy vị trí của Hunter (root) đẩy ra phía trước mặt 1.5 mét
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
        dropPos.y += 0.5f; // Nâng lên nửa mét để chống rớt xuyên map

        // 4. DÙNG LỆNH TELEPORT CHUẨN CỦA FUSION
        var netTransform = GetComponent<NetworkTransform>();
        if (netTransform != null)
        {
            netTransform.Teleport(dropPos, transform.rotation);
        }

        // 5. CẬP NHẬT TRẠNG THÁI
        IsDowned = false;
        IsHooked = false;
        IsBeingRevived = false;

        // Nhận 2 hit thương nặng
        CurrentHits = 2;
        HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 20f);
        InvincibilityTimer = TickTimer.CreateFromSeconds(Runner, 3f);

        // 6. BẬT LẠI CC CHO SERVER VÀ NGƯỜI CHƠI ĐÓ
        if (_characterController != null) _characterController.enabled = true;
    }
    public float GetSacrificeTimer() => SacrificeTimer.RemainingTime(Runner) ?? 0f;
}

public struct MrBeastGameplayInput : INetworkInput
{
    public Vector2 moveDirection;
    // 🚨 THÊM 2 BIẾN NÀY ĐỂ GỬI HƯỚNG CAMERA LÊN SERVER
    public Vector3 camForward;
    public Vector3 camRight;

    public NetworkBool isSprinting;
    public NetworkBool isWalking;
    public NetworkBool isVaulting;
    public NetworkBool isSkill;
    public NetworkBool isInteract;
}