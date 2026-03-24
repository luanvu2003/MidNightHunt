using UnityEngine;

public class BearTrap : MonoBehaviour
{
    [Header("Cài đặt Bẫy")]
    public float trapDuration = 3f; // Khóa chân mấy giây
    public AudioClip snapSound; 
    private AudioSource audioSource;
    private bool isTriggered = false;

    private void Awake()
    {
        audioSource = GetComponent<AudioSource>();
        // Ép bẫy rớt xuống đất cho chuẩn (tránh lơ lửng)
        RaycastHit hit;
        if (Physics.Raycast(transform.position + Vector3.up, Vector3.down, out hit, 2f))
        {
            transform.position = hit.point;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isTriggered && other.CompareTag("Player"))
        {
            TriggerTrap(other.gameObject);
        }
    }

    private void TriggerTrap(GameObject victim)
    {
        isTriggered = true;
        if (snapSound != null && audioSource != null) audioSource.PlayOneShot(snapSound);

        Debug.Log("Player đã dính bẫy!");
        // TODO: Ép tốc độ nạn nhân về 0 (Khóa chân)
        // TODO: Trừ máu nạn nhân

        // =======================================================
        // HOÀN TRẢ LẠI BẪY CHO HUNTER
        // =======================================================
        AttackController hunterAtk = FindObjectOfType<AttackController>();
        if (hunterAtk != null)
        {
            hunterAtk.AddAmmo(1); // Trả lại 1 bẫy
        }

        // Tự hủy bẫy sau 0.5s (hoặc chờ nạn nhân thoát ra tùy bạn)
        Destroy(gameObject, 0.5f);
    }
}