using UnityEngine;
using System.Collections;

public class CrowAI : MonoBehaviour
{
    [Header("Cài đặt Tuần tra")]
    public float moveSpeed = 0.8f;   
    public float patrolRadius = 3f; 
    public float waitTime = 3f;      
    public float rotationSpeed = 8f; 

    [Header("Cài đặt Hồi sinh")]
    [Tooltip("Thời gian (giây) để quạ hiện lại tại chỗ cũ")]
    public float respawnDelay = 300f; 

    [Header("Model & Bay")]
    public GameObject idleModel; 
    public GameObject flyModel;  
    public AudioSource cawSound;
    public float flyUpSpeed = 18f; 
    public float detectionRadius = 5f;

    private Vector3 startPosition;
    private Quaternion startRotation;
    private bool hasFled = false;

    void Start()
    {
        // Lưu lại vị trí ban đầu mà RandomSpawner đã đặt để hồi sinh đúng chỗ
        startPosition = transform.position;
        startRotation = transform.rotation;

        if (idleModel != null) idleModel.SetActive(true);
        if (flyModel != null) flyModel.SetActive(false);

        StartCoroutine(PatrolRoutine());
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

    // --- HÀM QUAN TRỌNG: Generator.cs gọi hàm này ---
    public void OnGeneratorExplosion()
    {
        if (!hasFled) 
        {
            // Tránh quạ bay đi đồng loạt quá thô, tạo độ trễ ngẫu nhiên
            Invoke("TriggerFlee", Random.Range(0.1f, 0.4f));
        }
    }

    IEnumerator PatrolRoutine()
    {
        while (!hasFled)
        {
            yield return new WaitForSeconds(Random.Range(waitTime * 0.5f, waitTime * 1.5f));
            if (hasFled) yield break;

            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 targetPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            Vector3 direction = (targetPoint - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                while (Quaternion.Angle(transform.rotation, targetRotation) > 5f && !hasFled)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * rotationSpeed);
                    yield return null;
                }
            }

            float startTime = Time.time;
            while (Vector3.Distance(transform.position, targetPoint) > 0.1f && !hasFled)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);
                if (Time.time - startTime > 5f) break; 
                yield return null;
            }
        }
    }

    public void TriggerFlee()
    {
        if (hasFled) return;
        hasFled = true;
        StopAllCoroutines();

        if (idleModel != null) idleModel.SetActive(false);
        if (flyModel != null) 
        {
            flyModel.SetActive(true);
            Animator anim = flyModel.GetComponentInChildren<Animator>();
            if (anim != null) { anim.Play("CrowFly", 0, 0f); anim.speed = 1.5f; }
        }

        if (cawSound != null) cawSound.Play();
        StartCoroutine(FlyAndRespawnRoutine());
    }

    IEnumerator FlyAndRespawnRoutine()
    {
        float timer = 0f;
        Vector3 randomDir = (new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * 1.5f + Vector3.up).normalized;
        
        // Giai đoạn 1: Bay vút lên trời
        while (timer < 2.5f)
        {
            float currentSpeed = Mathf.Lerp(flyUpSpeed, flyUpSpeed * 0.4f, timer / 2.5f);
            transform.Translate(randomDir * currentSpeed * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(randomDir), Time.deltaTime * 10f);
            timer += Time.deltaTime;
            yield return null;
        }

        // Giai đoạn 2: Ẩn model bay (quạ biến mất)
        if (flyModel != null) flyModel.SetActive(false);

        // Giai đoạn 3: Đợi hồi sinh (5 phút)
        yield return new WaitForSeconds(respawnDelay);

        // Giai đoạn 4: Đưa quạ về vị trí cũ và hiện lại
        transform.position = startPosition;
        transform.rotation = startRotation;
        hasFled = false;

        if (idleModel != null) idleModel.SetActive(true);
        StartCoroutine(PatrolRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasFled && other.CompareTag("Player")) TriggerFlee();
    }
}