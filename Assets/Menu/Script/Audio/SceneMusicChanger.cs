using UnityEngine;

public class SceneMusicChanger : MonoBehaviour
{
    [Header("== NHẠC NỀN RIÊNG CHO MAP (SCENE 4) ==")]
    public AudioClip mapBGM; 
    void Start()
    {
        if (AudioManager.Instance != null && mapBGM != null)
        {
            AudioSource globalSource = AudioManager.Instance.musicSource;
            if (globalSource.clip != mapBGM)
            {
                globalSource.clip = mapBGM;
                globalSource.Play();
            }
            globalSource.volume = AudioManager.Instance.musicSource.volume;
        }
    }
}