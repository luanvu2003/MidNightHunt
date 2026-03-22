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

    [Header("Hệ thống Vác Người")]
    public Transform shoulderPoint; // ĐÃ THÊM: Điểm trên vai Hunter để dán Player vào
    public bool isCarryingPlayer = false; // Đang có vác ai không?
    private GameObject carriedPlayerObject; // Ghi nhớ cái xác đang vác trên vai

    [Header("Tương tác (Đứng gần)")]
    public GameObject interactUI;
    public Slider interactionSlider;

    public float timeDapMay = 2.0f;
    public float timeTreoCUASO = 1.5f; 
    public float timeTreoMoc = 3.0f;   
    public float timeNhatPlayer = 1.5f;

    [Header("Cài đặt Cửa sổ")]
    public float vaultDistance = 2.5f; 

    private float currentInteractionDuration = 1f; 
    private Collider currentInteractTarget;

    private bool isSliderRunning = false;
    private float sliderTimer = 0f;

    private bool isVaulting = false;
    private Vector3 vaultStartPos;
    private Vector3 vaultEndPos;

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
        HandleVaultingMovement(); 

        if (isInteracting) return;

        HandleMovement();
        HandleInteractionInput();
        UpdateAnimator();
    }

    private void HandleInteractionInput()
    {
        if (currentInteractTarget != null)
        {
            // LOGIC KIỂM TRA ĐIỀU KIỆN TRƯỚC KHI TƯƠNG TÁC
            string tag = currentInteractTarget.tag;

            if (tag == "Moc" && !isCarryingPlayer) 
            {
                // Muốn treo móc nhưng không có người trên vai -> Bỏ qua
                return; 
            }
            if (tag == "Player" && isCarryingPlayer)
            {
                // Muốn nhặt người nhưng vai đang vác 1 người rồi -> Bỏ qua
                return;
            }

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

    private void HandleVaultingMovement()
    {
        if (isVaulting)
        {
            float progress = sliderTimer / currentInteractionDuration;
            transform.position = Vector3.Lerp(vaultStartPos, vaultEndPos, progress);
        }
    }

    public void PerformInteraction(Collider target)
    {
        isInteracting = true;
        string tag = target.tag;

        Vector3 lookPosition = target.transform.position;
        lookPosition.y = transform.position.y; 
        transform.LookAt(lookPosition);

        if (fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = true; 
            fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); 
        }

        if (tag == "May")
        {
            animator.SetTrigger(animstep);
            currentInteractionDuration = timeDapMay;
        }
        else if (tag == "Moc")
        {
            animator.SetTrigger(animhang);
            currentInteractionDuration = timeTreoMoc; 
        }
        else if (tag == "Player")
        {
            animator.SetTrigger(animpickup);
            currentInteractionDuration = timeNhatPlayer;
            // GHI NHỚ LẠI CÁI XÁC ĐỂ LÁT NỮA HÚT LÊN VAI
            carriedPlayerObject = target.gameObject; 
        }
        else if (tag == "Cuaso") 
        {
            animator.SetTrigger(animclimb);
            currentInteractionDuration = timeTreoCUASO; 
            
            isVaulting = true;
            controller.enabled = false; 
            
            vaultStartPos = transform.position; 
            vaultEndPos = transform.position + transform.forward * vaultDistance; 
        }

        if (interactionSlider != null)
        {
            interactionSlider.gameObject.SetActive(true);
            interactionSlider.value = 0f;
            sliderTimer = 0f;
            isSliderRunning = true;
        }
    }

    // =================================================================
    // ANIMATION EVENT: GỌI LÚC HUNTER SỐC NGƯỜI CHƠI LÊN VAI
    // =================================================================
    public void AttachPlayerToShoulder()
    {
        if (carriedPlayerObject != null && shoulderPoint != null)
        {
            // Tắt va chạm của Player đi để Hunter chạy không bị vướng
            Collider playerCol = carriedPlayerObject.GetComponent<Collider>();
            if (playerCol != null) playerCol.enabled = false;

            // Hút Player vào cái xương vai
            carriedPlayerObject.transform.SetParent(shoulderPoint);
            carriedPlayerObject.transform.localPosition = Vector3.zero;
            carriedPlayerObject.transform.localRotation = Quaternion.identity;

            isCarryingPlayer = true;
        }
    }

    // =================================================================
    // ANIMATION EVENT: GỌI LÚC HUNTER PHÓNG NGƯỜI CHƠI VÀO MÓC
    // =================================================================
    public void HookPlayerToHook()
    {
        if (carriedPlayerObject != null && currentInteractTarget != null)
        {
            // Tìm cái điểm treo trên cái Móc (HookPoint)
            Transform hookPoint = currentInteractTarget.transform.Find("HookPoint");
            
            // Nếu quên tạo HookPoint thì dán tạm vào gốc cái Móc luôn
            if (hookPoint == null) hookPoint = currentInteractTarget.transform; 

            // Chuyển Player từ vai sang Móc
            carriedPlayerObject.transform.SetParent(hookPoint);
            carriedPlayerObject.transform.localPosition = Vector3.zero;
            carriedPlayerObject.transform.localRotation = Quaternion.identity;

            isCarryingPlayer = false;
            carriedPlayerObject = null;
        }
    }

    public void FinishInteraction()
    {
        isInteracting = false;

        if (fpsCameraScript != null)
        {
            fpsCameraScript.isCameraLockedForAnim = false;
        }

        if (isVaulting)
        {
            isVaulting = false;
            controller.enabled = true; 
        }

        isSliderRunning = false;
        if (interactionSlider != null)
        {
            interactionSlider.value = 1f; 
            interactionSlider.gameObject.SetActive(false); 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Player") || other.CompareTag("Cuaso"))
        {
            // ĐÃ THÊM: Logic UI thông minh
            if (other.CompareTag("Moc") && !isCarryingPlayer) return; // Không có người thì không hiện chữ E ở Móc
            if (other.CompareTag("Player") && isCarryingPlayer) return; // Có người rồi thì không hiện chữ E ở xác khác

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