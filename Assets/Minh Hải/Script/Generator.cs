using UnityEngine;
using UnityEngine.UI;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class Generator : NetworkBehaviour
{
    [Header("Generator Settings")]
    public float repairTime = 60f;
    public float progressPenalty = 5f;
    public float stunDuration = 5f; // Thời gian cấm sửa sau khi nổ

    // 🚨 BIẾN CHO HUNTER
    public float hunterDamageAmount = 15f;

    [Header("UI & Minigame (Local Only)")]
    public SkillCheck skillCheck; // Nhớ sửa script SkillCheck gọi hàm LocalFailSkillCheck() thay vì Explode() cũ
    public Slider progressBar;
    public GameObject repairText;
    public ParticleSystem explosionFX;

    [Header("Visual Effects & Audio")]
    public GameObject repairedLight;
    public Animator animator;
    public AudioSource explosionSound;
    public AudioSource repairSound;

    // ========================================================
    // BIẾN ĐỒNG BỘ MẠNG (NETWORKED)
    // ========================================================
    [Networked] public float Progress { get; set; }
    [Networked] public NetworkBool IsRepaired { get; set; }
    [Networked] public NetworkBool IsDamagedByHunter { get; set; }

    // Danh sách lưu ID của những người chơi ĐANG NGỒI SỬA CÙNG LÚC
    [Networked, Capacity(4)] public NetworkLinkedList<NetworkId> ActiveRepairers => default;

    [Networked] private TickTimer StunTimer { get; set; }

    // ========================================================
    // BIẾN CỤC BỘ (LOCAL STATE)
    // ========================================================
    private bool _isPlayerInRange = false;
    private bool _isLocalPlayerRepairing = false;
    private IShowSpeedController_Fusion _localPlayer;
    private ChangeDetector _changeDetector;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
    }

    public override void Render()
    {
        // 1. Cập nhật thanh tiến trình liên tục cho tất cả mọi người thấy
        if (progressBar != null && (_isLocalPlayerRepairing || _isPlayerInRange))
        {
            progressBar.value = Progress / repairTime;
        }

        // 2. Lắng nghe sự thay đổi của mạng để Cập nhật Visual (Âm thanh, Animation)
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsRepaired):
                    if (IsRepaired) EnableRepairedVisuals();
                    break;
            }
        }

        // Bật tắt tiếng máy chạy dựa vào việc có ai đang sửa không
        bool isSomeoneRepairing = ActiveRepairers.Count > 0;
        UpdateVisuals(isSomeoneRepairing && !IsRepaired);
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;
        // Xử lý Input Cục bộ (Chỉ chạy trên máy của Player)
        if (IsRepaired) return;

        // Nếu máy đang bị Stun (Vừa nổ xong) thì giấu text
        bool isStunned = !StunTimer.ExpiredOrNotRunning(Runner);

        if (_isPlayerInRange && !isStunned && !_isLocalPlayerRepairing)
        {
            if (repairText != null) repairText.SetActive(true);

            // Bấm E để BẮT ĐẦU sửa
            if (Input.GetKeyDown(KeyCode.E) && _localPlayer != null)
            {
                StartLocalRepair();
            }
        }
        else if (_isLocalPlayerRepairing)
        {
            if (repairText != null) repairText.SetActive(false);

            // Bấm E lần nữa để DỪNG sửa
            if (Input.GetKeyDown(KeyCode.E))
            {
                StopLocalRepair();
            }
        }
        else
        {
            if (repairText != null) repairText.SetActive(false);
        }
    }

    public override void FixedUpdateNetwork()
    {
        // LOGIC CHÍNH: Chỉ Server/Host mới tính toán tiến trình sửa
        if (!Object.HasStateAuthority || IsRepaired) return;

        if (ActiveRepairers.Count > 0)
        {
            // Tốc độ sửa nhân lên theo số lượng người (1 người = 1x, 2 người = 2x, 3 người = 3x)
            float speedMultiplier = ActiveRepairers.Count;
            Progress += Runner.DeltaTime * speedMultiplier;

            if (Progress >= repairTime)
            {
                FinishRepairServer();
            }
        }
    }

    // ========================================================
    // QUẢN LÝ TRẠNG THÁI LOCAL (UI CỦA NGƯỜI CHƠI)
    // ========================================================
    private void StartLocalRepair()
    {
        _isLocalPlayerRepairing = true;

        if (progressBar != null) progressBar.gameObject.SetActive(true);
        if (skillCheck != null) skillCheck.StartNewSkillCheck(this); // Kích hoạt SkillCheck vòng quay

        // Gửi lệnh lên Server báo: "Tôi bắt đầu sửa"
        RPC_ChangeRepairState(_localPlayer.Object.Id, true);
    }

    private void StopLocalRepair()
    {
        _isLocalPlayerRepairing = false;

        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);

        // Gửi lệnh lên Server báo: "Tôi ngừng sửa"
        if (_localPlayer != null)
        {
            RPC_ChangeRepairState(_localPlayer.Object.Id, false);
        }
    }

    // SkillCheck sẽ gọi hàm này nếu bấm trượt
    public void LocalFailSkillCheck()
    {
        StopLocalRepair();
        RPC_ExplodeGenerator(); // Báo lên Server là tôi làm nổ máy
    }

    // ========================================================
    // SERVER RPCs (LỆNH GỬI TỪ CLIENT LÊN SERVER)
    // ========================================================
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ChangeRepairState(NetworkId playerId, NetworkBool isStarting)
    {
        if (IsRepaired) return;

        if (isStarting)
        {
            if (!ActiveRepairers.Contains(playerId)) ActiveRepairers.Add(playerId);
            IsDamagedByHunter = false; // Survivor chạm vào là mất cờ bị đạp
        }
        else
        {
            ActiveRepairers.Remove(playerId);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ExplodeGenerator()
    {
        if (IsRepaired) return;

        // 1. Trừ tiến trình
        Progress = Mathf.Max(0, Progress - progressPenalty);
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);

        // 2. Phạt Dính Hit TẤT CẢ những ai đang ngồi chung cái máy này
        foreach (var playerId in ActiveRepairers)
        {
            var playerObj = Runner.FindObject(playerId);
            if (playerObj != null)
            {
                var playerScript = playerObj.GetComponent<IShowSpeedController_Fusion>();
                if (playerScript != null)
                {
                    playerScript.TakeHit(); // Cắn 1 hit vì làm nổ máy
                }
            }
        }

        // 3. Đuổi tất cả ra khỏi máy
        ActiveRepairers.Clear();

        // 4. Phát tín hiệu hình ảnh/âm thanh nổ cho tất cả client
        RPC_PlayExplosionEffects();
    }

    private void FinishRepairServer()
    {
        Progress = repairTime;
        IsRepaired = true;
        ActiveRepairers.Clear();
        // Gọi cho GameManager nếu có: GameManager.Instance.GeneratorFixed();
    }

    // ========================================================
    // ALL RPCs (LỆNH TỪ SERVER PHÁT XUỐNG TẤT CẢ CLIENT)
    // ========================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionEffects()
    {
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();

        AlertNearbyCrows();

        // Ép văng UI ra nếu đang ngồi sửa (Vì bị kick)
        if (_isLocalPlayerRepairing)
        {
            StopLocalRepair();
        }
    }

    // ========================================================
    // HÀM CHO HUNTER ĐẠP MÁY (Gọi trên Server)
    // ========================================================
    public void DamageByHunterServer()
    {
        if (!Object.HasStateAuthority || !CanBeDamagedByHunter()) return;

        Progress = Mathf.Max(0, Progress - hunterDamageAmount);
        IsDamagedByHunter = true;

        RPC_PlayExplosionEffects();
    }

    public bool CanBeDamagedByHunter()
    {
        return !IsRepaired && Progress > 0f && !IsDamagedByHunter && ActiveRepairers.Count == 0;
    }

    // ========================================================
    // TRIGGER (PHÁT HIỆN LOCAL PLAYER TỚI GẦN)
    // ========================================================
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<IShowSpeedController_Fusion>();
            // CHỈ BẬT UI cho nhân vật MÀ MÌNH ĐIỀU KHIỂN (Tránh vụ thằng khác đi tới máy thì màn hình mình lại hiện chữ E)
            if (player != null && player.Object.HasInputAuthority)
            {
                _isPlayerInRange = true;
                _localPlayer = player;
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<IShowSpeedController_Fusion>();
            if (player != null && player.Object.HasInputAuthority)
            {
                _isPlayerInRange = false;
                if (_isLocalPlayerRepairing) StopLocalRepair();
                _localPlayer = null;
            }
        }
    }

    // ========================================================
    // VISUALS & UTILITIES
    // ========================================================
    private void UpdateVisuals(bool isRunning)
    {
        if (animator != null) animator.SetBool("isRunning", isRunning);
        if (repairSound != null)
        {
            if (isRunning && !repairSound.isPlaying) repairSound.Play();
            else if (!isRunning && repairSound.isPlaying) repairSound.Stop();
        }
    }

    private void EnableRepairedVisuals()
    {
        if (repairedLight != null) repairedLight.SetActive(true);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        UpdateVisuals(false);
    }

    private void AlertNearbyCrows()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20f);
        foreach (var hitCollider in hitColliders)
        {
            var crow = hitCollider.GetComponent<CrowAI>(); // Nhớ dùng script Quạ Fusion mới nhé
            if (crow != null) crow.OnGeneratorExplosion();
        }
    }
    // ========================================================
    // TRIGGER TỪ CÁC REPAIR ZONE GỬI VỀ
    // ========================================================
    public void PlayerEnteredZone(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<IShowSpeedController_Fusion>();

            // CHỈ BẬT UI cho nhân vật MÀ MÌNH ĐIỀU KHIỂN
            if (player != null && player.Object.HasInputAuthority)
            {
                _isPlayerInRange = true;
                _localPlayer = player;
            }
        }
    }

    public void PlayerExitedZone(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<IShowSpeedController_Fusion>();

            // Nếu chính mình đi ra khỏi vùng thì mới tắt UI và ngừng sửa
            if (player != null && player.Object.HasInputAuthority)
            {
                _isPlayerInRange = false;
                if (_isLocalPlayerRepairing) StopLocalRepair();
                _localPlayer = null;
            }
        }
    }
}