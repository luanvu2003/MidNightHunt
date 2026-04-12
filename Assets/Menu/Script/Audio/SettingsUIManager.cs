using UnityEngine;
using UnityEngine.UI;

public class SettingsUIManager : MonoBehaviour
{
    // 🚨 BIẾN ĐÈN GIAO THÔNG BÁO CHO TOÀN BỘ GAME BIẾT SETTING CÓ ĐANG MỞ KHÔNG
    public static bool IsOpen = false; 

    [Header("== UI PANELS ==")]
    public GameObject settingsPanel; 

    [Header("== SLIDERS ==")]
    public Slider musicSlider; 
    public Slider vfxSlider;   

    private bool wasMouseLocked;

    private void Start()
    {
        if (settingsPanel != null) settingsPanel.SetActive(false);
        IsOpen = false; // Reset trạng thái khi load Scene mới

        if (AudioManager.Instance != null)
        {
            musicSlider.value = AudioManager.Instance.musicSource.volume;
            vfxSlider.value = AudioManager.Instance.vfxVolume;

            musicSlider.onValueChanged.AddListener(AudioManager.Instance.SetMusicVolume);
            vfxSlider.onValueChanged.AddListener(AudioManager.Instance.SetVFXVolume);
        }
    }

    private void Update()
    {
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
        
        // 🚨 CHỐT TRẠNG THÁI MỞ/TẮT
        IsOpen = isOpening; 

        if (isOpening)
        {
            wasMouseLocked = (Cursor.lockState == CursorLockMode.Locked); 
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }
        else
        {
            if (wasMouseLocked)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }
    }
}