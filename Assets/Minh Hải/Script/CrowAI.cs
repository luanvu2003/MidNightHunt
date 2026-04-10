using UnityEngine;
using System.Collections;
using Fusion;

[RequireComponent(typeof(NetworkTransform))]
public class CrowAI : NetworkBehaviour
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
    public float flyUpSpeed = 18f;
    public float detectionRadius = 5f;

    [Header("Âm thanh & Báo động (MỚI)")]
    public AudioSource cawSound;
    [Tooltip("Khoảng cách tối đa mà Hunter/Player có thể nghe thấy tiếng quạ")]
    public float maxHearingDistance = 40f; 
    
    [Tooltip("Kéo Prefab hiệu ứng màu đỏ (Particle/Sprite) vào đây")]
    public GameObject redAlertPrefab;
    public float alertDuration = 3f; // Thời gian hiệu ứng đỏ tồn tại trên màn hình

    private Vector3 startPosition;
    private Quaternion startRotation;

    [Networked] public NetworkBool IsFleeing { get; set; }

    private ChangeDetector _changeDetector;
    private Collider[] _overlapResults = new Collider[10];

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        startPosition = transform.position;
        startRotation = transform.rotation;

        // 🚨 TỰ ĐỘNG CẤU HÌNH ÂM THANH 3D (Gần to, xa nhỏ)
        if (cawSound != null)
        {
            cawSound.spatialBlend = 1f; // 1 = Hoàn toàn là 3D, 0 = 2D (nghe rõ mồn một ở mọi nơi)
            cawSound.rolloffMode = AudioRolloffMode.Linear; // Âm lượng giảm dần đều theo khoảng cách
            cawSound.minDistance = 3f; // Đứng cách 3m sẽ nghe to nhất
            cawSound.maxDistance = maxHearingDistance; 
        }

        UpdateVisuals();

        if (HasStateAuthority)
        {
            StartCoroutine(PatrolRoutine());
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!HasStateAuthority || IsFleeing) return;

        int hitCount = Runner.GetPhysicsScene().OverlapSphere(transform.position, detectionRadius, _overlapResults, -1, QueryTriggerInteraction.UseGlobal);

        for (int i = 0; i < hitCount; i++)
        {
            Collider t = _overlapResults[i];
            if (t.CompareTag("Player"))
            {
                PlayerMovement player = t.GetComponent<PlayerMovement>();
                if (player != null && player.isSprinting)
                {
                    TriggerFleeServer();
                    break;
                }
            }
        }
    }

    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsFleeing):
                    OnFleeStateChanged();
                    break;
            }
        }
    }

    private void OnFleeStateChanged()
    {
        UpdateVisuals();

        if (IsFleeing)
        {
            if (cawSound != null) cawSound.Play();

            if (flyModel != null)
            {
                Animator anim = flyModel.GetComponentInChildren<Animator>();
                if (anim != null) { anim.Play("CrowFly", 0, 0f); anim.speed = 1.5f; }
            }

            // 🚨 TẠO HIỆU ỨNG ĐỎ BÁO ĐỘNG (Chạy trên tất cả Client để ai cũng thấy)
            if (redAlertPrefab != null)
            {
                // Sinh ra ở vị trí cũ của quạ, nhích lên trên một xíu cho dễ nhìn
                GameObject alert = Instantiate(redAlertPrefab, startPosition + Vector3.up * 2f, Quaternion.identity);
                // Tự động xóa đi sau vài giây
                Destroy(alert, alertDuration); 
            }
        }
    }

    private void UpdateVisuals()
    {
        if (idleModel != null) idleModel.SetActive(!IsFleeing);
        if (flyModel != null) flyModel.SetActive(IsFleeing);
    }

    public void OnGeneratorExplosion()
    {
        if (!HasStateAuthority || IsFleeing) return;
        StartCoroutine(DelayedFleeRoutine());
    }

    IEnumerator DelayedFleeRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
        TriggerFleeServer();
    }

    public void TriggerFleeServer()
    {
        if (IsFleeing || !HasStateAuthority) return;

        IsFleeing = true; 
        StopAllCoroutines();
        StartCoroutine(FlyAndRespawnRoutine());
    }

    IEnumerator PatrolRoutine()
    {
        while (!IsFleeing)
        {
            yield return new WaitForSeconds(Random.Range(waitTime * 0.5f, waitTime * 1.5f));
            if (IsFleeing) yield break;

            Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
            Vector3 targetPoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);

            Vector3 direction = (targetPoint - transform.position).normalized;
            if (direction != Vector3.zero)
            {
                Quaternion targetRotation = Quaternion.LookRotation(direction);
                while (Quaternion.Angle(transform.rotation, targetRotation) > 5f && !IsFleeing)
                {
                    transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Runner.DeltaTime * rotationSpeed);
                    yield return null;
                }
            }

            float startTime = Time.time;
            while (Vector3.Distance(transform.position, targetPoint) > 0.1f && !IsFleeing)
            {
                transform.position = Vector3.MoveTowards(transform.position, targetPoint, moveSpeed * Runner.DeltaTime);
                if (Time.time - startTime > 5f) break;
                yield return null;
            }
        }
    }

    IEnumerator FlyAndRespawnRoutine()
    {
        float timer = 0f;
        Vector3 randomDir = (new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * 1.5f + Vector3.up).normalized;

        while (timer < 2.5f)
        {
            float currentSpeed = Mathf.Lerp(flyUpSpeed, flyUpSpeed * 0.4f, timer / 2.5f);
            transform.Translate(randomDir * currentSpeed * Runner.DeltaTime, Space.World);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(randomDir), Runner.DeltaTime * 10f);
            timer += Runner.DeltaTime;
            yield return null;
        }

        transform.position = startPosition + Vector3.down * 100f;

        yield return new WaitForSeconds(respawnDelay);

        transform.position = startPosition;
        transform.rotation = startRotation;

        IsFleeing = false; 

        StartCoroutine(PatrolRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (!IsFleeing && other.CompareTag("Player"))
        {
            TriggerFleeServer();
        }
    }
}