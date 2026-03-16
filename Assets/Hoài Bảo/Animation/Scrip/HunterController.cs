using System;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class HunterController : MonoBehaviour
{
    [Header("Cai Dat Toc Do")]
    public float walkstraight = 5f; // Đi tới
    public float walkbackward = 5f; // Đi lùi
    // ĐÃ XÓA: rotationSpeed (Vì phần xoay người bây giờ do chuột và script FPSCamera đảm nhận)
    [Header("Trạng thái")]
    public bool isInteracting = false;
    [Header("Tương tác")]
    public Transform fpsCamera;
    public float interactRange = 3f; // 2. Tầm tay của Hunter (Dài 3 mét)
    public GameObject interactUI;

    [Header("Controller")]
    private HunterControllerInput input;
    private CharacterController controller;
    private Animator animator;

    private float currentSpeed;
    private float velocityY; // Vận tốc trục Y (Dùng để tính trọng lực kéo xuống đất)

    [Header("Animation")]
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animWalkStraight = Animator.StringToHash("Dithang");
    private readonly int animWalkBackward = Animator.StringToHash("Dilui");
    private readonly int animpickup = Animator.StringToHash("Nhacplayer");
    private readonly int animstep = Animator.StringToHash("Dapmay");
    private readonly int animclimb = Animator.StringToHash("Treocuaso");
    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        input = new HunterControllerInput();
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        if (isInteracting) return;
        HandleMovement();
        CheckInteractable();
        UpdateAnimator();
    }

    // =================================================================
    // HỆ THỐNG DÒ TÌM MỤC TIÊU BẰNG "ỐNG NƯỚC" (SPHERECAST)
    // =================================================================
    private void CheckInteractable()
    {
        if (fpsCamera == null || interactUI == null) return;

        Ray ray = new Ray(fpsCamera.position, fpsCamera.forward);
        RaycastHit hit;

        // ĐÃ NÂNG CẤP: Dùng SphereCast thay vì Raycast.
        // Số 0.5f ở giữa chính là "Bán kính" của chùm tia (Tia to chà bá 1 mét).
        // Quét trúng cực kỳ dễ, không cần phải ngắm kỹ nữa!
        if (Physics.SphereCast(ray, 0.5f, out hit, interactRange))
        {
            bool isValidTarget = hit.collider.CompareTag("May") || 
                                 hit.collider.CompareTag("Moc") || 
                                 hit.collider.CompareTag("Player");

            if (isValidTarget)
            {
                interactUI.SetActive(true); 

                if (Keyboard.current.eKey.wasPressedThisFrame)
                {
                    interactUI.SetActive(false); 
                    PerformInteraction(hit.collider.tag);
                }
            }
            else
            {
                interactUI.SetActive(false);
            }
        }
        else
        {
            interactUI.SetActive(false);
        }
    }
    public void PerformInteraction(string targetTag)
    {
        isInteracting = true; // khóa chân
        if(targetTag == ("May"))
        {
            animator.SetTrigger(animstep);
        }
        else if(targetTag == ("Moc"))
        {
            animator.SetTrigger(animclimb);
        }
        else if(targetTag == ("Player"))
        {
            animator.SetTrigger(animpickup);
        }

    }
    public void FinishInteraction()
    {
        isInteracting = false; // trả lại tự do cho tôi chân
    }

    private void HandleMovement()
    {
        // =================================================================
        // 1. ĐỌC PHÍM VÀ TÍNH HƯỚNG ĐI (CHUẨN FPS)
        // =================================================================
        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();

        // THAY ĐỔI QUAN TRỌNG NHẤT:
        // transform.forward: Vectơ chỉ thẳng về phía trước mặt nhân vật.
        // transform.right: Vectơ chỉ sang bên phải nhân vật.
        // Ta nhân phím W/S (inputDir.y) với hướng trước mặt, và A/D (inputDir.x) với hướng ngang.
        Vector3 moveDirection = (transform.forward * inputDir.y + transform.right * inputDir.x).normalized;

        // =================================================================
        // 2. TỐC ĐỘ MỤC TIÊU
        // =================================================================
        float targetSpeed = 0f;
        if (inputDir.y < 0)
        {
            targetSpeed = walkbackward;
        }
        else if (moveDirection.magnitude > 0)
        {
            targetSpeed = walkstraight;
        }

        // =================================================================
        // 3. XỬ LÝ TRỌNG LỰC (GRAVITY) ĐỂ KHÔNG BỊ BAY
        // =================================================================
        // isGrounded: Lệnh kiểm tra xem chân nhân vật có đang chạm đất không
        if (controller.isGrounded && velocityY < 0)
        {
            velocityY = -2f; // Ép 1 lực nhỏ xuống để dính chặt vào sàn
        }
        // Trọng lực kéo xuống (-9.81 là chuẩn trái đất, có thể tăng lên -20f nếu muốn nặng hơn)
        velocityY += -9.81f * Time.deltaTime;

        // =================================================================
        // 4. THỰC THI DI CHUYỂN
        // =================================================================
        // Lerp: Nội suy làm mượt tốc độ để có cảm giác đà (trớn)
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f);

        // Gộp hướng đi ngang (moveDirection) và hướng rơi dọc (velocityY) vào 1 lệnh Move duy nhất
        controller.Move(moveDirection * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        float animationSpeedPercent = currentSpeed / walkstraight;

        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();
        if (inputDir.y < 0)
        {
            animationSpeedPercent = -animationSpeedPercent;
        }

        animator.SetFloat(animSpeed, animationSpeedPercent);
    }
    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("May"))           /////// bug
        {
            if (Input.GetKeyDown(KeyCode.E))
            {
                animator.SetTrigger("Dapmay");
            }
        }
    }
}