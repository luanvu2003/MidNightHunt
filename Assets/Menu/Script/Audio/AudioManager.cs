using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;
    [Header("== NHẠC NỀN (BGM) ==")]
    public AudioSource musicSource;
    public float vfxVolume = 1f;
    private void Awake()
    {
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
    public void SetMusicVolume(float volume)
    {
        if (musicSource != null) musicSource.volume = volume;
    }
    public void SetVFXVolume(float volume)
    {
        vfxVolume = volume;
    }
}