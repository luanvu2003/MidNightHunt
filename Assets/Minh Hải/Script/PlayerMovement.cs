using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public bool isSprinting; 
    public float walkSpeed = 5f;
    public float sprintSpeed = 9f; 
    
    private float currentSpeed;
    Rigidbody rb;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        currentSpeed = walkSpeed;
    }
    void Update()
    {
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
        rb.MovePosition(transform.position + move * currentSpeed * Time.fixedDeltaTime);
    }
}