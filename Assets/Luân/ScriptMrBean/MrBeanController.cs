// using UnityEngine;
// using UnityEngine.InputSystem;
// using Fusion;
// using Unity.Mathematics;
// using System.Collections;

// [RequireComponent(typeof(CharacterController))]
// public class MrBeanController : MonoBehaviour
// {
//     [Header("Animator Settings")]
//     public Animator animator;

//     [Header("Movement Settings")]
//     public float slowWalkSpeed = 2f;
//     public float mediumRunSpeed = 5f;
//     public float sprintSpeed = 8f;
//     public float rotationSpeed = 10f;

//     [Header("Input Settings")]
//     public InputActionReference moveInput;
//     public InputActionReference sprintInput;
//     public InputActionReference walkInput;
//     public InputActionReference jumpInput;

//     [Header("Window Vaulting Settings")]
//     public GameObject interactUI;
//     public string vaultAnimationTrigger = "Vault";
//     public float vaultDuration = 1.5f;
//     [Tooltip("Khoảng cách trượt qua cửa sổ")]
//     public float vaultDistance = 2.5f; // Đã thêm biến khoảng cách

//     [Header("Camera Reference")]
//     public Transform mainCamera;

//     private CharacterController characterController;

//     private bool isNearWindow = false;
//     private bool isVaulting = false;

//     private void Awake()
//     {
//         characterController = GetComponent<CharacterController>();
//         animator = GetComponentInChildren<Animator>();

//         if (interactUI != null) interactUI.SetActive(false);
//     }

//     private void OnEnable()
//     {
//         moveInput.action.Enable();
//         sprintInput.action.Enable();
//         walkInput.action.Enable();
//         if (jumpInput != null) jumpInput.action.Enable();
//     }

//     private void OnDisable()
//     {
//         moveInput.action.Disable();
//         sprintInput.action.Disable();
//         walkInput.action.Disable();
//         if (jumpInput != null) jumpInput.action.Disable();
//     }

//     public void Update()
//     {
//         if (mainCamera == null) return;

//         // Cập nhật: Chỉ return nếu không có CC, không block nếu CC bị tắt (để xử lý vụ trượt cửa sổ)
//         if (characterController == null) return;

//         // Chặn di chuyển tự do khi đang chạy animation đu cửa sổ
//         if (isVaulting) return;

//         HandleMovement();
//         HandleWindowInteraction();
//     }

//     private void HandleMovement()
//     {
//         // Chặn di chuyển nếu CharacterController đang tắt
//         if (!characterController.enabled) return;

//         Vector2 moveInputValue = moveInput.action.ReadValue<Vector2>();

//         Vector3 camForward = mainCamera.forward;
//         Vector3 camRight = mainCamera.right;

//         camForward.y = 0f;
//         camRight.y = 0f;
//         camForward.Normalize();
//         camRight.Normalize();

//         Vector3 moveDirection = (camForward * moveInputValue.y) + (camRight * moveInputValue.x);

//         bool isSprinting = sprintInput.action.IsPressed();
//         bool isWalking = walkInput.action.IsPressed();

//         float currentMoveSpeed = mediumRunSpeed;
//         float targetAnimSpeed = 0.5f;

//         if (isWalking)
//         {
//             currentMoveSpeed = slowWalkSpeed;
//             targetAnimSpeed = 0.2f;
//         }
//         else if (isSprinting)
//         {
//             currentMoveSpeed = sprintSpeed;
//             targetAnimSpeed = 1f;
//         }

//         if (moveDirection.magnitude == 0)
//         {
//             targetAnimSpeed = 0f;
//         }

//         if (moveDirection.magnitude >= 0.1f)
//         {
//             Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
//             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

//             characterController.Move(moveDirection * currentMoveSpeed * Time.deltaTime);
//         }

//         if (animator != null)
//         {
//             float currentAnimSpeed = animator.GetFloat("Speed");
//             animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 10f));
//         }
//     }

//     private void HandleWindowInteraction()
//     {
//         if (isNearWindow && jumpInput != null && jumpInput.action.triggered)
//         {
//             StartCoroutine(VaultWindowRoutine());
//         }
//     }

//     private IEnumerator VaultWindowRoutine()
//     {
//         isVaulting = true;

//         if (interactUI != null) interactUI.SetActive(false);

//         if (animator != null) animator.SetTrigger(vaultAnimationTrigger);

//         // QUAN TRỌNG: Tắt CharacterController để không bị kẹt vào tường/cửa sổ khi trượt
//         characterController.enabled = false;

//         // Lưu lại vị trí xuất phát và tính điểm rơi
//         Vector3 startPosition = transform.position;
//         Vector3 targetPosition = transform.position + (transform.forward * vaultDistance);

//         float elapsedTime = 0f;

//         // Vòng lặp trượt nhân vật mượt mà
//         while (elapsedTime < vaultDuration)
//         {
//             float t = elapsedTime / vaultDuration;
//             t = Mathf.SmoothStep(0f, 1f, t); // Làm mượt gia tốc

//             transform.position = Vector3.Lerp(startPosition, targetPosition, t);

//             elapsedTime += Time.deltaTime;
//             yield return null;
//         }

//         // Đảm bảo đáp xuống đúng vị trí
//         transform.position = targetPosition;

//         // Bật lại CharacterController để đi lại bình thường
//         characterController.enabled = true;
//         isVaulting = false;
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Cuaso"))
//         {
//             isNearWindow = true;
//             if (interactUI != null && !isVaulting)
//             {
//                 interactUI.SetActive(true);
//             }
//         }
//     }

//     private void OnTriggerExit(Collider other)
//     {
//         if (other.CompareTag("Cuaso"))
//         {
//             isNearWindow = false;
//             if (interactUI != null)
//             {
//                 interactUI.SetActive(false);
//             }
//         }
//     }

// }

using UnityEngine;
using UnityEngine.InputSystem;
using Fusion; // Thư viện Fusion
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class MrBeanController_Fusion : NetworkBehaviour // Đổi từ MonoBehaviour
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
    public GameObject PlayerDeadthBox;

    [Header("Revive Settings")]
    public float reviveTime = 90f; // Đặt là 90s theo yêu cầu
    public string revivingAnimBool = "IsReviving"; // Tên parameter trong Animator
    private MrBeanController_Fusion _targetToRevive;
    public InputActionReference interactInput; // Gán phím E vào đây
    public GameObject revivePrompt; // Cái Text hoặc Icon hiện chữ "Nhấn E để cứu"
    public Slider reviveSlider;     // Thanh Slider chạy từ 0 đến 1

    [Networked] public NetworkBool IsBeingRevived { get; set; } // Đang được cứu
    [Networked] public NetworkBool IsReviving { get; set; } // Đang đi cứu người khác
    [Networked] private TickTimer ReviveTimer { get; set; }
    [Networked] private NetworkObject ReviverObject { get; set; } // Lưu thông tin người đang cứu mình

    private CharacterController _characterController;
    private bool _isNearWindow = false;

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

    public override void Spawned()
    {
        _characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        // Chỉ bật UI và Camera cho chính chủ (máy của người chơi này)
        if (Object.HasInputAuthority)
        {
            if (mainCamera == null) mainCamera = Camera.main?.transform;
            InitUI();
        }
    }

    private void InitUI()
    {
        if (interactUI) interactUI.SetActive(false);
        if (durationSlider) durationSlider.gameObject.SetActive(false);
        if (cooldownImage) cooldownImage.gameObject.SetActive(false);
        if (cooldownText) { cooldownText.text = ""; cooldownText.gameObject.SetActive(false); }
        if (hookSlider) hookSlider.gameObject.SetActive(false);
    }

    // FixedUpdateNetwork là Update của Fusion (Đồng bộ mọi máy)
    public override void FixedUpdateNetwork()
    {
        // 1. Logic hồi phục Hit ngầm
        if (CurrentHits > 0 && HitDecayTimer.Expired(Runner))
        {
            CurrentHits = 0;
        }

        // 2. Chết (Destroy) khi hết thời gian trên móc
        if (IsHooked && SacrificeTimer.Expired(Runner))
        {
            Runner.Despawn(Object);
            return;
        }

        // 3. Khóa di chuyển nếu Gục hoặc Treo
        if (IsDowned || IsHooked) return;

        // 4. Xử lý nhảy cửa sổ (Priority cao)
        if (IsVaulting)
        {
            HandleVaultingMovement();
            return;
        }

        // 5. Input và di chuyển bình thường
        HandleSkillInput();
        HandleMovement();
        HandleWindowInput();
        HandleReviveLogic();
    }

    // Render dùng để cập nhật UI và Animator mượt mà (chạy theo FPS máy khách)
    public override void Render()
    {
        // Các dòng code cũ của bạn...
        animator.SetBool(downedAnimationBool, IsDowned);
        animator.SetBool(hookedAnimationBool, IsHooked);

        // Thêm dòng này để chạy animation cứu người (dạng bool)
        animator.SetBool(revivingAnimBool, IsReviving);

        if (Object.HasInputAuthority)
        {
            UpdateSkillUI();
            UpdateHookUI();

            // --- PHẦN 1: HIỆN NÚT E ---
            // Nút E hiện lên khi: Bạn còn sống VÀ ở gần xác đồng đội VÀ chưa bắt đầu cứu
            bool canShowE = !IsDowned && IsNearDeadBody() && !IsReviving;
            if (revivePrompt) revivePrompt.SetActive(canShowE);

            // --- PHẦN 2: HIỆN THANH TIẾN TRÌNH SLIDER ---
            UpdateReviveProgressUI();
        }
    }

    // Hàm hỗ trợ kiểm tra xem có xác người chơi nào ở gần không để hiện UI
    private bool IsNearDeadBody()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 2f);
        foreach (var hit in hitColliders)
        {
            if (hit.CompareTag("Playerchet")) return true;
        }
        return false;
    }

    private void HandleMovement()
    {
        if (mainCamera == null) return;

        Vector2 moveVal = moveInput.action.ReadValue<Vector2>();
        Vector3 direction = CalculateDirection(moveVal);

        float speed = mediumRunSpeed;
        float animSpeed = 0.5f;

        bool skillActive = !SkillDurationTimer.ExpiredOrNotRunning(Runner);

        if (skillActive)
        {
            speed = sprintSpeed + skillSpeedBonus;
            animSpeed = 1f;
        }
        else
        {
            if (walkInput.action.IsPressed()) { speed = slowWalkSpeed; animSpeed = 0.2f; }
            else if (sprintInput.action.IsPressed()) { speed = sprintSpeed; animSpeed = 1f; }
        }

        if (direction.magnitude == 0) animSpeed = 0f;

        if (direction.magnitude >= 0.1f)
        {
            _characterController.Move(direction * speed * Runner.DeltaTime);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(direction), rotationSpeed * Runner.DeltaTime);
        }

        animator.SetFloat("Speed", Mathf.Lerp(animator.GetFloat("Speed"), animSpeed, Runner.DeltaTime * 10f));
    }

    private void HandleWindowInput()
    {
        if (_isNearWindow && vaultInput.action.triggered)
        {
            IsVaulting = true;
            _isNearWindow = false;
            VaultStartPos = transform.position;
            VaultTargetPos = transform.position + (transform.forward * vaultDistance);
            VaultTimer = TickTimer.CreateFromSeconds(Runner, vaultDuration);

            // Trigger animation qua mạng
            RPC_PlayVaultAnim();
        }
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

    private void HandleSkillInput()
    {
        if (skillInput.action.triggered && SkillCooldownTimer.ExpiredOrNotRunning(Runner))
        {
            SkillDurationTimer = TickTimer.CreateFromSeconds(Runner, skillDuration);
            SkillCooldownTimer = TickTimer.CreateFromSeconds(Runner, skillDuration + skillCooldown);
        }
    }

    public void TakeHit()
    {
        // Chỉ StateAuthority (Host) mới được xử lý máu
        if (IsDowned || IsHooked || !Object.HasStateAuthority) return;

        CurrentHits++;

        // Kiểm tra xem nhân vật có đang di chuyển hay không (vận tốc > 0.1)
        bool isMoving = _characterController.velocity.magnitude > 0.1f;

        // Truyền trạng thái di chuyển sang RPC để đồng bộ animation cho mọi máy
        RPC_PlayHitAnim(isMoving);

        // Logic thời gian phục hồi hit ngầm
        if (CurrentHits == 1) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 10f);
        else if (CurrentHits == 2) HitDecayTimer = TickTimer.CreateFromSeconds(Runner, 20f);
        else if (CurrentHits >= 3)
        {
            IsDowned = true;
            _characterController.enabled = false;
            PlayerDeadthBox.SetActive(true);
        }
    }

    // Thay đổi RPC: Thêm tham số NetworkBool isMoving
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim(NetworkBool isMoving)
    {
        // Set biến IsMoving vào Animator để hệ thống tự chia Layer
        animator.SetBool("IsMoving", isMoving);
        animator.SetTrigger(hitAnimationTrigger);
    }
    public void GetHooked(Vector3 hookPos)
    {
        if (IsHooked || !Object.HasStateAuthority) return;
        IsHooked = true;
        IsDowned = false;
        transform.position = hookPos;
        SacrificeTimer = TickTimer.CreateFromSeconds(Runner, sacrificeTime);
    }

    // --- RPCs (Gửi lệnh thực thi animation cho mọi máy) ---
    [Rpc(RpcSources.All, RpcTargets.All)]
    private void RPC_PlayVaultAnim() => animator.SetTrigger(vaultAnimationTrigger);

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayHitAnim() => animator.SetTrigger(hitAnimationTrigger);

    // --- UI UPDATES (Local) ---
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

    private Vector3 CalculateDirection(Vector2 input)
    {
        Vector3 forward = Vector3.Scale(mainCamera.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 right = Vector3.Scale(mainCamera.right, new Vector3(1, 0, 1)).normalized;
        return (forward * input.y + right * input.x);
    }

    // --- VA CHẠM ---
    private void OnTriggerEnter(Collider other)
    {
        // Chỉ Host/StateAuthority mới xử lý trừ máu/treo móc
        if (!Object.HasStateAuthority) return;

        if (other.CompareTag("Cuaso")) _isNearWindow = true;
        else if (other.CompareTag("HunterHit")) TakeHit();
        else if (other.CompareTag("Moc") && IsDowned) GetHooked(other.transform.position);
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cuaso")) _isNearWindow = false;
        else if (Object.HasInputAuthority && other.CompareTag("Playerchet"))
        {
            // Nếu đang cứu mà đi ra khỏi vùng box thì hủy cứu
            if (IsReviving) RPC_SetReviveState(false, default); // Sử dụng default thay cho NetworkId.None
            _targetToRevive = null; // Xóa mục tiêu khi đi xa
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (!Object.HasInputAuthority) return;

        if (!IsDowned && other.CompareTag("Playerchet"))
        {
            var target = other.GetComponentInParent<MrBeanController_Fusion>();
            if (target != null && target.IsDowned)
            {
                _targetToRevive = target; // Lưu lại để lấy Slider progress ở trên

                if (interactInput.action.IsPressed())
                {
                    if (!IsReviving) RPC_SetReviveState(true, target.Object.Id);
                }
                else
                {
                    if (IsReviving) RPC_SetReviveState(false, target.Object.Id);
                }
            }
        }
    }


    [Rpc(RpcSources.InputAuthority, RpcTargets.StateAuthority)]
    private void RPC_SetReviveState(NetworkBool start, NetworkId targetId)
    {
        IsReviving = start; // Bật/tắt animation người cứu

        if (targetId.IsValid)
        {
            var target = Runner.FindObject(targetId).GetComponent<MrBeanController_Fusion>();
            if (target != null)
            {
                target.IsBeingRevived = start;
                if (start)
                    target.ReviveTimer = TickTimer.CreateFromSeconds(Runner, reviveTime);
                else
                    target.ReviveTimer = TickTimer.None; // Reset timer nếu ngừng cứu
            }
        }
    }

    private void HandleReviveLogic()
    {
        // Chỉ chạy trên Server (StateAuthority)
        if (!Object.HasStateAuthority) return;

        if (IsBeingRevived && ReviveTimer.Expired(Runner))
        {
            CompleteRevive();
        }
    }

    private void CompleteRevive()
    {
        IsDowned = false;
        IsBeingRevived = false;
        _characterController.enabled = true;
        PlayerDeadthBox.SetActive(false); // Ẩn box khi được cứu xong
        CurrentHits = 1; // Cho phép hồi phục về 1 hit thay vì 0 để tránh vừa dậy đã full máu
    }

    private void UpdateReviveProgressUI()
    {
        if (reviveSlider == null) return;

        // Hiển thị Slider nếu BẠN đang cứu người khác HOẶC BẠN đang được người khác cứu
        bool showingSlider = IsReviving || IsBeingRevived;
        reviveSlider.gameObject.SetActive(showingSlider);

        if (showingSlider)
        {
            // Lấy thời gian còn lại từ TickTimer
            // Vì mỗi người gục có một ReviveTimer riêng, chúng ta cần xác định đang lấy Timer của ai
            float? remainingTime = 0;

            if (IsBeingRevived)
                remainingTime = ReviveTimer.RemainingTime(Runner); // Nếu mình là người bị gục
            else if (IsReviving)
                // Nếu mình là người đi cứu, mình cần lấy Timer từ thằng đang được mình cứu
                // (Đoạn này bạn nên lưu target player vào một biến local như _targetToRevive)
                remainingTime = _targetToRevive != null ? _targetToRevive.ReviveTimer.RemainingTime(Runner) : 0;

            if (remainingTime.HasValue)
            {
                // Tính toán giá trị Slider (0 đến 1)
                // Công thức: 1 - (Thời gian còn lại / Tổng thời gian 90s)
                float progress = 1f - (remainingTime.Value / reviveTime);
                reviveSlider.value = progress;
            }
        }
    }
}