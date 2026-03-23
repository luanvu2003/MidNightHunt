using UnityEngine;
using UnityEngine.SceneManagement; // Để load lại cảnh hoặc menu

public class EscapeZone : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("CHÚC MỪNG! BẠN ĐÃ THOÁT!");
            // Ở đây bạn có thể hiện một cái UI "Victory" 
            // Hoặc load lại scene: SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}