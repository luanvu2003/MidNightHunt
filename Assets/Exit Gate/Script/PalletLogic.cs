using UnityEngine;
using System.Collections;

public class PalletController : MonoBehaviour
{
    [Header("Settings")]
    public float dropSpeed = 0.2f;
    public bool isDropped = false;
    private bool canInteract = false;

    [Header("Stun Settings")]
    public BoxCollider stunZoneCollider; // Kéo Object "Stun Zone" vào đây
    public LayerMask killerLayer;        // Chọn Layer "Killer"
    public float stunDuration = 3f;      // Thời gian choáng có thể chỉnh public

    [Header("References")]
    public AudioSource dropSound;
    public GameObject instructionText;
    private UnityEngine.AI.NavMeshObstacle navObstacle;

    void Start()
    {
        navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (navObstacle != null) navObstacle.carving = false;
        
        // Mặc định tắt cái Collider của vùng Stun đi, chỉ bật khi ván đổ
        if (stunZoneCollider != null) stunZoneCollider.enabled = false;
    }

    void Update()
    {
        if (isDropped) return;

        if (canInteract && Input.GetKeyDown(KeyCode.Space))
        {
            StartCoroutine(DropRoutine());
        }
    }

    IEnumerator DropRoutine()
    {
        isDropped = true;
        if (instructionText != null) instructionText.SetActive(false);
        if (dropSound != null) dropSound.Play();

        // Xoay ván
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(90, transform.localEulerAngles.y, transform.localEulerAngles.z);
        
        float elapsed = 0;
        while (elapsed < dropSpeed)
        {
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / dropSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localRotation = targetRotation;

        if (navObstacle != null) navObstacle.carving = true;

        // KÍCH HOẠT KIỂM TRA STUN
        CheckForStun();
    }

    void CheckForStun()
    {
        if (stunZoneCollider == null) return;

        // Lấy thông số từ Box Collider bạn đã đặt trong Inspector
        Vector3 center = stunZoneCollider.transform.position;
        Vector3 halfExtents = stunZoneCollider.size / 2;
        // Nhân với lossyScale để kích thước chuẩn nếu Object cha bị scale
        halfExtents.x *= stunZoneCollider.transform.lossyScale.x;
        halfExtents.y *= stunZoneCollider.transform.lossyScale.y;
        halfExtents.z *= stunZoneCollider.transform.lossyScale.z;

        // Quét tất cả Killer nằm trong vùng Box đó
        Collider[] hitKillers = Physics.OverlapBox(center, halfExtents, stunZoneCollider.transform.rotation, killerLayer);

        // foreach (Collider killer in hitKillers)
        // {
        //     // Tìm script Killer và gọi hàm Stun
        //     // Hãy đảm bảo script Killer của bạn có hàm public GetStunned
        //     var killerScript = killer.GetComponent<KillerAI>(); 
        //     if (killerScript != null)
        //     {
        //         killerScript.GetStunned(stunDuration);
        //         Debug.Log("<color=yellow>Pallet:</color> Killer bị dính ván và Choáng!");
        //     }
        // }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (!isDropped && instructionText != null) instructionText.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            if (instructionText != null) instructionText.SetActive(false);
        }
    }
}