using UnityEngine;

public class RepairZone : MonoBehaviour
{
    // 🚨 Đổi từ Generator sang bản mạng: Generator_Fusion
    public Generator generator; 

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && generator != null)
        {
            generator.PlayerEnteredZone(other);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player") && generator != null)
        {
            generator.PlayerExitedZone(other);
        }
    }
}