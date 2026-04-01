using UnityEngine;
using UnityEngine.UI;

public class ExitGateLogic : MonoBehaviour
{
    [Header("UI References")]
    public Slider progressSlider;    
    // --- ĐỔI TỪ TEXT SANG IMAGE (Sử dụng GameObject để linh hoạt) ---
    public GameObject instructionImage; 
    public SkillCheckExitGate skillCheck;

    [Header("Gate Settings")]
    public GameObject gateMesh;       
    public float timeToOpen = 20f; 

    [Header("Aura & Visuals")]
    public GameObject gateAuraEffect; 

    [Header("Audio")]
    public AudioSource gateOpenSound; 

    private bool isPlayerInside = false;
    private bool isOpened = false;
    private bool hasReachedGate = false; 
    private float skillTimer; 

    void Start() {
        if (progressSlider != null) {
            progressSlider.value = 0;
            progressSlider.gameObject.SetActive(false);
        }
        // --- CẬP NHẬT TRẠNG THÁI BAN ĐẦU ---
        if (instructionImage != null) instructionImage.SetActive(false);
        if (gateAuraEffect != null) gateAuraEffect.SetActive(false);
        
        ResetSkillTimer();
    }

    void Update() {
        if (isOpened) return;

        // Quản lý Aura
        if (GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
            if (!hasReachedGate && !isPlayerInside) {
                if (gateAuraEffect != null && !gateAuraEffect.activeSelf) gateAuraEffect.SetActive(true);
            } else {
                if (gateAuraEffect != null && gateAuraEffect.activeSelf) gateAuraEffect.SetActive(false);
            }
        }

        // Logic Skill Check đang chạy thì đóng băng tiến trình
        if (skillCheck != null && skillCheck.gameObject.activeSelf && skillCheck.exitGate == this) {
            // --- ẨN IMAGE KHI ĐANG LÀM SKILL CHECK ---
            if(instructionImage != null && instructionImage.activeSelf) instructionImage.SetActive(false);
            return;
        }

        if (isPlayerInside && GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
            if (Input.GetKey(KeyCode.T)) {
                // --- ẨN IMAGE KHI ĐANG NHẤN GIỮ T ---
                if(instructionImage != null) instructionImage.SetActive(false);

                if (progressSlider != null) {
                    progressSlider.gameObject.SetActive(true);
                    progressSlider.value += (1f / timeToOpen) * Time.deltaTime;
                }

                skillTimer -= Time.deltaTime;
                if (skillTimer <= 0) TriggerSkillCheck();
                
                if (progressSlider != null && progressSlider.value >= 1f) OpenGate();
            } 
            else {
                StopInteracting();
            }
        }
    }

    public void StopInteracting() {
        if (!isOpened) {
            // --- HIỆN LẠI IMAGE KHI NGỪNG NHẤN T (Nếu vẫn ở trong vùng) ---
            if (instructionImage != null) instructionImage.SetActive(isPlayerInside);
            if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        }
    }

    void OpenGate() {
        isOpened = true;
        if (gateMesh != null) gateMesh.SetActive(false);
        if (progressSlider != null) progressSlider.gameObject.SetActive(false);
        // --- ẨN HOÀN TOÀN KHI CỔNG MỞ ---
        if (instructionImage != null) instructionImage.SetActive(false);
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

    void ResetSkillTimer() => skillTimer = Random.Range(4f, 8f);

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isOpened) {
            isPlayerInside = true;
            hasReachedGate = true;
            // --- HIỆN IMAGE KHI BƯỚC VÀO VÙNG ---
            if (instructionImage != null && !Input.GetKey(KeyCode.T)) instructionImage.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            isPlayerInside = false;
            StopInteracting();
        }
    }
}