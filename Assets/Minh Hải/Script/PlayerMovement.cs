using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool isSprinting; // Biến này để Quạ kiểm tra
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f; // Tốc độ khi chạy nhanh
    
    private float currentSpeed;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
    }

    void Update()
    {
        // KIỂM TRA NHẤN SHIFT ĐỂ CHẠY
        if (Input.GetKey(KeyCode.LeftShift) && (Input.GetAxis("Horizontal") != 0 || Input.GetAxis("Vertical") != 0))
        {
            isSprinting = true;
            currentSpeed = sprintSpeed;
        }
        else
        {
            isSprinting = false;
            currentSpeed = walkSpeed;
        }
    }

    void FixedUpdate()
    {
        float h = Input.GetAxis("Horizontal");
        float v = Input.GetAxis("Vertical");

        Vector3 move = new Vector3(h, 0, v);
        // Dùng currentSpeed để thay đổi tốc độ linh hoạt
        rb.MovePosition(transform.position + move * currentSpeed * Time.fixedDeltaTime);
    }
}