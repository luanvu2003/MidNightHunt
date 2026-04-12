using UnityEngine;

public class AudioManager : MonoBehaviour
{
    // Biến Singleton tĩnh để các Scene sau dễ dàng gọi nó mà không cần dùng GameObject.Find (tránh bug sai tên)
    public static AudioManager Instance;

    [Header("== NHẠC NỀN (BGM) ==")]
    public AudioSource musicSource;

    // Biến lưu âm lượng VFX. Không dùng PlayerPrefs nên thoát game mở lại sẽ tự reset.
    public float vfxVolume = 1f;

    private void Awake()
    {
        // Setup Singleton và bùa bất tử
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // Giữ nhạc không bị ngắt khi đổi Scene
        }
        else
        {
            // Nếu qua Scene mới mà thấy đã có AudioManager rồi thì tự hủy thằng mới sinh ra
            Destroy(gameObject);
        }
    }

    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }

    public void SetVFXVolume(float volume)
    {
        vfxVolume = volume;
    }
}