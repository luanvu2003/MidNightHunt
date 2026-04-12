using UnityEngine;
using UnityEngine.UI;

public class SettingsUIManager : MonoBehaviour
{
    [Header("== UI PANELS ==")]
    public GameObject settingsPanel; // Kéo GameObject chứa nguyên cái bảng OptionPanel/IconST vào đây

    [Header("== SLIDERS ==")]
    public Slider musicSlider; // Kéo Slider Music vào đây
    public Slider vfxSlider;   // Kéo Slider VFX vào đây

    // Lưu lại trạng thái chuột trước khi mở Settings để tắt đi trả lại như cũ
    private bool wasMouseLocked;

    private void Start()
    {
        // 1. Tắt panel lúc mới vào game
        if (settingsPanel != null) settingsPanel.SetActive(false);

        // 2. Đồng bộ giá trị Slider với AudioManager (nếu có)
        if (AudioManager.Instance != null)
        {
            musicSlider.value = AudioManager.Instance.musicSource.volume;
            vfxSlider.value = AudioManager.Instance.vfxVolume;

            // Lắng nghe khi bạn kéo thanh trượt
            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            vfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetVFXVolume);
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

    // Nút X (Close) hoặc Nút Icon Cài đặt cũng sẽ gọi hàm này
    public void ToggleSettingsPanel()
    {
        if (settingsPanel == null) return;

        bool isOpening = !settingsPanel.activeSelf;
        settingsPanel.SetActive(isOpening);

        if (isOpening)
        {
            // KHI MỞ SETTINGS: Bắt buộc hiện chuột ra để kéo Slider
            wasMouseLocked = (Cursor.lockState == CursorLockMode.Locked); // Nhớ xem trước đó chuột đang khóa hay thả
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            // KHI TẮT SETTINGS: Nếu trước đó chuột bị khóa (đang chơi game), thì khóa lại.
            // (Không cần nhấn Alt nữa)
            if (wasMouseLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}