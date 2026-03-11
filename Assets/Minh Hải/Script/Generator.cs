using UnityEngine;
using UnityEngine.UI;

public class Generator : MonoBehaviour
{
    public float repairTime = 10f;
    private float progress = 0f;

    public float interactRadius = 3f;
    public Transform player;

    private bool playerInRange = false;
    private bool isRepaired = false;

    public Slider progressBar;
    public GameObject repairText;

    void Start()
    {
        progressBar.gameObject.SetActive(false);
        repairText.SetActive(false);
    }

    void Update()
    {
        if (isRepaired) return;

        if (playerInRange && Input.GetKey(KeyCode.E))
        {
            progressBar.gameObject.SetActive(true);   // hiện thanh sửa

            progress += Time.deltaTime;
            progressBar.value = progress / repairTime;

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
            playerInRange = true;    // hiện chữ E
            repairText.SetActive(true);
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;

            repairText.SetActive(false);      // ẩn chữ E
            progressBar.gameObject.SetActive(false);   
        }
    }
}