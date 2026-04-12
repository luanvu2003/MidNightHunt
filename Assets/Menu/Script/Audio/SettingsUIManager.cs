using UnityEngine;
using UnityEngine.UI;

public class SettingsUIManager : MonoBehaviour
{
    [Header("== UI PANELS ==")]
    public GameObject settingsPanel; 

    [Header("== SLIDERS ==")]
    public Slider musicSlider; 
    public Slider vfxSlider;   

    private bool wasMouseLocked;

    private void Start()
    {
        // Luôn tắt Panel khi mới vào Scene
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 🚨 AUTO SYNC: Kiểm tra xem AudioManager từ Scene 1 có đang tồn tại không
        if (AudioManager.Instance != null)
        {
            // 1. Cập nhật vị trí thanh trượt cho khớp với âm lượng đang lưu
            musicSlider.value = AudioManager.Instance.musicSource.volume;
            vfxSlider.value = AudioManager.Instance.vfxVolume;

            // 2. Lắng nghe khi người chơi kéo Slider
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            vfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetVFXVolume);
        }
        else
        {
            Debug.LogWarning("[SettingsUI] Không tìm thấy AudioManager! (Chỉ báo lỗi nếu bạn chạy test thẳng từ Scene này mà không qua Lobby)");
        }
    }

    private void Update()
    {
        // Nhấn nút ESC để bật/tắt Settings
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ToggleSettingsPanel();
        }
    }

    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;

        bool isOpening = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpening);

        if (isOpening)
        {
            // KHI MỞ: Lưu trạng thái chuột hiện tại và hiện chuột ra
            wasMouseLocked = (Cursor.lockState == CursorLockMode.Locked); 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // KHI TẮT: Trả lại trạng thái chuột như cũ
            if (wasMouseLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}