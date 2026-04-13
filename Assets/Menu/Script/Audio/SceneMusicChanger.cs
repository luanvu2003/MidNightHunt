using UnityEngine;

public class SceneMusicChanger : MonoBehaviour
{
    [Header("== NHẠC NỀN RIÊNG CHO MAP (SCENE 4) ==")]
    public AudioClip mapBGM; // Kéo bài nhạc của Scene 4 vào đây

    void Start()
    {
        // Kiểm tra xem AudioManager từ Scene 0 có tồn tại không
        if (AudioManager.Instance != null && mapBGM != null)
        {
            AudioSource globalSource = AudioManager.Instance.musicSource;

            // 1. Nếu bài nhạc đang phát khác với bài nhạc của Map thì mới đổi
            if (globalSource.clip != mapBGM)
            {
                globalSource.clip = mapBGM;
                globalSource.Play();
            }

            // 2. 🚨 ĐỒNG BỘ QUAN TRỌNG: Ép âm lượng loa theo đúng mức đã lưu
            // Dòng này giúp nhạc ở Scene 4 ngay lập tức bé đi 50% nếu Menu đã chỉnh vậy
            globalSource.volume = AudioManager.Instance.musicSource.volume;
        }
    }
}