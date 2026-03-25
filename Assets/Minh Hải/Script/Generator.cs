using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 60f;
    public float progress = 0f;

    public int currentSkillLevel = 1;

    [Header("Penalty Settings")]
    public float stunDuration = 10f;
    private float stunTimer = 0f;
    public float progressPenalty = 5f;

    [Header("Player Settings")]
    public Transform player;
    // --- BIẾN MỚI ĐỂ KHÓA DI CHUYỂN ---
    public MonoBehaviour playerMovementScript;

    private bool playerInRange = false;
    private int zonesOccupied = 0;

    private bool isRepaired = false;
    public bool isRepairing = false;

    [Header("UI & Minigame")]
    public SkillCheck skillCheck;
    public Slider progressBar;
    public GameObject repairText;
    public ParticleSystem explosionFX;

    [Header("Visual Effects & Audio")]
    public GameObject repairedLight;
    public Animator animator;
    public AudioSource explosionSound;
    public AudioSource repairSound;

    void Start()
    {
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (repairedLight != null) repairedLight.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        if (stunTimer > 0)
        {
            stunTimer -= Time.deltaTime;
            if (stunTimer <= 0 && playerInRange)
            {
                if (repairText != null) repairText.SetActive(true);
            }
        }

        // 1. NHẤN E ĐỂ BẮT ĐẦU HOẶC DỪNG SỬA
        if (playerInRange && Input.GetKeyDown(KeyCode.E) && stunTimer <= 0)
        {
            if (isRepairing)
            {
                if (skillCheck.isChecking) ExplodeGenerator();
                else StopRepairing();
            }
            else
            {
                isRepairing = true;

                // --- KHÓA DI CHUYỂN ---
                if (playerMovementScript != null) playerMovementScript.enabled = false;

                if (progressBar != null) progressBar.gameObject.SetActive(true);
                if (skillCheck != null && !skillCheck.gameObject.activeSelf)
                    skillCheck.StartNewSkillCheck(this);
            }
        }

        // 2. XỬ LÝ LOGIC KHI ĐANG SỬA
        if (isRepairing && playerInRange)
        {
            UpdateVisuals(true);

            progress += Time.deltaTime;
            if (progressBar != null) progressBar.value = progress / repairTime;

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
        else if (isRepairing && !playerInRange)
        {
            if (skillCheck.isChecking) ExplodeGenerator();
            else StopRepairing();
        }
    }

    public void ApplyStun()
    {
        currentSkillLevel = 1;
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();

        StopRepairing();
        stunTimer = stunDuration;
        if (repairText != null) repairText.SetActive(false);
    }

    public void ExplodeGenerator()
    {
        currentSkillLevel = 1;
        if (explosionFX != null) explosionFX.Play();
        if (explosionSound != null) explosionSound.Play();

        progress = Mathf.Max(0, progress - progressPenalty);
        if (progressBar != null) progressBar.value = progress / repairTime;

        StopRepairing();
    }

    public void StopRepairing()
    {
        isRepairing = false;

        // --- MỞ KHÓA DI CHUYỂN ---
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        UpdateVisuals(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }

    void UpdateVisuals(bool running)
    {
        if (animator != null) animator.SetBool("isRunning", running);
        if (repairSound != null)
        {
            if (running && !repairSound.isPlaying) repairSound.Play();
            else if (!running && repairSound.isPlaying) repairSound.Stop();
        }
    }

    void FinishRepair()
    {
        isRepaired = true;
        isRepairing = false;

        // --- MỞ KHÓA DI CHUYỂN ---
        if (playerMovementScript != null) playerMovementScript.enabled = true;

        if (progressBar != null)
        {
            progressBar.value = 1f;
            progressBar.gameObject.SetActive(false);
        }

        if (repairText != null) repairText.SetActive(false);
        if (skillCheck != null) skillCheck.gameObject.SetActive(false);
        UpdateVisuals(false);

        if (repairedLight != null) repairedLight.SetActive(true);

        RepairZone[] zones = GetComponentsInChildren<RepairZone>();
        foreach (RepairZone zone in zones)
        {
            Collider col = zone.GetComponent<Collider>();
            if (col != null) col.enabled = false;
        }
    }

    public void PlayerEnteredZone()
    {
        if (isRepaired) return;

        zonesOccupied++;
        if (zonesOccupied > 0)
        {
            playerInRange = true;
            if (stunTimer <= 0)
            {
                if (repairText != null) repairText.SetActive(true);
            }
        }
    }

    public void PlayerExitedZone()
    {
        zonesOccupied--;
        if (zonesOccupied <= 0)
        {
            zonesOccupied = 0;
            playerInRange = false;
            if (repairText != null) repairText.SetActive(false);
        }
    }
}