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
        if (instructionText != null) instructionText.SetActive(false);
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
            if(instructionText.activeSelf) instructionText.SetActive(false);
            return;
        }

        if (isPlayerInside && GameManager.Instance != null && GameManager.Instance.CanOpenExitGate()) {
            if (Input.GetKey(KeyCode.T)) {
                instructionText.SetActive(false);
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

    // Hàm này giúp dọn dẹp trạng thái khi ngừng nhấn T hoặc trượt Skill Check
    public void StopInteracting() {
        if (!isOpened) {
            if (instructionText != null) instructionText.SetActive(isPlayerInside);
            if (progressSlider != null) progressSlider.gameObject.SetActive(false);
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
            skillCheck.exitGate = this; // "Khai báo" chủ sở hữu cho Skill Check
            skillCheck.gameObject.SetActive(true); 
            ResetSkillTimer();
        }
    }

    void ResetSkillTimer() => skillTimer = Random.Range(4f, 8f); // Tăng thời gian chờ cho đỡ dồn dập

    private void OnTriggerEnter(Collider other) {
        if (other.CompareTag("Player") && !isOpened) {
            isPlayerInside = true;
            hasReachedGate = true;
        }
    }

    private void OnTriggerExit(Collider other) {
        if (other.CompareTag("Player")) {
            isPlayerInside = false;
            StopInteracting();
        }
    }
}