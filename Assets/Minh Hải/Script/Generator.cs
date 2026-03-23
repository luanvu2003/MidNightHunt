using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 10f;
    public float progress = 0f;

    public float interactRadius = 3f;

    [Header("GameObject")]
    public Transform player;

    private bool playerInRange = false;
    private bool isRepaired = false;
    
    private bool isSkillCheckPaused = false; 

    public SkillCheck skillCheck;

    public ParticleSystem explosionFX;

    float skillTimer = 5f;

    public Slider progressBar;
    public GameObject repairText;

    [Header("Visual Effects")]
    public GameObject repairedLight;

    // 🔥 THÊM ANIMATION
    [Header("Animation")]
    public Animator animator;

    [Header("Audio Source")]
    public AudioSource explosionSound;
    public AudioSource repairSound;

    void Start()
    {
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
        skillCheck.gameObject.SetActive(false);

        if (repairedLight != null) 
        {
            repairedLight.SetActive(false);
        }

        // 🔥 TẮT animation lúc đầu
        if (animator != null)
        {
            animator.SetBool("isRunning", false);
        }

        if (repairSound != null)
        {
            repairSound.Stop();
        }
    }

    void Update()
    {
        if (isRepaired) return;

        // 🔥 Nếu đang skill check thì dừng animation
        if (skillCheck.gameObject.activeSelf) 
        {
            if (animator != null)
                animator.SetBool("isRunning", false);

            if (repairSound != null && repairSound.isPlaying)
            {
                repairSound.Stop();
            }

            if (!playerInRange || !Input.GetKey(KeyCode.E))
            {
                skillCheck.gameObject.SetActive(false);
                isSkillCheckPaused = true;
            }
            return;
        }

        // 🔥 ĐANG SỬA MÁY
        if (playerInRange && Input.GetKey(KeyCode.E))
        {
            // 👉 BẬT animation
            if (animator != null)
                animator.SetBool("isRunning", true);

            // 🔊 BẬT ÂM THANH
            if (repairSound != null && !repairSound.isPlaying)
            {
                repairSound.Play();
            }

            if (isSkillCheckPaused)
            {
                skillCheck.gameObject.SetActive(true);
                isSkillCheckPaused = false;
                return; 
            }
            
            progressBar.gameObject.SetActive(true);

            progress += Time.deltaTime;
            progressBar.value = progress / repairTime;

            skillTimer -= Time.deltaTime;

            if (skillTimer <= 0)
            {
                skillCheck.StartNewSkillCheck(this); 
                skillTimer = Random.Range(5f, 10f);
            }

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
        else 
        {
            // 👉 TẮT animation khi không sửa
            if (animator != null)
                animator.SetBool("isRunning", false);

            // 🔇 TẮT ÂM THANH
            if (repairSound != null && repairSound.isPlaying)
            {
                repairSound.Stop();
            }
        }
    }

    void FinishRepair()
    {
        isRepaired = true;

        Debug.Log("Generator này đã sửa xong!");

        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);

        // 🔥 TẮT animation khi xong
        if (animator != null)
            animator.SetBool("isRunning", false);

        if (repairSound != null)
        {
            repairSound.Stop();
        }


        if (repairedLight != null)
        {
            repairedLight.SetActive(true);
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player") && !isRepaired)
        {
            playerInRange = true;
            repairText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            repairText.SetActive(false);
            progressBar.gameObject.SetActive(false);

            // 🔥 TẮT animation khi đi ra
            if (animator != null)
                animator.SetBool("isRunning", false);

            if (repairSound != null)
            {
                repairSound.Stop();
            }
        }
    }
}