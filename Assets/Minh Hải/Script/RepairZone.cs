using UnityEngine;

public class RepairZone : MonoBehaviour
{
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