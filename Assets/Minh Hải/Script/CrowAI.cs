using UnityEngine;
using System.Collections;

public class CrowAI : MonoBehaviour
{
    [Header("Cài đặt Tuần tra (Vừa đi vừa quay đầu)")]
    public float moveSpeed = 0.8f;   
    public float patrolRadius = 3f; 
    public float waitTime = 3f;      
    public float rotationSpeed = 8f; // Tốc độ quay đầu

    private Vector3 targetPoint;
    private Vector3 startPosition;
    private bool isMoving = false;

    [Header("Model & Bay")]
    public GameObject idleModel; 
    public GameObject flyModel;  
    public AudioSource cawSound;
    public float flyUpSpeed = 18f; 
    public float detectionRadius = 5f;

    private bool hasFled = false;

    void Start()
    {
        startPosition = transform.position;
        if (idleModel != null) idleModel.SetActive(true);
        if (flyModel != null) flyModel.SetActive(false);

        StartCoroutine(PatrolRoutine());
    }

    void Update()
    {
        if (hasFled) return;

        // Quét Player đang chạy (Sprinting)
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

    IEnumerator PatrolRoutine()
    {
        while (!hasFled)
        {
            // Nghỉ ngơi tại chỗ
            yield return new WaitForSeconds(Random.Range(waitTime * 0.5f, waitTime * 1.5f));

            // Chọn điểm mới
            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            targetPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
            
            // Xoay đầu nhìn sang đó trước khi đi
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

            // Bắt đầu di chuyển
            isMoving = true;
            
            float startTime = Time.time;
            while (Vector3.Distance(transform.position, targetPoint) > 0.1f && !hasFled)
            {
                // Vừa đi vừa đảm bảo hướng luôn nhìn thẳng vào mục tiêu
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Time.deltaTime);
                
                // Fail-safe: Nếu đi quá lâu mà không tới (bị kẹt) thì dừng lại
                if (Time.time - startTime > 5f) break; 
                
                yield return null;
            }

            isMoving = false;
        }
    }

    // --- CÁC HÀM CŨ GIỮ NGUYÊN ---
    public void OnGeneratorExplosion()
    {
        if (!hasFled) Invoke("TriggerFlee", Random.Range(0.1f, 0.4f));
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!hasFled && other.CompareTag("Player")) TriggerFlee();
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
        StartCoroutine(FlyUpRoutine());
    }

    IEnumerator FlyUpRoutine()
    {
        float timer = 0f;
        Vector3 randomDir = (new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * 1.5f + Vector3.up).normalized;
        while (timer < 2.5f)
        {
            float currentSpeed = Mathf.Lerp(flyUpSpeed, flyUpSpeed * 0.4f, timer / 2.5f);
            transform.Translate(randomDir * currentSpeed * Time.deltaTime, Space.World);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(randomDir), Time.deltaTime * 10f);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}