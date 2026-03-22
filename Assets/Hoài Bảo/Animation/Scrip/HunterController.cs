using System;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterController), typeof(Animator))]
public class HunterController : MonoBehaviour
{
    [Header("Cai Dat Toc Do")]
    public float walkstraight = 5f;
    public float walkbackward = 5f;

    [Header("Trạng thái")]
    public bool isInteracting = false;
    public float currentSpeedMultiplier = 1f;

    [Header("Tương tác (Đứng gần)")]
    public GameObject interactUI;
    public Slider interactionSlider;

    // ĐÃ SỬA: Bổ sung đầy đủ 4 mốc thời gian cho 4 hành động khác nhau
    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f; // Thời gian leo cửa sổ thường khá nhanh
    public float timeTreoMoc = 3.0f;   // Thời gian móc người lên
    public float timeNhatPlayer = 1.5f;

    private float currentInteractionDuration = 1f; 
    private Collider currentInteractTarget;

    private bool isSliderRunning = false;
    private float sliderTimer = 0f;

    [Header("Controller")]
    private HunterControllerInput input;
    private CharacterController controller;
    private Animator animator;
    private FPSCamera fpsCameraScript; 

    private float currentSpeed;
    private float velocityY;

    [Header("Animation")]
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animWalkStraight = Animator.StringToHash("Dithang");
    private readonly int animWalkBackward = Animator.StringToHash("Dilui");
    private readonly int animpickup = Animator.StringToHash("Nhacplayer");
    private readonly int animstep = Animator.StringToHash("Dapmay");
    private readonly int animclimb = Animator.StringToHash("Treocuaso");
    private readonly int animhang = Animator.StringToHash("Treomoc");

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        input = new HunterControllerInput();

        if (Camera.main != null)
        {
            fpsCameraScript = Camera.main.GetComponent<FPSCamera>();
        }
    }

    private void Start()
    {
        if (interactionSlider != null) interactionSlider.gameObject.SetActive(false);
        if (interactUI != null) interactUI.SetActive(false);
    }

    private void OnEnable() => input.Enable();
    private void OnDisable() => input.Disable();

    private void Update()
    {
        HandleSliderProgress();

        if (isInteracting) return;

        HandleMovement();
        HandleInteractionInput();
        UpdateAnimator();
    }

    private void HandleInteractionInput()
    {
        if (currentInteractTarget != null)
        {
            if (Keyboard.current.spaceKey.wasPressedThisFrame)
            {
                if (interactUI != null) interactUI.SetActive(false);
                PerformInteraction(currentInteractTarget); 
            }
        }
    }

    private void HandleSliderProgress()
    {
        if (isSliderRunning && interactionSlider != null)
        {
            sliderTimer += Time.deltaTime;

            interactionSlider.value = sliderTimer / currentInteractionDuration;

            if (sliderTimer >= currentInteractionDuration)
            {
                isSliderRunning = false;
                interactionSlider.gameObject.SetActive(false);
            }
        }
    }

    // =================================================================
    // THỰC THI TƯƠNG TÁC
    // =================================================================
    public void PerformInteraction(Collider target)
    {
        isInteracting = true;
        string tag = target.tag;

        // ÉP NHÂN VẬT XOAY MẶT VỀ PHÍA ĐỒ VẬT
        Vector3 lookPosition = target.transform.position;
        lookPosition.y = transform.position.y; 
        transform.LookAt(lookPosition);

        // KHÓA CAMERA VÀ ĐỒNG BỘ GÓC
        if (fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true; 
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); 
        }

        // SET THỜI GIAN VÀ CHẠY ANIMATION TƯƠNG ỨNG MỘT CÁCH CHUẨN XÁC
        if (tag == "May")
        {
            animator.SetTrigger(animstep);
            currentInteractionDuration = timeDapMay;
        }
        else if (tag == "Moc")
        {
            animator.SetTrigger(animhang);
            currentInteractionDuration = timeTreoMoc; // Lấy đúng biến timeTreoMoc
        }
        else if (tag == "Player")
        {
            animator.SetTrigger(animpickup);
            currentInteractionDuration = timeNhatPlayer;
        }
        else if (tag == "Cuaso") // Đảm bảo Tag ngoài Unity bạn gõ đúng chữ C và S hoa/thường y như này nhé
        {
            animator.SetTrigger(animclimb);
            currentInteractionDuration = timeTreoCUASO; // Lấy đúng biến timeTreoCUASO
        }

        // BẬT THANH SLIDER
        if (interactionSlider != null)
        {
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.value = 0f;
            sliderTimer = 0f;
            isSliderRunning = true;
        }
    }

    // =================================================================
    // MỞ KHÓA MỌI THỨ KHI ANIMATION CHẠY XONG
    // =================================================================
    public void FinishInteraction()
    {
        isInteracting = false;

        if (fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = false;
        }

        isSliderRunning = false;
        if (interactionSlider != null)
        {
            interactionSlider.value = 1f; 
            interactionSlider.gameObject.SetActive(false); 
        }
    }

    // =================================================================
    // HỆ THỐNG VÙNG CHẠM
    // =================================================================
    private void OnTriggerEnter(Collider other)
    {
        // ĐÃ SỬA: Bổ sung thêm other.CompareTag("Cuaso") để nhận diện cửa sổ
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Player") || other.CompareTag("Cuaso"))
        {
            currentInteractTarget = other; 
            if (interactUI != null) interactUI.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (currentInteractTarget == other)
        {
            currentInteractTarget = null;
            if (interactUI != null) interactUI.SetActive(false);
        }
    }

    // =================================================================
    // DI CHUYỂN VÀ ANIMATION
    // =================================================================
    private void HandleMovement()
    {
        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();
        Vector3 moveDirection = (transform.forward * inputDir.y + transform.right * inputDir.x).normalized;

        float targetSpeed = 0f;
        if (inputDir.y < 0) targetSpeed = walkbackward * currentSpeedMultiplier;
        else if (moveDirection.magnitude > 0) targetSpeed = walkstraight * currentSpeedMultiplier;

        if (controller.isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += -9.81f * Time.deltaTime;

        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f);
        controller.Move(moveDirection * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);
    }

    private void UpdateAnimator()
    {
        float animationSpeedPercent = currentSpeed / walkstraight;
        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();
        if (inputDir.y < 0) animationSpeedPercent = -animationSpeedPercent;
        animator.SetFloat(animSpeed, animationSpeedPercent);
    }

    public void ApplySlow(float multiplier) => currentSpeedMultiplier = multiplier;
    public void ResetSlow() => currentSpeedMultiplier = 1f;
}