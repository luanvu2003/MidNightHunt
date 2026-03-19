using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 10f;
    public float progress = 0f;

    public float interactRadius = 3f;
    public Transform player;

    private bool playerInRange = false;
    private bool isRepaired = false;

    public SkillCheck skillCheck;

    public ParticleSystem explosionFX; // Hiệu ứng
    public AudioSource explosionSound;  // Audio

    float skillTimer = 5f;   // thời gian xuất hiện skill check

    public Slider progressBar;
    public GameObject repairText;

    void Start()
    {
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);

        skillCheck.gameObject.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        // nếu skill check đang chạy thì tạm dừng sửa
        if (skillCheck.gameObject.activeSelf) return;

        if (playerInRange && Input.GetKey(KeyCode.E))
        {
            progressBar.gameObject.SetActive(true);

            progress += Time.deltaTime;
            progressBar.value = progress / repairTime;

            // đếm thời gian skill check
            skillTimer -= Time.deltaTime;

            if (skillTimer <= 0)
            {
                skillCheck.generator = this; // truyền generator vào skill check
                skillCheck.gameObject.SetActive(true);

                skillTimer = Random.Range(5f, 10f);
            }

            if (progress >= repairTime)
            {
                FinishRepair();
            }
        }
    }

    void FinishRepair()
    {
        isRepaired = true;

        Debug.Log("Generator đã sửa xong!");

        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
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
        }
    }
}