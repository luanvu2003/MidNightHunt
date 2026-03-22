using UnityEngine;

public class EscapeZone : MonoBehaviour
{
    // Khi có một vật thể chạm vào vùng này
    void OnTriggerEnter(Collider other)
    {
        // Kiểm tra xem vật thể đó có tag là "Player" (người chơi) hay không
        if (other.CompareTag("Player"))
        {
            // Báo ngay cho GameManager là có người đã tẩu thoát
            if (GameManager.instance != null)
            {
                GameManager.instance.SurvivorEscaped();
            }

            // Tùy chọn: Ẩn nhân vật đó đi (vì đã chạy thoát rồi)
            other.gameObject.SetActive(false); 
        }
    }
}