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
    public InputActionReference jumpInput;
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

    [Tooltip("Dùng Image thay vì Slider để quét tròn đẹp hơn")]
    public Image cooldownImage; // Đổi lại thành Image
    public TextMeshProUGUI cooldownText;

    [Header("Camera Reference")]
    public Transform mainCamera;

    private CharacterController characterController;
    private bool isNearWindow = false;
    private bool isVaulting = false;

    private bool isSkillActive = false;
    private bool isSkillOnCooldown = false;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();

        if (interactUI != null) interactUI.SetActive(false);

        // Ẩn tất cả lúc mới vào game
        if (durationSlider != null) durationSlider.gameObject.SetActive(false);
        if (cooldownImage != null) cooldownImage.gameObject.SetActive(false); // Ẩn lớp mờ
        if (cooldownText != null) cooldownText.text = "";
    }

    private void OnEnable()
    {
        moveInput.action.Enable();
        sprintInput.action.Enable();
        walkInput.action.Enable();
        if (jumpInput != null) jumpInput.action.Enable();
        if (skillInput != null) skillInput.action.Enable();
    }

    private void OnDisable()
    {
        moveInput.action.Disable();
        sprintInput.action.Disable();
        walkInput.action.Disable();
        if (jumpInput != null) jumpInput.action.Disable();
        if (skillInput != null) skillInput.action.Disable();
    }

    public void Update()
    {
        if (mainCamera == null || characterController == null) return;

        HandleSkillInput();

        if (isVaulting) return;

        HandleMovement();
        HandleWindowInteraction();
    }

    private void HandleSkillInput()
    {
        if (skillInput != null && skillInput.action.triggered)
        {
            if (!isSkillActive && !isSkillOnCooldown)
            {
                StartCoroutine(SpeedSkillRoutine());
            }
        }
    }

    private IEnumerator SpeedSkillRoutine()
    {
        // -----------------------------------------------------
        // GIAI ĐOẠN 1: ĐANG SỬ DỤNG KỸ NĂNG (10s)
        // -----------------------------------------------------
        isSkillActive = true;

        // Bật thanh Slider 10s
        if (durationSlider != null)
        {
            durationSlider.gameObject.SetActive(true);
            durationSlider.maxValue = skillDuration;
            durationSlider.value = skillDuration;
        }

        // Bật lớp mờ Cooldown lên che icon, tô viền ĐẦY (1f) nhưng CHƯA HIỆN SỐ
        if (cooldownImage != null)
        {
            cooldownImage.gameObject.SetActive(true);
            cooldownImage.fillAmount = 1f;
        }

        // Đảm bảo Text được bật lên nhưng để trống
        if (cooldownText != null)
        {
            cooldownText.gameObject.SetActive(true);
            cooldownText.text = "";
        }

        // Chạy tụt thanh Slider 10s
        float timeLeft = skillDuration;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;
            if (durationSlider != null) durationSlider.value = timeLeft;
            yield return null;
        }

        // -----------------------------------------------------
        // GIAI ĐOẠN 2: HẾT KỸ NĂNG -> BẮT ĐẦU HỒI CHIÊU (30s)
        // -----------------------------------------------------
        isSkillActive = false;
        isSkillOnCooldown = true;

        // Tắt hẳn Slider 10s
        if (durationSlider != null) durationSlider.gameObject.SetActive(false);

        // Chạy tụt lớp mờ Overlay và Đếm số
        timeLeft = skillCooldown;
        while (timeLeft > 0)
        {
            timeLeft -= Time.deltaTime;

            // Ép Overlay quét hình tròn dần dần (từ 1 về 0)
            if (cooldownImage != null) cooldownImage.fillAmount = timeLeft / skillCooldown;

            // Ép Text đếm số giây
            if (cooldownText != null) cooldownText.text = Mathf.Ceil(timeLeft).ToString();

            yield return null;
        }

        // -----------------------------------------------------
        // GIAI ĐOẠN 3: HỒI CHIÊU XONG
        // -----------------------------------------------------
        isSkillOnCooldown = false;

        // Tắt lớp mờ và xóa số
        if (cooldownImage != null) cooldownImage.gameObject.SetActive(false);
        if (cooldownText != null) cooldownText.text = "";
    }

    private void HandleMovement()
    {
        if (!characterController.enabled) return;

        Vector2 moveInputValue = moveInput.action.ReadValue<Vector2>();

        Vector3 camForward = mainCamera.forward;
        Vector3 camRight = mainCamera.right;

        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        Vector3 moveDirection = (camForward * moveInputValue.y) + (camRight * moveInputValue.x);

        bool isSprinting = sprintInput.action.IsPressed();
        bool isWalking = walkInput.action.IsPressed();

        float currentMoveSpeed = mediumRunSpeed;
        float targetAnimSpeed = 0.5f;

        if (isSkillActive)
        {
            currentMoveSpeed = sprintSpeed + skillSpeedBonus;
            targetAnimSpeed = 1f;
        }
        else
        {
            if (isWalking)
            {
                currentMoveSpeed = slowWalkSpeed;
                targetAnimSpeed = 0.2f;
            }
            else if (isSprinting)
            {
                currentMoveSpeed = sprintSpeed;
                targetAnimSpeed = 1f;
            }
        }

        if (moveDirection.magnitude == 0)
        {
            targetAnimSpeed = 0f;
        }

        if (moveDirection.magnitude >= 0.1f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            characterController.Move(moveDirection * currentMoveSpeed * Time.deltaTime);
        }

        if (animator != null)
        {
            float currentAnimSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 10f));
        }
    }

    private void HandleWindowInteraction()
    {
        if (isNearWindow && jumpInput != null && jumpInput.action.triggered)
        {
            StartCoroutine(VaultWindowRoutine());
        }
    }

    private IEnumerator VaultWindowRoutine()
    {
        isVaulting = true;
        if (interactUI != null) interactUI.SetActive(false);
        if (animator != null) animator.SetTrigger(vaultAnimationTrigger);

        characterController.enabled = false;

        Vector3 startPosition = transform.position;
        Vector3 targetPosition = transform.position + (transform.forward * vaultDistance);

        float elapsedTime = 0f;

        while (elapsedTime < vaultDuration)
        {
            float t = elapsedTime / vaultDuration;
            t = Mathf.SmoothStep(0f, 1f, t);

            transform.position = Vector3.Lerp(startPosition, targetPosition, t);

            elapsedTime += Time.deltaTime;
            yield return null;
        }

        transform.position = targetPosition;
        characterController.enabled = true;
        isVaulting = false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Cuaso"))
        {
            isNearWindow = true;
            if (interactUI != null && !isVaulting) interactUI.SetActive(true);
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