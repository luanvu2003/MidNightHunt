using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int totalGenerators = 3; 
    private int generatorsFixed = 0;
    
    private bool hasPlayedSuccessSound = false; // Biến để khóa âm thanh

    [Header("Audio")]
    public AudioSource allFixedSound;

    void Awake() {
        if (Instance == null) Instance = this;
    }

    public void GeneratorFixed() {
        generatorsFixed++;
        
        // Kiểm tra nếu đủ máy VÀ âm thanh chưa từng phát
        if (generatorsFixed >= totalGenerators && !hasPlayedSuccessSound) {
            if (allFixedSound != null) {
                allFixedSound.Play();
                hasPlayedSuccessSound = true; // Khóa lại ngay sau khi phát
            }
            Debug.Log("Đã đủ máy! Âm thanh thông báo chỉ phát 1 lần.");
        }
    }

    public bool CanOpenExitGate() => generatorsFixed >= totalGenerators;
}