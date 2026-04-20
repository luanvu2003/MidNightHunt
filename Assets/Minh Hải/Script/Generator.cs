using UnityEngine;
using UnityEngine.UI;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class Generator : NetworkBehaviour
{
    [Header("Generator Settings")]
    public float repairTime = 60f;
    public float progressPenalty = 5f;
    public float stunDuration = 5f; 
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
    [Networked] public float Progress { get; set; }
    [Networked] public NetworkBool IsRepaired { get; set; }
    [Networked] public NetworkBool IsDamagedByHunter { get; set; }
    [Networked, Capacity(4)] public NetworkLinkedList<NetworkId> ActiveRepairers => default;

    [Networked] private TickTimer StunTimer { get; set; }
    private bool _isPlayerInRange = false;
    private bool _isLocalPlayerRepairing = false;
    private ISurvivor _localPlayer;
    private ChangeDetector _changeDetector;
    private float _localRepairStartTime;
    public float spamThreshold = 0.8f; 
    public float maxExplosionHearingDistance = 50f; 
    public float maxRepairHearingDistance = 20f;

    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
        if (explosionSound != null)
        {
            explosionSound.spatialBlend = 1f; 
            explosionSound.rolloffMode = AudioRolloffMode.Linear;
            explosionSound.minDistance = 5f; 
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
        if (progressBar != null && (_isLocalPlayerRepairing || _isPlayerInRange))
        {
            progressBar.value = Progress / repairTime;
        }
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsRepaired):
                    if (IsRepaired) EnableRepairedVisuals();
                    break;
            }
        }
        bool isSomeoneRepairing = ActiveRepairers.Count > 0;
        UpdateVisuals(isSomeoneRepairing && !IsRepaired);
    }

    private void Update()
    {
        if (Object == null || !Object.IsValid) return;
        if (IsRepaired) return;
        if (_localPlayer != null)
        {
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
    private void StartLocalRepair()
    {
        _isLocalPlayerRepairing = true;
        _localRepairStartTime = Time.time;
        if (progressBar != null) progressBar.gameObject.SetActive(true);
        if (skillCheck != null) skillCheck.StartNewSkillCheck(this);
        if (_localPlayer != null) _localPlayer.OnStartRepair();

        RPC_ChangeRepairState(_localPlayer.Object.Id, true);
    }
    private void StopLocalRepair()
    {
        _isLocalPlayerRepairing = false;

        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (_localPlayer != null)
        {
            _localPlayer.OnStopRepair();
            RPC_ChangeRepairState(_localPlayer.Object.Id, false);
        }
    }
    public void LocalFailSkillCheck()
    {
        if (_localPlayer != null)
        {
            RPC_FailSkillCheckMinigame(_localPlayer.Object.Id);
        }
    }

    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_FailSkillCheckMinigame(NetworkId playerId)
    {
        if (IsRepaired) return;
        StunTimer = TickTimer.CreateFromSeconds(Runner, stunDuration);
        foreach (var id in ActiveRepairers)
        {
            var playerObj = Runner.FindObject(id);
            if (playerObj != null)
            {
                var survivor = playerObj.GetComponent<ISurvivor>();
                if (survivor != null)
                {
                    survivor.SetRepairAnimation(false);
                    survivor.TakeHit();
                }
            }
        }
        ActiveRepairers.Clear();
        RPC_PlayExplosionEffects();
    }
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
                    survivor.SetRepairAnimation(false); 
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
        if (GameManager_Fusion.Instance != null)
        {
            GameManager_Fusion.Instance.OnGeneratorRepaired();
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PlayExplosionEffects()
    {
        if (explosionFX != null) explosionFX.Play();

        if (explosionSound != null)
        {
            if (AudioManager.Instance != null)
            {
                explosionSound.volume = AudioManager.Instance.vfxVolume;
            }
            explosionSound.Play();
        }

        AlertNearbyCrows();
        if (_isLocalPlayerRepairing)
        {
            StopLocalRepair();
        }
    }
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
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
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
    private void UpdateVisuals(bool isRunning)
    {
        if (animator != null) animator.SetBool("isRunning", isRunning);

        if (repairSound != null)
        {
            if (isRunning)
            {
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
        if (_isLocalPlayerRepairing)
        {
            StopLocalRepair();
        }
        RemoveAuraMaterials();
    }
    private void RemoveAuraMaterials()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
        {
            if (r is ParticleSystemRenderer) continue;
            Material[] currentMats = r.materials;
            System.Collections.Generic.List<Material> cleanedMats = new System.Collections.Generic.List<Material>();
            foreach (Material m in currentMats)
            {
                if (!m.name.Contains("Mat_AuraRed") && !m.name.Contains("Mat_AuraWhite"))
                {
                    cleanedMats.Add(m);
                }
            }
            r.materials = cleanedMats.ToArray();
        }
    }
    private void AlertNearbyCrows()
    {
        Collider[] hitColliders = Physics.OverlapSphere(transform.position, 20f);
        foreach (var hitCollider in hitColliders)
        {
            var crow = hitCollider.GetComponent<CrowAI>(); 
            if (crow != null) crow.OnGeneratorExplosion();
        }
    }
    public void PlayerEnteredZone(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
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
            if (player != null && player.Object.HasInputAuthority)
            {
                _isPlayerInRange = false;
                if (_isLocalPlayerRepairing) StopLocalRepair();
                _localPlayer = null;
            }
        }
    }
}