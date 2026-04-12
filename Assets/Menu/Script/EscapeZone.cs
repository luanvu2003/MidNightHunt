using UnityEngine;
using Fusion;

public class EscapeZone : NetworkBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var playerObj = other.GetComponentInParent<NetworkObject>();
            
            // Nếu người bước vào vùng Win chính là máy trạm của bạn
            if (playerObj != null && playerObj.HasInputAuthority)
            {
                Debug.Log("🎉 MỘT SURVIVOR ĐÃ THOÁT KHỎI BẢN ĐỒ!");
                
                // --- BẠN SẼ VIẾT CODE WIN Ở ĐÂY ---
                // Ví dụ: Bật Panel WIN UI, Tắt Player, Gọi hàm Win về cho Server...
                
                // Mẫu: 
                // UIManager.Instance.ShowWinScreen();
                // playerObj.gameObject.SetActive(false);
            }
        }
    }
}