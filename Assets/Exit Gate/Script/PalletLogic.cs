using UnityEngine;
using System.Collections;
using UnityEngine.UI; // Thêm thư viện này để điều khiển Image

public class PalletController : MonoBehaviour
{
    [Header("Settings")]
    public float dropSpeed = 0.2f;
    public bool isDropped = false;
    private bool canInteract = false;

    [Header("Stun Settings")]
    public BoxCollider stunZoneCollider; 
    public LayerMask killerLayer;        
    public float stunDuration = 3f;      

    [Header("References")]
    public AudioSource dropSound;
    
    // Đổi GameObject thành Image (Hoặc giữ GameObject nếu bạn muốn bật/tắt nguyên cụm)
    public GameObject instructionImage; 
    
    private UnityEngine.AI.NavMeshObstacle navObstacle;

    void Start()
    {
        navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (navObstacle != null) navObstacle.carving = false;
        
        if (stunZoneCollider != null) stunZoneCollider.enabled = false;

        // Mặc định ẩn hình ảnh hướng dẫn
        if (instructionImage != null) instructionImage.SetActive(false);
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
        if (instructionImage != null) instructionImage.SetActive(false);
        if (dropSound != null) dropSound.Play();

        // 1. KIỂM TRA HUNTER TRƯỚC KHI ĐỔ (Để đẩy lùi)
        PushHunterBack();

        // 2. XOAY VÁN MƯỢT MÀ
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

        // 3. GÂY CHOÁNG (Stun)
        CheckForStun();
    }

    void PushHunterBack()
    {
        // 1. Tính toán vùng quét dựa trên StunZone (Khung xanh)
        Vector3 center = stunZoneCollider.transform.position;
        // Scale vùng quét to hơn một chút để Hunter không kịp chạm vào ván
        Vector3 halfExtents = Vector3.Scale(stunZoneCollider.size / 1.8f, stunZoneCollider.transform.lossyScale);
        
        // 2. Chỉ quét những gì thuộc Layer Killer
        Collider[] hitKillers = Physics.OverlapBox(center, halfExtents, stunZoneCollider.transform.rotation, killerLayer);

        foreach (Collider killer in hitKillers)
        {
            // Tính hướng từ Pivot ván đến Hunter
            Vector3 pushDir = (killer.transform.position - transform.position).normalized;
            pushDir.y = 0; // Chỉ đẩy ngang

            // 3. XỬ LÝ ĐẨY LÙI QUYẾT LIỆT
            // Nếu Hunter có Rigidbody, chúng ta phải "dịch" nó đi thông qua MovePosition hoặc ép Position trực tiếp
            Rigidbody rb = killer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Dịch chuyển tức thời ra xa 1.8 đơn vị
                Vector3 targetPos = killer.transform.position + pushDir * 1.8f;
                rb.position = targetPos; // Ép vị trí vật lý
            }
            else
            {
                killer.transform.position += pushDir * 1.8f;
            }

            Debug.Log("<color=orange>DBD Logic:</color> Đã ép Hunter lùi lại!");
        }
    }
    // IEnumerator DropRoutine()
    // {
    //     isDropped = true;
        
    //     // Ẩn Image khi bắt đầu đổ ván
    //     if (instructionImage != null) instructionImage.SetActive(false);
        
    //     if (dropSound != null) dropSound.Play();

    //     Quaternion startRotation = transform.localRotation;
    //     Quaternion targetRotation = Quaternion.Euler(90, transform.localEulerAngles.y, transform.localEulerAngles.z);
        
    //     float elapsed = 0;
    //     while (elapsed < dropSpeed)
    //     {
    //         transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / dropSpeed);
    //         elapsed += Time.deltaTime;
    //         yield return null;
    //     }
    //     transform.localRotation = targetRotation;

    //     if (navObstacle != null) navObstacle.carving = true;

    //     CheckForStun();
    // }

    void CheckForStun()
    {
        if (stunZoneCollider == null) return;

        Vector3 center = stunZoneCollider.transform.position;
        Vector3 halfExtents = stunZoneCollider.size / 2;
        
        halfExtents.x *= stunZoneCollider.transform.lossyScale.x;
        halfExtents.y *= stunZoneCollider.transform.lossyScale.y;
        halfExtents.z *= stunZoneCollider.transform.lossyScale.z;

        Collider[] hitKillers = Physics.OverlapBox(center, halfExtents, stunZoneCollider.transform.rotation, killerLayer);

        
        foreach (Collider killer in hitKillers)
        {
            var killerScript = killer.GetComponent<HunterTest>(); 
            if (killerScript != null)
            {
                killerScript.GetStunned(stunDuration);
                Debug.Log("<color=yellow>Pallet:</color> Killer bị dính ván và Choáng!");
            }
        }
        
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            // Hiện Image khi đến gần
            if (!isDropped && instructionImage != null) instructionImage.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            // Ẩn Image khi đi xa
            if (instructionImage != null) instructionImage.SetActive(false);
        }
    }
}