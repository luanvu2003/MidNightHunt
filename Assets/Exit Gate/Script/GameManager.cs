using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;
    public int totalGenerators = 3; 
    public int activatedGenerators = 0;

    void Awake() { Instance = this; }

    // Hàm này để các máy phát điện gọi khi sửa xong
    public void GeneratorFixed() {
        activatedGenerators++;
        Debug.Log("Máy phát điện đã xong: " + activatedGenerators + "/" + totalGenerators);
    }

    public bool CanOpenExitGate() {
        return activatedGenerators >= totalGenerators;
    }
}