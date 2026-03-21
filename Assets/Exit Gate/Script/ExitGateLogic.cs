using UnityEngine;
using UnityEngine.UI;

public class ExitGateLogic : MonoBehaviour
{
    public Slider progressSlider; // Kéo Slider vào đây
    public GameObject gateMesh;   // Kéo cánh cổng vào đây
    public float openSpeed = 0.2f; // Tốc độ mở (0.2 là khoảng 5 giây)
    
    private bool isPlayerInside = false;

    void Start() {
        progressSlider.value = 0; // Lúc đầu thanh tiến trình bằng 0
    }

    void Update() {
        // Nếu người chơi ở trong vùng và giữ phím E
        if (isPlayerInside && Input.GetKey(KeyCode.T) && GameManager.Instance.CanOpenExitGate()) {
            progressSlider.value += openSpeed * Time.deltaTime;
            
            if (progressSlider.value >= 1f) {
                gateMesh.SetActive(false); // Mở cổng (ẩn mesh)
            }
        }
    }

    // Kiểm tra xem Player có đứng trong vùng Trigger không
    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player")) isPlayerInside = true;
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) isPlayerInside = false;
    }
}