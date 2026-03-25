using UnityEngine;

public class RepairZone : MonoBehaviour
{
    public Generator generator; // Kéo object Generator vào đây

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            generator.PlayerEnteredZone();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            generator.PlayerExitedZone();
        }
    }
}