using UnityEngine;
using UnityEngine.UI;

public class ExitGateLogic : MonoBehaviour
{
    public Slider progressSlider;    
    public GameObject gateMesh;      
    public GameObject instructionText; 
    public SkillCheckExitGate skillCheck; // Thêm dòng này để kéo từ máy phát điện vào
    public float openSpeed = 0.2f;   
    
    private bool isPlayerInside = false;
    private bool isOpened = false;
    float skillTimer = 3f; // Thời gian chờ xuất hiện skill check đầu tiên

    void Start() {
        progressSlider.value = 0;
        progressSlider.gameObject.SetActive(false);
        instructionText.SetActive(false);
        if(skillCheck != null) skillCheck.gameObject.SetActive(false);
    }

    void Update() {
        if (isOpened) return;

        // Nếu Skill Check đang hiện thì dừng tiến trình mở cổng
        if (skillCheck != null && skillCheck.gameObject.activeSelf) return;

        if (isPlayerInside) {
            if (GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
                
                if (!Input.GetKey(KeyCode.T)) {
                    instructionText.SetActive(true);
                    progressSlider.gameObject.SetActive(false);
                } 
                else {
                    instructionText.SetActive(false);
                    progressSlider.gameObject.SetActive(true);
                    
                    progressSlider.value += openSpeed * Time.deltaTime;

                    // Logic xuất hiện Skill Check ngẫu nhiên giống máy phát điện
                    skillTimer -= Time.deltaTime;
                    if (skillTimer <= 0) {
                        TriggerSkillCheck();
                        skillTimer = Random.Range(4f, 8f); // Khoảng cách giữa các lần skill check
                    }
                    
                    if (progressSlider.value >= 1f) {
                        OpenGate();
                    }
                }
            }
        }
    }

    void TriggerSkillCheck() {
        if (skillCheck != null) {
            skillCheck.exitGate = this; // Truyền chính nó vào script SkillCheck mới
            // Lưu ý: Bạn cần kiểm tra xem script SkillCheck có biến "exitGate" không 
            // Nếu không, bạn có thể truyền tạm vào biến "generator" nếu script SkillCheck cho phép
            skillCheck.gameObject.SetActive(true);
        }
    }

    void OpenGate() {
        isOpened = true;
        gateMesh.SetActive(false);
        progressSlider.gameObject.SetActive(false);
        instructionText.SetActive(false);
        if(skillCheck != null) skillCheck.gameObject.SetActive(false);
    }

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isOpened) {
            isPlayerInside = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        isPlayerInside = false;
        instructionText.SetActive(false);
        progressSlider.gameObject.SetActive(false);
        if(skillCheck != null) skillCheck.gameObject.SetActive(false);
    }
}