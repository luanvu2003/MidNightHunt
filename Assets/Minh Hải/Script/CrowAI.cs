using UnityEngine;
using System.Collections;
using Fusion;

// Yêu cầu GameObject phải có component NetworkTransform để đồng bộ vị trí
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
    public AudioSource cawSound;
    public float flyUpSpeed = 18f;
    public float detectionRadius = 5f;

    private Vector3 startPosition;
    private Quaternion startRotation;

    // Biến Networked lưu trạng thái bay. Khi State Authority đổi biến này, các Client sẽ nhận được.
    [Networked] public NetworkBool IsFleeing { get; set; }

    // Dùng để phát hiện sự thay đổi của biến [Networked] trong Fusion 2
    private ChangeDetector _changeDetector;
    private Collider[] _overlapResults = new Collider[10];

    public override void Spawned()
    {
        // Khởi tạo ChangeDetector
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        startPosition = transform.position;
        startRotation = transform.rotation;

        // Cập nhật model ngay khi spawn (hữu ích cho người chơi vào phòng muộn - Late Joiners)
        UpdateVisuals();

        // Chỉ có máy chủ (State Authority) mới chạy logic tuần tra
        if (HasStateAuthority)
        {
            StartCoroutine(PatrolRoutine());
        }
    }

    public override void FixedUpdateNetwork()
    {
        // Khách (Client) hoặc Quạ đang bay thì không cần quét tìm Player
        if (!HasStateAuthority || IsFleeing) return;

        // Quét va chạm và đổ kết quả vào mảng _overlapResults. 
        // Trả về hitCount là số lượng mục tiêu thực tế nằm trong vùng quét.
        // -1 là check tất cả các layer.
        int hitCount = Runner.GetPhysicsScene().OverlapSphere(transform.position, detectionRadius, _overlapResults, -1, QueryTriggerInteraction.UseGlobal);

        // Chỉ lặp qua đúng số lượng hitCount thay vì toàn bộ mảng
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
        // Bắt sự kiện khi biến IsFleeing thay đổi trên tất cả các máy
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

    // Hàm này chạy trên TẤT CẢ client khi biến IsFleeing thay đổi
    private void OnFleeStateChanged()
    {
        UpdateVisuals();

        if (IsFleeing)
        {
            // Bật âm thanh cho tất cả người chơi
            if (cawSound != null) cawSound.Play();

            // Kích hoạt Animation
            if (flyModel != null)
            {
                Animator anim = flyModel.GetComponentInChildren<Animator>();
                if (anim != null) { anim.Play("CrowFly", 0, 0f); anim.speed = 1.5f; }
            }
        }
    }

    private void UpdateVisuals()
    {
        // Tắt bật model dựa trên trạng thái đồng bộ
        if (idleModel != null) idleModel.SetActive(!IsFleeing);
        if (flyModel != null) flyModel.SetActive(IsFleeing);
    }

    // --- HÀM QUAN TRỌNG: Generator.cs gọi hàm này ---
    public void OnGeneratorExplosion()
    {
        // Chỉ Server mới có quyền quyết định cho quạ bay
        if (!HasStateAuthority || IsFleeing) return;

        // Chạy delay ngẫu nhiên bằng Coroutine thay vì Invoke (an toàn hơn trong Multiplayer)
        StartCoroutine(DelayedFleeRoutine());
    }

    IEnumerator DelayedFleeRoutine()
    {
        yield return new WaitForSeconds(Random.Range(0.1f, 0.4f));
        TriggerFleeServer();
    }

    // Hàm này chỉ chạy trên Server
    public void TriggerFleeServer()
    {
        if (IsFleeing || !HasStateAuthority) return;

        IsFleeing = true; // Set biến này sẽ tự động báo cho tất cả Client bật âm thanh & animation
        StopAllCoroutines();

        StartCoroutine(FlyAndRespawnRoutine());
    }

    // Logic di chuyển chỉ chạy trên Server. Vị trí sẽ tự đồng bộ qua NetworkTransform
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

    // Vị trí bay lên chỉ tính toán trên Server
    IEnumerator FlyAndRespawnRoutine()
    {
        float timer = 0f;
        Vector3 randomDir = (new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized * 1.5f + Vector3.up).normalized;

        // Giai đoạn 1: Bay vút lên trời
        while (timer < 2.5f)
        {
            float currentSpeed = Mathf.Lerp(flyUpSpeed, flyUpSpeed * 0.4f, timer / 2.5f);
            transform.Translate(randomDir * currentSpeed * Runner.DeltaTime, Space.World);
            transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(randomDir), Runner.DeltaTime * 10f);
            timer += Runner.DeltaTime;
            yield return null;
        }

        // Giai đoạn 2: Ẩn model bay (Quạ bay đi xa) -> Server tạm cất nó đi dưới lòng đất hoặc tàng hình
        // Client đã tự tắt model nếu chúng ta set biến State, nhưng để cho chắc có thể giấu vị trí
        transform.position = startPosition + Vector3.down * 100f;

        // Giai đoạn 3: Đợi hồi sinh
        yield return new WaitForSeconds(respawnDelay);

        // Giai đoạn 4: Đưa quạ về vị trí cũ
        transform.position = startPosition;
        transform.rotation = startRotation;

        IsFleeing = false; // Báo cho mọi client đổi lại model thành Idle

        StartCoroutine(PatrolRoutine());
    }

    private void OnTriggerEnter(Collider other)
    {
        // Chỉ Server mới được check va chạm để tránh việc nhiều Client cùng gọi quạ bay
        if (!HasStateAuthority) return;

        if (!IsFleeing && other.CompareTag("Player"))
        {
            TriggerFleeServer();
        }
    }
}