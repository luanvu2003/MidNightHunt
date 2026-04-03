using UnityEngine;
using System.Collections;
using UnityEngine.UI;

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
    public GameObject instructionImage; 
    
    private UnityEngine.AI.NavMeshObstacle navObstacle;

    [Header("Advanced Collision Prediction")]
    public float defaultTargetAngle = 90f;

    void Start()
    {
        navObstacle = GetComponent<UnityEngine.AI.NavMeshObstacle>();
        if (navObstacle != null) navObstacle.carving = false;
        if (stunZoneCollider != null) stunZoneCollider.enabled = false;
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

        // 1. ĐẨY HUNTER (Logic DBD)
        PushHunterBack(); 

        // 2. --- LOGIC QUÉT ĐA ĐIỂM "KHÍT RỊT" ---
        float finalTargetAngle = defaultTargetAngle; 
        RaycastHit hit;
        bool hasHit = false;
        float bestAngle = defaultTargetAngle;

        // Quét 3 điểm dọc theo chiều dài ván để không hụt Cube (Gốc - Giữa - Ngọn)
        Vector3[] scanPoints = new Vector3[] {
            stunZoneCollider.transform.position - transform.forward * 0.5f + Vector3.up * 2.0f,
            stunZoneCollider.transform.position + Vector3.up * 2.0f,
            stunZoneCollider.transform.position + transform.forward * 1.0f + Vector3.up * 2.0f 
        };

        foreach (Vector3 origin in scanPoints)
        {
            // Quét Layer "Cube"
            if (Physics.BoxCast(origin, new Vector3(0.5f, 0.1f, 0.1f), Vector3.down, out hit, transform.rotation, 3.0f, LayerMask.GetMask("Cube")))
            {
                hasHit = true;
                
                // Tính khoảng cách nằm ngang từ Pivot đến điểm va chạm
                float distanceToPivot = Vector3.Distance(new Vector3(transform.position.x, 0, transform.position.z), 
                                                         new Vector3(hit.point.x, 0, hit.point.z));
                
                // Tính độ cao của vật cản so với chân ván
                float obstacleHeight = hit.point.y - transform.position.y;

                // Công thức lượng giác: Góc nghiêng = Atan(Đối / Kề)
                float angleRad = Mathf.Atan2(obstacleHeight, distanceToPivot);
                float angleDeg = angleRad * Mathf.Rad2Deg;

                // Tính góc xoay mục tiêu (90 độ là nằm bệt, trừ đi góc nghiêng để ván dựng lên)
                float calculatedAngle = 90f - angleDeg;

                // Lấy góc nhỏ nhất (để ván đứng cao nhất, không xuyên qua vật cao nhất)
                if (calculatedAngle < bestAngle) bestAngle = calculatedAngle;
            }
        }

        if (hasHit)
        {
            // Cộng thêm 2 độ để "ép" ván chạm hẳn vào Collider của Cube, xóa khoảng trống
            finalTargetAngle = Mathf.Clamp(bestAngle + 2f, 15f, 85f);
            Debug.Log($"<color=lime>Phát hiện Cube!</color> Góc ép sát: {finalTargetAngle}");
        }

        // 3. THỰC HIỆN XOAY MƯỢT MÀ
        Quaternion startRotation = transform.localRotation;
        Quaternion targetRotation = Quaternion.Euler(finalTargetAngle, transform.localEulerAngles.y, transform.localEulerAngles.z);
        
        float elapsed = 0;
        while (elapsed < dropSpeed)
        {
            transform.localRotation = Quaternion.Slerp(startRotation, targetRotation, elapsed / dropSpeed);
            elapsed += Time.deltaTime;
            yield return null;
        }
        transform.localRotation = targetRotation;

        if (navObstacle != null) navObstacle.carving = true;
        CheckForStun();
    }

    void PushHunterBack()
    {
        Vector3 center = stunZoneCollider.transform.position;
        Vector3 halfExtents = Vector3.Scale(stunZoneCollider.size / 1.8f, stunZoneCollider.transform.lossyScale);
        Collider[] hitKillers = Physics.OverlapBox(center, halfExtents, stunZoneCollider.transform.rotation, killerLayer);

        foreach (Collider killer in hitKillers)
        {
            Vector3 pushDir = (killer.transform.position - transform.position).normalized;
            pushDir.y = 0; 

            Rigidbody rb = killer.GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.position = killer.transform.position + pushDir * 1.8f;
            }
            else
            {
                killer.transform.position += pushDir * 1.8f;
            }
            Debug.Log("<color=orange>DBD Logic:</color> Đã ép Hunter lùi lại!");
        }
    }

    void CheckForStun()
    {
        if (stunZoneCollider == null) return;
        Vector3 center = stunZoneCollider.transform.position;
        Vector3 halfExtents = Vector3.Scale(stunZoneCollider.size / 2f, stunZoneCollider.transform.lossyScale);

        Collider[] hitKillers = Physics.OverlapBox(center, halfExtents, stunZoneCollider.transform.rotation, killerLayer);
        
        foreach (Collider killer in hitKillers)
        {
            var killerScript = killer.GetComponent<HunterTest>(); 
            if (killerScript != null)
            {
                killerScript.GetStunned(stunDuration);
                Debug.Log("<color=yellow>Pallet:</color> Killer bị dính ván!");
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = true;
            if (!isDropped && instructionImage != null) instructionImage.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            canInteract = false;
            if (instructionImage != null) instructionImage.SetActive(false);
        }
    }
}