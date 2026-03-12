using UnityEngine;
using UnityEngine.InputSystem;

[RequireComponent(typeof(CharacterController), typeof(Animator))] // Thêm dòng này cho chuẩn
public class HunterController : MonoBehaviour
{
    [Header("Cai Dat Toc Do")]
    public float walkstraight = 5f; // Đi tới
    public float walkbackward = 2.5f; // Đi lùi
    public float rotationSpeed = 15f; // ĐÃ THÊM: Tốc độ xoay mặt

    [Header("Controller")]
    private HunterControllerInput input;
    private CharacterController controller; 
    private Animator animator;
    
    private float currentSpeed; // ĐÃ SỬA: Lỗi dư chữ 'n'
    private float velocityY;

    [Header("Animation")]
    private readonly int animSpeed = Animator.StringToHash("Speed");
    private readonly int animWalkStraight = Animator.StringToHash("Dithang");
    private readonly int animWalkBackward = Animator.StringToHash("Dilui");
    private readonly int animpickup = Animator.StringToHash("Nhacplayer");
    private readonly int animstep = Animator.StringToHash("Dapmay");
    private readonly int animclimb = Animator.StringToHash("Treocuaso");

    private void Awake()
    {
        // ĐÃ SỬA: Lấy đúng component
        controller = GetComponent<CharacterController>();
        animator = GetComponent<Animator>();
        
        // ĐÃ SỬA: Khởi tạo Input system đúng chuẩn
        input = new HunterControllerInput(); 
    }

    private void OnEnable()
    {
        input.Enable();
    }

    private void OnDisable()
    {
        input.Disable();
    }

    private void Update()
    {
        HandleMovementAndRotation();
        UpdateAnimator(); // ĐÃ THÊM: Quên gọi hàm này thì Animation không chạy đâu nhé
    }

    private void HandleMovementAndRotation()
    {
        // ĐỌC PHÍM
        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();
        Vector3 moveDirection = new Vector3(inputDir.x, 0f, inputDir.y).normalized;

        // TỐC ĐỘ MỤC TIÊU
        float targetSpeed = 0f;
        if(inputDir.y < 0)
        {
            targetSpeed = walkbackward; 
        }
        else if(moveDirection.magnitude > 0)
        {
            targetSpeed = walkstraight; 
        }

        // XOAY MẶT
        if(moveDirection.magnitude >= 0.1f)
        {
            if(inputDir.y >= 0)
            {
                float targetAngle = Mathf.Atan2(moveDirection.x, moveDirection.z) * Mathf.Rad2Deg;
                float angle = Mathf.LerpAngle(transform.eulerAngles.y, targetAngle, rotationSpeed * Time.deltaTime);
                transform.rotation = Quaternion.Euler(0f, angle, 0f);
            }
        }

        // THỰC THI DI CHUYỂN
        currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f);
        controller.Move(moveDirection * (currentSpeed * Time.deltaTime)); // ĐÃ SỬA: Lỗi gạch đỏ ở đây
    }

    private void UpdateAnimator()
    {
        float animationSpeedPercent = currentSpeed / walkstraight;

        Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>();
        if(inputDir.y < 0)
        {
            animationSpeedPercent = -animationSpeedPercent;
        }
        
        animator.SetFloat(animSpeed, animationSpeedPercent);
    }
}