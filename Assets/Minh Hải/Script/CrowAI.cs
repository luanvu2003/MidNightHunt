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
    public float respawnDelay = 300f;

    [Header("Model & Bay")]
    public GameObject idleModel;
    public GameObject flyModel;
    public float flyUpSpeed = 18f;
    public float detectionRadius = 5f;

    [Header("Âm thanh & Bóng Đỏ (Hunter Vision)")]
    public AudioSource cawSound;
    public float maxHearingDistance = 40f;

    [Tooltip("Kéo Prefab bóng con quạ màu đỏ vào đây")]
    public GameObject redSilhouettePrefab;
    [Tooltip("Thời gian cái bóng đỏ tồn tại (giây)")]
    public float silhouetteDuration = 5f;

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

        if (cawSound != null)
        {
            cawSound.spatialBlend = 1f;
            cawSound.rolloffMode = AudioRolloffMode.Linear;
            cawSound.minDistance = 3f;
            cawSound.maxDistance = maxHearingDistance;
            if (cawSound.isPlaying) cawSound.Stop();
        }

        UpdateVisuals();

        if (HasStateAuthority) StartCoroutine(PatrolRoutine());
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
                    if (IsFleeing) ProcessCrowFlight();
                    else UpdateVisuals();
                    break;
            }
        }
    }

    private void ProcessCrowFlight()
    {
        UpdateVisuals();

        if (flyModel != null)
        {
            Animator anim = flyModel.GetComponentInChildren<Animator>();
            if (anim != null) { anim.Play("CrowFly", 0, 0f); anim.speed = 1.5f; }
        }

        if (cawSound != null) cawSound.Play();

        // 🚨 GỌI HÀM SINH RA BÓNG ĐỎ
        SpawnRedSilhouetteLocal();
    }

    private void SpawnRedSilhouetteLocal()
    {
        // 1. Tìm script Camera đang chạy trên máy này
        GameCameraController cam = FindFirstObjectByType<GameCameraController>();

        // 2. CHỈ thực hiện tạo bóng đỏ nếu người chơi đang ở chế độ Hunter (FPS)
        if (cam != null && cam.currentMode == GameCameraController.CameraMode.FPS_Hunter)
        {
            if (redSilhouettePrefab != null)
            {
                // Sinh ra bóng đỏ tại vị trí và hướng lúc quạ bắt đầu bay
                GameObject silhouette = Instantiate(redSilhouettePrefab, startPosition, startRotation);

                // Xóa bóng sau 5 giây
                Destroy(silhouette, silhouetteDuration);
            }
        }
        // Nếu là Survivor (TPS), hàm này sẽ chạy nhưng không làm gì cả, không tốn tài nguyên
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

        if (HasStateAuthority) StartCoroutine(PatrolRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!HasStateAuthority) return;

        if (!IsFleeing && other.CompareTag("Player")) TriggerFleeServer();
    }
}