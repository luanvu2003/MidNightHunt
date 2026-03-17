using Unity.VisualScripting;
using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class HammerProjectile : MonoBehaviour
{
    public float throwForce = 20f;
    public float lifeTime = 3f;
    private Rigidbody rb;
    void Start()
    {
        rb = GetComponent<Rigidbody>();

        // Cấp lực đẩy thẳng về phía trước (trục Z)
        rb.linearVelocity = transform.forward * throwForce;

        // Cho nó xoay tròn tròn nhìn cho ngầu (Quay quanh trục X)
        rb.angularVelocity = new Vector3(20f, 0f, 0f);

        Destroy(gameObject, lifeTime);
    }

    private void OnTriggerEnter(Collider other)
    {
        // Nếu đập trúng đối tượng mang Tag NguoiChoi
        if (other.CompareTag("Player"))
        {
            Debug.Log("Đã ném trúng đầu Player!");
            // TODO: Trừ máu Player ở đây
            
            // Trúng rồi thì nổ/biến mất luôn
            Destroy(gameObject);
        }
    }
}
