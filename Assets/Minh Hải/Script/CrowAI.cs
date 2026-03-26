using UnityEngine;
using System.Collections;

public class CrowAI : MonoBehaviour
{
    [Header("Cài đặt khoảng cách")]
    public float detectionRadius = 5f; 
    
    [Header("Hoán đổi Model")]
    public GameObject idleModel; // Kéo Model đứng im vào đây
    public GameObject flyModel;  // Kéo Model bay vào đây
    public Animator flyAnimator; // Animator của con quạ bay

    [Header("Âm thanh")]
    public AudioSource cawSound;

    [Header("Cài đặt bay")]
    public float flyUpSpeed = 15f; 

    private bool hasFled = false;

    void Start()
    {
        // Đảm bảo lúc đầu chỉ có con đứng im hiện lên
        if (idleModel != null) idleModel.SetActive(true);
        if (flyModel != null) flyModel.SetActive(false);
    }

    void Update()
    {
        if (hasFled) return;

        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var t in targets)
        {
            if (t.CompareTag("Player"))
            {
                PlayerMovement player = t.GetComponent<PlayerMovement>();
                if (player != null && player.isSprinting)
                {
                    TriggerFlee();
                    break;
                }
            }
        }
    }

    void TriggerFlee()
    {
        hasFled = true;

        // 1. Hoán đổi Model
        if (idleModel != null) idleModel.SetActive(false); // Tắt con đứng im
        if (flyModel != null) 
        {
            flyModel.SetActive(true); // Bật con đang bay
            if (flyAnimator != null) flyAnimator.Play("Fly"); // Chạy thẳng Anim tên là "Fly"
        }

        // 2. Âm thanh
        if (cawSound != null) cawSound.Play();

        // 3. Bay đi
        StartCoroutine(FlyUpRoutine());

        // Tắt va chạm của bẫy
        if(GetComponent<Collider>() != null) GetComponent<Collider>().enabled = false;
    }

    IEnumerator FlyUpRoutine()
    {
        float timer = 0f;
        float duration = 3f;

        // --- BƯỚC 1: TẠO HƯỚNG BAY NGẪU NHIÊN CHO TỪNG CON ---
        // Lấy một điểm ngẫu nhiên trên vòng tròn nằm ngang (XZ)
        Vector3 randomHorizontal = Random.insideUnitCircle; 
        Vector3 horizontalDir = new Vector3(randomHorizontal.x, 0, randomHorizontal.y);

        // Kết hợp với hướng đi lên (Vector3.up)
        // Nhân horizontalDir với 1.5f để quạ bay chéo nhiều hơn là bay thẳng đứng
        Vector3 flyDirection = (horizontalDir * 1.5f + Vector3.up).normalized;

        // --- BƯỚC 2: DI CHUYỂN ---
        while (timer < duration)
        {
            // Tốc độ bay nhanh lúc đầu và chậm lại một chút (Lerp)
            float currentSpeed = Mathf.Lerp(flyUpSpeed, flyUpSpeed * 0.3f, timer / duration);

            // Di chuyển toàn bộ object (bao gồm cả model đang bay bên trong)
            transform.Translate(flyDirection * currentSpeed * Time.deltaTime, Space.World);
            
            // Xoay đầu quạ nhìn về hướng nó đang bay
            if (flyDirection != Vector3.zero)
            {
                Quaternion targetRot = Quaternion.LookRotation(flyDirection);
                transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, Time.deltaTime * 10f);
            }
            
            timer += Time.deltaTime;
            yield return null;
        }

        Destroy(gameObject);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}