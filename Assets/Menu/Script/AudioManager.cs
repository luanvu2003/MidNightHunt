using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("== NHẠC NỀN (BGM) ==")]
    public AudioSource musicSource;

    // Biến lưu giữ âm lượng VFX xuyên suốt lúc bật game
    // Thoát game mở lại nó sẽ tự reset về 1 (100%)
    public float vfxVolume = 1f;

    private void Awake()
    {
        // Bùa chú giúp nó sống sót qua mọi Scene
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // Hàm để UI Slider gọi vào chỉnh nhạc nền
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null)
        {
            musicSource.volume = volume;
        }
    }

    // Hàm để UI Slider gọi vào chỉnh tiếng Character (VFX/SFX)
    public void SetVFXVolume(float volume)
    {
        vfxVolume = volume;
        
        // Cập nhật ngay lập tức cho các AudioSource đang có trong Scene (nếu cần)
        AudioSource[] allAudioSources = FindObjectsOfType<AudioSource>();
        foreach (AudioSource audio in allAudioSources)
        {
            // Bỏ qua nhạc nền, chỉ chỉnh các âm thanh khác
            if (audio != musicSource)
            {
                audio.volume = vfxVolume;
            }
        }
    }
}