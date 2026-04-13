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
    public SkillCheck skillCheck;
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
    private ISurvivor _localPlayer;
    private ChangeDetector _changeDetector;
    private float _localRepairStartTime;
    public float spamThreshold = 0.8f; // Dưới 0.8s tính là spam
    // 🚨 THÊM MỚI: Biến chỉnh khoảng cách nghe âm thanh 3D
    public float maxExplosionHearingDistance = 50f; // Tiếng nổ vang xa
    public float maxRepairHearingDistance = 20f;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);

        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
        // 🚨 THÊM MỚI: Cấu hình âm thanh 3D (nhỏ dần khi đi xa) cho Máy phát điện
        if (explosionSound != null)
        {
            explosionSound.spatialBlend = 1f; // 1 = 100% 3D Audio
            explosionSound.rolloffMode = AudioRolloffMode.Linear;
            explosionSound.minDistance = 5f; // Đứng trong bán kính 5m nghe to nhất
            explosionSound.maxDistance = maxExplosionHearingDistance;
        }

        if (repairSound != null)
        {
            repairSound.spatialBlend = 1f;
            repairSound.rolloffMode = AudioRolloffMode.Linear;
            repairSound.minDistance = 3f;
            repairSound.maxDistance = maxRepairHearingDistance;
        }
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
        if (IsRepaired) return;

        // 🚨 FAILSAFE
        if (_localPlayer != null)
        {
            // 🚨 ĐÃ SỬA LẠI CÁCH GỌI TRANSFORM CỦA INTERFACE
            float distanceToPlayer = Vector3.Distance(transform.position, _localPlayer.Object.transform.position);
            if (distanceToPlayer > 3.5f)
            {
                _isPlayerInRange = false;
                if (_isLocalPlayerRepairing) StopLocalRepair();
                _localPlayer = null;
                if (repairText != null) repairText.SetActive(false);
            }
        }

        bool isStunned = !StunTimer.ExpiredOrNotRunning(Runner);

        if (_isPlayerInRange && !isStunned && !_isLocalPlayerRepairing)
        {
            if (repairText != null) repairText.SetActive(true);

            if (Input.GetKeyDown(KeyCode.E) && _localPlayer != null)
            {
                StartLocalRepair();
            }
        }
        else if (_isLocalPlayerRepairing)
        {
            if (repairText != null) repairText.SetActive(false);

            if (Input.GetKeyDown(KeyCode.E))
            {
                float repairDuration = Time.time - _localRepairStartTime;
                if (repairDuration < spamThreshold)
                {
                    RPC_ExplodeGenerator();
                }

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
        if (!Object.HasStateAuthority || IsRepaired) return;

        if (ActiveRepairers.Count > 0)
        {
            float totalSpeedMultiplier = 0f;

            // 🚨 Quét từng người đang sửa để lấy tốc độ (Nếu MrBean đang bật skill sẽ cộng nhiều hơn)
            foreach (var playerId in ActiveRepairers)
            {
                var playerObj = Runner.FindObject(playerId);
                if (playerObj != null)
                {
                    var survivor = playerObj.GetComponent<ISurvivor>();
                    if (survivor != null)
                    {
                        totalSpeedMultiplier += survivor.GetRepairSpeedMultiplier();
                    }
                }
            }

            Progress += Runner.DeltaTime * totalSpeedMultiplier;

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
        _localRepairStartTime = Time.time;

        if (progressBar != null) progressBar.gameObject.SetActive(true);
        if (skillCheck != null) skillCheck.StartNewSkillCheck(this);

        // 🚨 Báo cho nhân vật biết là đã bắt đầu sửa (Kích hoạt thời gian skill MrBean)
        if (_localPlayer != null) _localPlayer.OnStartRepair();

        RPC_ChangeRepairState(_localPlayer.Object.Id, true);
    }

    private void StopLocalRepair()
    {
        _isLocalPlayerRepairing = false;

        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);

        // 🚨 Báo cho nhân vật biết đã ngừng sửa
        if (_localPlayer != null)
        {
            _localPlayer.OnStopRepair();
            RPC_ChangeRepairState(_localPlayer.Object.Id, false);
        }
    }

    // SkillCheck sẽ gọi hàm này nếu bấm trượt
    // SkillCheck sẽ gọi hàm này nếu bấm trượt
    public void LocalFailSkillCheck()
    {
        // 🚨 KHÔNG gọi StopLocalRepair() ở đây nữa! 
        // Chỉ gửi thẳng lên Server báo "Tôi vừa bấm hụt", để Server giật điện đúng người.
        if (_localPlayer != null)
        {
            RPC_FailSkillCheckMinigame(_localPlayer.Object.Id);
        }
    }

    // 🚨 ĐỔI RpcSources.InputAuthority THÀNH RpcSources.All
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_FailSkillCheckMinigame(NetworkId playerId)
    {
        if (IsRepaired) return;

        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);

        var playerObj = Runner.FindObject(playerId);
        if (playerObj != null)
        {
            var survivor = playerObj.GetComponent<ISurvivor>();
            if (survivor != null)
            {
                survivor.TakeHit();
                survivor.SetRepairAnimation(false); // 🚨 THÊM DÒNG NÀY (Tắt anim khi bấm hụt)
            }
        }

        ActiveRepairers.Clear();
        RPC_PlayExplosionEffects();
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
            IsDamagedByHunter = false;
        }
        else
        {
            ActiveRepairers.Remove(playerId);
        }

        // 🚨 ĐỒNG BỘ ANIMATION CHO NHÂN VẬT
        // Khi Server nhận lệnh, nó sẽ ép nhân vật đó bật/tắt biến IsRepairingAnim
        var playerObj = Runner.FindObject(playerId);
        if (playerObj != null)
        {
            var survivor = playerObj.GetComponent<ISurvivor>();
            if (survivor != null)
            {
                survivor.SetRepairAnimation(isStarting);
            }
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_ExplodeGenerator()
    {
        if (IsRepaired) return;

        Progress = Mathf.Max(0, Progress - progressPenalty);
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);

        foreach (var playerId in ActiveRepairers)
        {
            var playerObj = Runner.FindObject(playerId);
            if (playerObj != null)
            {
                var survivor = playerObj.GetComponent<ISurvivor>();
                if (survivor != null)
                {
                    survivor.TakeHit();
                    survivor.SetRepairAnimation(false); // 🚨 THÊM DÒNG NÀY (Tắt anim khi thả E spam)
                }
            }
        }

        ActiveRepairers.Clear();
        RPC_PlayExplosionEffects();
    }

    private void FinishRepairServer()
    {
        Progress = repairTime;
        IsRepaired = true;

        // 🚨 THÊM MỚI: Tắt sạch animation sửa máy của những ai đang ngồi sửa
        foreach (var playerId in ActiveRepairers)
        {
            var playerObj = Runner.FindObject(playerId);
            if (playerObj != null)
            {
                var survivor = playerObj.GetComponent<ISurvivor>();
                if (survivor != null) survivor.SetRepairAnimation(false); // Ép tắt Anim
            }
        }

        ActiveRepairers.Clear();

        // Báo cho GameManager để đếm lùi số máy
        if (GameManager_Fusion.Instance != null)
        {
            GameManager_Fusion.Instance.OnGeneratorRepaired();
        }
    }

    // ========================================================
    // ALL RPCs (LỆNH TỪ SERVER PHÁT XUỐNG TẤT CẢ CLIENT)
    // ========================================================
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionEffects()
    {
        if (explosionFX != null) explosionFX.Play();

        if (explosionSound != null)
        {
            // 🚨 THÊM MỚI: Cập nhật volume tiếng nổ theo setting VFX
            if (AudioManager.Instance != null)
            {
                explosionSound.volume = AudioManager.Instance.vfxVolume;
            }
            explosionSound.Play();
        }

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
            var player = other.GetComponent<ISurvivor>();
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
            var player = other.GetComponent<ISurvivor>();
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
            if (isRunning)
            {
                // 🚨 THÊM MỚI: Liên tục đồng bộ volume khi máy đang chạy
                if (AudioManager.Instance != null) 
                {
                    repairSound.volume = AudioManager.Instance.vfxVolume;
                }

                if (!repairSound.isPlaying) repairSound.Play();
            }
            else if (!isRunning && repairSound.isPlaying) 
            {
                repairSound.Stop();
            }
        }
    }

    private void EnableRepairedVisuals()
    {
        if (repairedLight != null) repairedLight.SetActive(true);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        UpdateVisuals(false);

        // 🚨 THÊM MỚI: Mở khóa di chuyển cho người chơi (Tránh kẹt nút WASD)
        if (_isLocalPlayerRepairing)
        {
            StopLocalRepair();
        }
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
            var player = other.GetComponent<ISurvivor>();

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
            var player = other.GetComponent<ISurvivor>();

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