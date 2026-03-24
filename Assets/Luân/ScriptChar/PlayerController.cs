using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;
using Unity.Mathematics;

[RequireComponent(typeof(CharacterController))]
public class PlayerController : MonoBehaviour
{
    [Header("Animator Settings")]
    public Animator animator;
    [Header("Movement Settings")]
    public float slowWalkSpeed = 2f;    // Tốc độ đi bộ (Khi giữ Left Ctrl)
    public float mediumRunSpeed = 5f;   // Tốc độ chạy vừa (Mặc định)
    public float sprintSpeed = 8f;      // Tốc độ chạy nhanh (Khi giữ Sprint)
    public float rotationSpeed = 10f;

    [Header("Jump & Gravity Settings")]
    public float jumpHeight = 2f;
    public float gravity = -15f; // Tăng trọng lực để nhân vật rơi đầm hơn
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Input Settings")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference attackInput;
    public InputActionReference sprintInput;
    public InputActionReference walkInput;

    [Header("Camera Reference")]
    public Transform mainCamera;

    private CharacterController characterController;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        // Tự động tìm Animator ở mô hình 3D nằm bên trong (đối tượng Con)
        animator = GetComponentInChildren<Animator>();
    }

    private void OnEnable()
    {
        moveInput.action.Enable();
        jumpInput.action.Enable();
        attackInput.action.Enable();
        sprintInput.action.Enable();
        walkInput.action.Enable();
    }

    private void OnDisable()
    {
        moveInput.action.Disable();
        jumpInput.action.Disable();
        attackInput.action.Disable();
        sprintInput.action.Disable();
        walkInput.action.Disable();
    }

    public void Start()
    {
        // Các logic chặn Camera và Fusion của bạn tôi vẫn giữ nguyên nếu cần mở lại
    }

    // Đổi thành Update thay vì FixedUpdate để Character Controller di chuyển mượt mà không bị delay nút bấm
    public void Update()
    {
        if (mainCamera == null) return;
        if (characterController != null && characterController.enabled == false) return;

        HandleMovement();
        applyGravityAndJumping();
    }

    private void HandleMovement()
    {
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

        // Bọc thêm check Null để an toàn nếu chưa có Animator
        if (animator != null)
        {
            float currentAnimSpeed = animator.GetFloat("Speed");
            animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 10f));
        }
    }

    private void applyGravityAndJumping()
    {
        isGrounded = characterController.isGrounded;

        // Trọng lực kéo xuống liên tục, khi chạm đất thì ép một lực nhẹ (-2f) để bám chặt sàn
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            if (animator != null) animator.SetBool("IsGrounded", true);
        }
        else
        {
            if (animator != null) animator.SetBool("IsGrounded", false);
        }

        if (jumpInput.action.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            if (animator != null) animator.SetTrigger("Jump");
        }

        // Áp dụng trọng lực vào vận tốc trục Y
        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
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