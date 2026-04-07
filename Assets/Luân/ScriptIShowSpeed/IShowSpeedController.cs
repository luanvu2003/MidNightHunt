using UnityEngine;
using UnityEngine.InputSystem;
using Unity.Mathematics;
using System.Collections;
using UnityEngine.UI;
using TMPro;

[RequireComponent(typeof(CharacterController))]
public class IShowSpeedController : MonoBehaviour
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
    [Tooltip("Nút để nhảy qua cửa sổ (Ví dụ: Space hoặc E)")]
    public InputActionReference vaultInput; // Đổi tên từ jumpInput để tránh nhầm lẫn
    public InputActionReference skillInput;

    [Header("Window Vaulting Settings")]
    public GameObject interactUI;
    public string vaultAnimationTrigger = "Vault";
    public float vaultDuration = 1.5f;
    public float vaultDistance = 2.5f;

    [Header("Speed Skill Settings")]
    public float skillSpeedBonus = 3f;
    public float skillDuration = 10f;
    public float skillCooldown = 30f;

    [Header("Skill UI References")]
    public Slider durationSlider;
    public Image cooldownImage;
    public TextMeshProUGUI cooldownText;

    [Header("Health & States")]
    public string hitAnimationTrigger = "AnHit";
    public string downedAnimationBool = "BiGuc";
    public string hookedAnimationBool = "BiTreo";
    public float sacrificeTime = 90f;

    [Header("Hook UI")]
    public Slider hookSlider;

    [Header("Camera Reference")]
    public Transform mainCamera;

    private CharacterController characterController;
    private bool isNearWindow = false;
    private bool isVaulting = false;
    private bool isSkillActive = false;
    private bool isSkillOnCooldown = false;

    private int currentHits = 0;
    private float hitDecayTimer = 0f;
    public bool isDowned = false;
    public bool isHooked = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (interactUI != null) interactUI.SetActive(false);
        if (durationSlider != null) durationSlider.gameObject.SetActive(false);
        if (cooldownImage != null) cooldownImage.gameObject.SetActive(false);

        if (cooldownText != null)
        {
            cooldownText.text = "";
            cooldownText.gameObject.SetActive(false); // Đảm bảo text tắt lúc đầu
        }

        if (hookSlider != null) hookSlider.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        moveInput.action.Enable();
        sprintInput.action.Enable();
        walkInput.action.Enable();
        if (vaultInput != null) vaultInput.action.Enable();
        if (skillInput != null) skillInput.action.Enable();
    }

    private void OnDisable()
    {
        moveInput.action.Disable();
        sprintInput.action.Disable();
        walkInput.action.Disable();
        if (vaultInput != null) vaultInput.action.Disable();
        if (skillInput != null) skillInput.action.Disable();
    }

    public void Update()
    {
        if (mainCamera == null || characterController == null) return;

        HandleHitDecay();

        if (isDowned || isHooked) return;

        HandleSkillInput();

        if (isVaulting) return;

        HandleMovement();
        HandleWindowInteraction(); // Chỉ thực hiện nhảy khi gặp cửa sổ
    }

    private void HandleHitDecay()
    {
        if (currentHits == 0 || isDowned || isHooked) return;

        hitDecayTimer -= Time.deltaTime;
        if (hitDecayTimer <= 0)
        {
            currentHits = 0;
            hitDecayTimer = 0f;
        }
    }

    public void TakeHit()
    {
        if (isDowned || isHooked) return;

        currentHits++;
        if (currentHits == 1)
        {
            if (animator != null) animator.SetTrigger(hitAnimationTrigger);
            hitDecayTimer = 10f;
        }
        else if (currentHits == 2)
        {
            if (animator != null) animator.SetTrigger(hitAnimationTrigger);
            hitDecayTimer = 20f;
        }
        else if (currentHits >= 3)
        {
            isDowned = true;
            if (animator != null) animator.SetBool(downedAnimationBool, true);
            characterController.enabled = false;
        }
    }

    public void GetHooked(Vector3 hookPos)
    {
        if (isHooked) return;

        isHooked = true;
        isDowned = false;

        transform.position = hookPos;

        if (animator != null)
        {
            animator.SetBool(downedAnimationBool, false);
            animator.SetBool(hookedAnimationBool, true);
        }

        characterController.enabled = false;
        StartCoroutine(SacrificeRoutine());
    }

    private IEnumerator SacrificeRoutine()
    {
        if (hookSlider != null)
        {
            hookSlider.gameObject.SetActive(true);
            hookSlider.maxValue = sacrificeTime;
            hookSlider.value = sacrificeTime;
        }

        float timer = sacrificeTime;
        while (timer > 0 && isHooked)
        {
            timer -= Time.deltaTime;
            if (hookSlider != null) hookSlider.value = timer;
            yield return null;
        }

        if (timer <= 0)
        {
            Destroy(gameObject);
        }
    }

    private void HandleSkillInput()
    {
        if (skillInput != null && skillInput.action.triggered && !isSkillActive && !isSkillOnCooldown)
        {
            StartCoroutine(SpeedSkillRoutine());
        }
    }

    private IEnumerator SpeedSkillRoutine()
    {
        isSkillActive = true;
        if (durationSlider != null)
        {
            durationSlider.gameObject.SetActive(true);
            durationSlider.maxValue = skillDuration;
            durationSlider.value = skillDuration;
        }
        if (cooldownImage != null) { cooldownImage.gameObject.SetActive(true); cooldownImage.fillAmount = 1f; }

        float timeLeft = skillDuration;
        while (timeLeft > 0 && !isDowned && !isHooked)
        {
            timeLeft -= Time.deltaTime;
            if (durationSlider != null) durationSlider.value = timeLeft;
            yield return null;
        }

        isSkillActive = false;
        isSkillOnCooldown = true;
        if (durationSlider != null) durationSlider.gameObject.SetActive(false);

        // HIỆN TEXT KHI BẮT ĐẦU COOLDOWN
        if (cooldownText != null) cooldownText.gameObject.SetActive(true);

        timeLeft = skillCooldown;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (cooldownImage != null) cooldownImage.fillAmount = timeLeft / skillCooldown;
            if (cooldownText != null) cooldownText.text = Mathf.Ceil(timeLeft).ToString();
            yield return null;
        }
        isSkillOnCooldown = false;
        if (cooldownImage != null) cooldownImage.gameObject.SetActive(false);
        if (cooldownText != null)
        {
            cooldownText.text = "";
            cooldownText.gameObject.SetActive(false); // Tắt text khi xong
        }
    }

    private void HandleMovement()
    {
        if (!characterController.enabled) return;

        Vector2 moveInputValue = moveInput.action.ReadValue<Vector2>();
        Vector3 camForward = Vector3.Scale(mainCamera.forward, new Vector3(1, 0, 1)).normalized;
        Vector3 camRight = Vector3.Scale(mainCamera.right, new Vector3(1, 0, 1)).normalized;
        Vector3 moveDirection = (camForward * moveInputValue.y) + (camRight * moveInputValue.x);

        float currentMoveSpeed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;

        if (isSkillActive)
        {
            currentMoveSpeed = sprintSpeed + skillSpeedBonus;
            targetAnimSpeed = 1f;
        }
        else
        {
            if (walkInput.action.IsPressed()) { currentMoveSpeed = slowWalkSpeed; targetAnimSpeed = 0.2f; }
            else if (sprintInput.action.IsPressed()) { currentMoveSpeed = sprintSpeed; targetAnimSpeed = 1f; }
        }

        if (moveDirection.magnitude == 0) targetAnimSpeed = 0f;

        if (moveDirection.magnitude >= 0.1f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(moveDirection), rotationSpeed * Time.deltaTime);
            characterController.Move(moveDirection * currentMoveSpeed * Time.deltaTime);
        }

        if (animator != null)
            animator.SetFloat("Speed", Mathf.Lerp(animator.GetFloat("Speed"), targetAnimSpeed, Time.deltaTime * 10f));
    }

    private void HandleWindowInteraction()
    {
        // BUG FIX: Chỉ cho phép nhấn nút khi đang ở gần cửa sổ
        if (isNearWindow && vaultInput != null && vaultInput.action.triggered)
        {
            StartCoroutine(VaultWindowRoutine());
        }
    }

    private IEnumerator VaultWindowRoutine()
    {
        isVaulting = true;

        // BUG FIX: Ép biến này về false ngay lập tức khi bắt đầu nhảy
        // Để tránh việc sau khi nhảy xong máy vẫn tưởng mình đang ở gần cửa sổ.
        isNearWindow = false;

        if (interactUI != null) interactUI.SetActive(false);
        if (animator != null) animator.SetTrigger(vaultAnimationTrigger);

        characterController.enabled = false;

        Vector3 startPos = transform.position;
        Vector3 targetPos = transform.position + (transform.forward * vaultDistance);

        float elapsedTime = 0f;
        while (elapsedTime < vaultDuration)
        {
            float t = elapsedTime / vaultDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPos, targetPos, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPos;

        // Sau khi nhảy xong mới bật lại va chạm
        characterController.enabled = true;
        isVaulting = false;

        // Cẩn thận hơn: Kiểm tra lại một lần nữa xem có thực sự thoát khỏi Trigger chưa
        // (Dòng này giúp reset UI nếu bạn vô tình nhảy nhưng vẫn còn dính collider)
        if (interactUI != null) interactUI.SetActive(false);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cuaso"))
        {
            isNearWindow = true;
            if (interactUI != null && !isVaulting && !isDowned && !isHooked) interactUI.SetActive(true);
        }
        else if (other.CompareTag("HunterHit"))
        {
            TakeHit();
        }
        else if (other.CompareTag("Moc") && isDowned)
        {
            GetHooked(other.transform.position);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Cuaso"))
        {
            isNearWindow = false;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }
}
// using UnityEngine;
// using UnityEngine.InputSystem;
// using Fusion;
// using Unity.Mathematics;

// [RequireComponent(typeof(CharacterController))]
// public class PlayerController : NetworkBehaviour // Đổi thành NetworkBehaviour
// {
//     [Header("Animator Settings")]
//     public Animator animator;

//     [Header("Movement Settings")]
//     public float slowWalkSpeed = 2f;    // Tốc độ đi bộ (Khi giữ Left Ctrl)
//     public float mediumRunSpeed = 5f;   // Tốc độ chạy vừa (Mặc định)
//     public float sprintSpeed = 8f;      // Tốc độ chạy nhanh (Khi giữ Sprint)
//     public float rotationSpeed = 10f;

//     [Header("Jump & Gravity Settings")]
//     public float jumpHeight = 2f;
//     public float gravity = -15f; // Tăng trọng lực để nhân vật rơi đầm hơn
//     private Vector3 velocity;
//     private bool isGrounded;

//     [Header("Input Settings")]
//     public InputActionReference moveInput;
//     public InputActionReference jumpInput;
//     public InputActionReference attackInput;
//     public InputActionReference sprintInput;
//     public InputActionReference walkInput;

//     [Header("Camera Reference")]
//     public Transform mainCamera;

//     private CharacterController characterController;

//     private void Awake()
//     {
//         characterController = GetComponent<CharacterController>();
//         // Tự động tìm Animator ở mô hình 3D nằm bên trong (đối tượng Con)
//         animator = GetComponentInChildren<Animator>();
//     }

//     private void OnEnable()
//     {
//         moveInput.action.Enable();
//         jumpInput.action.Enable();
//         attackInput.action.Enable();
//         sprintInput.action.Enable();
//         walkInput.action.Enable();
//     }

//     private void OnDisable()
//     {
//         moveInput.action.Disable();
//         jumpInput.action.Disable();
//         attackInput.action.Disable();
//         sprintInput.action.Disable();
//         walkInput.action.Disable();
//     }

//     // Dùng Spawned thay cho Start trong Fusion
//     public override void Spawned()
//     {
//         // 1. QUẢN LÝ VẬT LÝ (Bật cho Chủ nhân VÀ Bật cho Host để tính toán trọng lực)
//         // Nếu là máy của mình (Input) HOẶC nếu mình là Chủ phòng (State) -> Bật CharacterController
//         if (HasStateAuthority || HasInputAuthority)
//         {
//             if (characterController != null) characterController.enabled = true;
//         }
//         else
//         {
//             // Chỉ tắt đối với những người đứng xem (Proxy)
//             if (characterController != null) characterController.enabled = false;
//         }

//         // 2. QUẢN LÝ CAMERA (Tuyệt đối CHỈ bắt Camera cho máy của chính mình)
//         if (HasInputAuthority)
//         {
//             if (mainCamera == null && Camera.main != null)
//             {
//                 mainCamera = Camera.main.transform;
//             }

//             ThirdPersonCamera camScript = FindAnyObjectByType<ThirdPersonCamera>();
//             if (camScript != null)
//             {
//                 camScript.target = this.transform;
//                 Debug.Log("✅ Đã bắt được Camera vào nhân vật!");
//             }
//         }
//     }
//     // Dùng FixedUpdateNetwork để đồng bộ hoàn hảo trên mạng
//     public override void FixedUpdateNetwork()
//     {
//         // Chặn: Nếu không phải nhân vật của mình thì không cho phép tính toán di chuyển
//         if (HasInputAuthority == false && HasStateAuthority == false) return;

//         if (mainCamera == null) return;
//         if (characterController != null && characterController.enabled == false) return;

//         HandleMovement();
//         applyGravityAndJumping();
//     }

//     private void HandleMovement()
//     {
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
//             // Đổi Time.deltaTime thành Runner.DeltaTime
//             transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Runner.DeltaTime);

//             // Đổi Time.deltaTime thành Runner.DeltaTime
//             characterController.Move(moveDirection * currentMoveSpeed * Runner.DeltaTime);
//         }

//         if (animator != null)
//         {
//             float currentAnimSpeed = animator.GetFloat("Speed");
//             // Đổi Time.deltaTime thành Runner.DeltaTime
//             animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Runner.DeltaTime * 10f));
//         }
//     }

//     private void applyGravityAndJumping()
//     {
//         isGrounded = characterController.isGrounded;

//         if (isGrounded && velocity.y < 0)
//         {
//             velocity.y = -2f;
//             if (animator != null) animator.SetBool("IsGrounded", true);
//         }
//         else
//         {
//             if (animator != null) animator.SetBool("IsGrounded", false);
//         }

//         if (jumpInput.action.triggered && isGrounded)
//         {
//             velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
//             if (animator != null) animator.SetTrigger("Jump");
//         }

//         // Đổi Time.deltaTime thành Runner.DeltaTime
//         velocity.y += gravity * Runner.DeltaTime;
//         characterController.Move(velocity * Runner.DeltaTime);
//     }
// }