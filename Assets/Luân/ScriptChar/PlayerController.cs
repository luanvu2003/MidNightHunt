using UnityEngine;
using UnityEngine.InputSystem;
using Fusion;

[RequireComponent(typeof(CharacterController))]
[RequireComponent(typeof(Animator))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement Settings")]
    public float slowWalkSpeed = 2f;    // Tốc độ đi bộ (Khi giữ Left Ctrl)
    public float mediumRunSpeed = 5f;   // Tốc độ chạy vừa (Mặc định)
    public float sprintSpeed = 8f;      // Tốc độ chạy nhanh (Khi giữ Sprint)
    public float rotationSpeed = 10f;

    [Header("Jump & Gravity Settings")]
    public float jumpHeight = 2f;
    public float gravity = -9.81f;
    private Vector3 velocity;
    private bool isGrounded;

    [Header("Input Settings")]
    public InputActionReference moveInput;
    public InputActionReference jumpInput;
    public InputActionReference attackInput;
    public InputActionReference sprintInput;
    public InputActionReference walkInput; // Thêm Action này cho nút Left Ctrl

    [Header("Camera Reference")]
    public Transform mainCamera; // Sẽ kéo Main Camera vào đây trên Inspector

    private CharacterController characterController;
    private Animator animator;

    private void Awake()
    {
        characterController = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        moveInput.action.Enable();
        jumpInput.action.Enable();
        attackInput.action.Enable();
        sprintInput.action.Enable();
        walkInput.action.Enable(); // Bật input đi bộ

        // attackInput.action.performed += OnAttack;
    }

    private void OnDisable()
    {
        moveInput.action.Disable();
        jumpInput.action.Disable();
        attackInput.action.Disable();
        sprintInput.action.Disable();
        walkInput.action.Disable(); // Tắt input đi bộ

        // attackInput.action.performed -= OnAttack;
    }

    public void Start()
    {
        // 1. XỬ LÝ CAMERA: CHỈ bắt camera nếu đây là nhân vật CỦA TÔI
        // if (Object.HasInputAuthority)
        // {
        //     if (mainCamera == null && Camera.main != null)
        //     {
        //         mainCamera = Camera.main.transform;
        //     }

        //     ThirdPersonCamera camScript = FindAnyObjectByType<ThirdPersonCamera>();
        //     if (camScript != null)
        //     {
        //         camScript.target = this.transform;
        //         Debug.Log("✅ [THÀNH CÔNG] Đã bắt được Camera vào nhân vật!");
        //     }
        // }
        // else
        // {
        //     // 2. XỬ LÝ ĐỨNG YÊN: Nếu là nhân vật của người khác (Proxy), TẮT CharacterController đi
        //     // Để NetworkTransform của Fusion có thể thoải mái kéo nhân vật này đi theo tọa độ mạng
        //     if (characterController != null)
        //     {
        //         characterController.enabled = false;
        //     }
        // }
    }


    public void FixedUpdate()
    {
        // 1. Chặn cơ bản của Fusion
        // if (Object.HasInputAuthority == false && Object.HasStateAuthority == false) return;

        // 2. CHỐT CHẶN QUAN TRỌNG: Nếu không có camera (tức là nhân vật của người khác trên máy mình) -> Dừng lại!
        if (mainCamera == null) return;

        // 3. CHỐT CHẶN PHỤ: Nếu CharacterController đang bị tắt -> Dừng lại để tránh lỗi báo vàng của Unity!
        if (characterController != null && characterController.enabled == false) return;

        HandleMovement();
        applyGravityAndJumping();
    }

    private void HandleMovement()
    {
        Vector2 moveInputValue = moveInput.action.ReadValue<Vector2>();

        // --- ĐIỂM KHÁC BIỆT LỚN NHẤT Ở ĐÂY ---
        // Lấy hướng nhìn thẳng (forward) và hướng ngang (right) của Camera
        Vector3 camForward = mainCamera.forward;
        Vector3 camRight = mainCamera.right;

        // Ép trục Y về 0 để nhân vật không cắm đầu xuống đất hoặc bay lên trời khi camera nhìn lên/xuống
        camForward.y = 0f;
        camRight.y = 0f;
        camForward.Normalize();
        camRight.Normalize();

        // Tính toán hướng di chuyển cuối cùng dựa trên hướng camera
        Vector3 moveDirection = (camForward * moveInputValue.y) + (camRight * moveInputValue.x);
        // ------------------------------------

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
            // Xoay nhân vật mượt mà về hướng đang di chuyển
            Quaternion targetRotation = Quaternion.LookRotation(moveDirection);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, rotationSpeed * Time.deltaTime);

            characterController.Move(moveDirection * currentMoveSpeed * Time.deltaTime);
        }

        float currentAnimSpeed = animator.GetFloat("Speed");
        animator.SetFloat("Speed", Mathf.Lerp(currentAnimSpeed, targetAnimSpeed, Time.deltaTime * 10f));
    }

    private void applyGravityAndJumping()
    {
        isGrounded = characterController.isGrounded;
        if (isGrounded && velocity.y < 0)
        {
            velocity.y = -2f;
            animator.SetBool("IsGrounded", true);
        }
        else
        {
            animator.SetBool("IsGrounded", false);
        }

        if (jumpInput.action.triggered && isGrounded)
        {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            animator.SetTrigger("Jump");
        }

        velocity.y += gravity * Time.deltaTime;
        characterController.Move(velocity * Time.deltaTime);
    }

    // private void OnAttack(InputAction.CallbackContext context)
    // {
    //     // CHẶN NGAY: Ngăn không cho nhân vật người khác đánh theo khi mình bấm chuột
    //     if (Object.HasInputAuthority == false && Object.HasStateAuthority == false) return;

    //     if (isGrounded)
    //     {
    //         animator.SetTrigger("Attack");
    //         Debug.Log("Player Attacked!");
    //     }
    // }
}