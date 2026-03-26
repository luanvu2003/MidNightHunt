using UnityEngine;

public class BearTrap : MonoBehaviour
{
    private bool isSprung = false;

    private void OnTriggerEnter(Collider other)
    {
        // Tạm thời cứ ai/cái gì dẫm vào là nổ (để test cho dễ)
        if (!isSprung)
        {
            isSprung = true;
            Debug.Log("🐻 PHẬP! Bẫy đã nổ tại vị trí: " + transform.position);
            
            // Tạm thời vứt hiệu ứng / trừ máu sang một bên
        }
    }
}