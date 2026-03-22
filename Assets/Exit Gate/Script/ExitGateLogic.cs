using UnityEngine;
using UnityEngine.UI;

public class ExitGateLogic : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressSlider;    
    public GameObject instructionText; 
    public SkillCheckExitGate skillCheck;

    [Header("Gate Settings")]
    public GameObject gateMesh;       
    [Tooltip("Thời gian mở cổng tính bằng giây (ví dụ: 10)")]
    public float timeToOpen = 10f; // Nhập 10 là mở trong 10 giây

    [Header("Aura & Visuals")]
    public GameObject gateAuraEffect; 

    [Header("Audio")]
    public AudioSource gateOpenSound; 

    private bool isPlayerInside = false;
    private bool isOpened = false;
    private bool hasReachedGate = false; // Biến để khóa Aura vĩnh viễn
    private float skillTimer; 

    void Start() {
        if (progressSlider != null) {
            progressSlider.value = 0;
            progressSlider.gameObject.SetActive(false);
        }
        if (instructionText != null) instructionText.SetActive(false);
        if (gateAuraEffect != null) gateAuraEffect.SetActive(false);
        
        ResetSkillTimer();
    }

    void Update() {
        if (isOpened) return;

        // --- LOGIC ĐIỀU KHIỂN AURA ---
        if (GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
            // Chỉ hiện Aura nếu CHƯA chạm vào cổng VÀ đang ở xa
            if (!hasReachedGate && !isPlayerInside) {
                if (gateAuraEffect != null && !gateAuraEffect.activeSelf) 
                    gateAuraEffect.SetActive(true);
            } 
            else {
                // Tắt vĩnh viễn khi đã chạm hoặc đứng gần
                if (gateAuraEffect != null && gateAuraEffect.activeSelf) 
                    gateAuraEffect.SetActive(false);
            }
        }

        // --- LOGIC MỞ CỔNG ---
        // Nếu Skill Check đang hiện thì không chạy tiếp thanh slider
        if (skillCheck != null && skillCheck.gameObject.activeSelf) {
             // Ẩn instructionText để không bị đè lên Skill Check
             if(instructionText.activeSelf) instructionText.SetActive(false);
             return;
        }

        if (isPlayerInside && GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
            if (Input.GetKey(KeyCode.T)) {
                instructionText.SetActive(false);
                
                // Bật Slider khi nhấn T
                if (progressSlider != null && !progressSlider.gameObject.activeSelf)
                    progressSlider.gameObject.SetActive(true);

                // Tính toán tiến trình dựa trên giây (1 chia cho tổng giây)
                if (progressSlider != null)
                    progressSlider.value += (1f / timeToOpen) * Time.deltaTime;

                skillTimer -= Time.deltaTime;
                if (skillTimer <= 0) TriggerSkillCheck();
                
                if (progressSlider != null && progressSlider.value >= 1f) OpenGate();
            } 
            else {
                // Hiện hướng dẫn và ẩn Slider khi buông T
                instructionText.SetActive(true);
                if (progressSlider != null) progressSlider.gameObject.SetActive(false);
            }
        }
    }

    void OpenGate() {
        isOpened = true;
        if (gateMesh != null) gateMesh.SetActive(false);
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        if (instructionText != null) instructionText.SetActive(false);
        if (gateAuraEffect != null) gateAuraEffect.SetActive(false); 

        if (gateOpenSound != null) gateOpenSound.Play();
    }

    void TriggerSkillCheck() {
        if (skillCheck != null) {
            skillCheck.exitGate = this;
            skillCheck.gameObject.SetActive(true); 
            ResetSkillTimer();
        }
    }

    void ResetSkillTimer() => skillTimer = Random.Range(3f, 6f);

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isOpened) {
            isPlayerInside = true;
            hasReachedGate = true; // NGƯỜI CHƠI ĐÃ TỚI -> KHÓA AURA LUÔN
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            isPlayerInside = false;
            if (instructionText != null) instructionText.SetActive(false);
            if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        }
    }
}