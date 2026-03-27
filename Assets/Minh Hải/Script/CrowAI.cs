using UnityEngine;
using System.Collections;

public class CrowAI : MonoBehaviour
{
    [Header("Cài đặt khoảng cách")]
    public float detectionRadius = 5f; 
    
    [Header("Hoán đổi Model")]
    public GameObject idleModel; 
    public GameObject flyModel;  
    public AudioSource cawSound;

    [Header("Cài đặt bay")]
    public float flyUpSpeed = 15f; 

    private bool hasFled = false;

    void Start()
    {
        if (idleModel != null) idleModel.SetActive(true);
        if (flyModel != null) flyModel.SetActive(false);
    }

    void Update()
    {
        if (hasFled) return;

        // Quét tìm Player đang CHẠY (Sprinting)
        Collider[] targets = Physics.OverlapSphere(transform.position, detectionRadius);
        foreach (var t in targets)
        {
            if (t.CompareTag("Player"))
            {
                PlayerMovement player = t.GetComponent<PlayerMovement>();
                if (player != null && player.isSprinting)
                {
                    TriggerFlee();
                    break;
                }
            }
        }
    }

    // LOGIC 1: VA CHẠM TRỰC TIẾP (Đi bộ chạm vào)
    private void OnTriggerEnter(Collider other)
    {
        if (hasFled) return;

        if (other.CompareTag("Player"))
        {
            Debug.Log("Player chạm vào quạ!");
            TriggerFlee();
        }
    }

    // LOGIC 2: TIẾNG NỔ MÁY (Gọi từ Script Generator)
    public void OnGeneratorExplosion()
    {
        if (hasFled) return;
        Invoke("TriggerFlee", Random.Range(0.1f, 0.5f));
    }

    public void TriggerFlee()
    {
        if (hasFled) return;
        hasFled = true;

        if (idleModel != null) idleModel.SetActive(false);
        if (flyModel != null) 
        {
            flyModel.SetActive(true);
            Animator anim = flyModel.GetComponentInChildren<Animator>();
            if (anim != null) anim.Play("CrowFly"); 
        }

        if (cawSound != null) cawSound.Play();

        // ĐÃ XÓA DÒNG HunterManager TẠI ĐÂY

        StartCoroutine(FlyUpRoutine());
    }

    IEnumerator FlyUpRoutine()
    {
        float timer = 0f;
        Vector3 randomDir = new Vector3(Random.Range(-1f, 1f), 0, Random.Range(-1f, 1f)).normalized;
        Vector3 flyDirection = (randomDir * 1.5f + Vector3.up).normalized;

        while (timer < 3f)
        {
            transform.Translate(flyDirection * flyUpSpeed * Time.deltaTime, Space.World);
            if (flyDirection != Vector3.zero)
                transform.rotation = Quaternion.Slerp(transform.rotation, Quaternion.LookRotation(flyDirection), Time.deltaTime * 10f);
            timer += Time.deltaTime;
            yield return null;
        }
        Destroy(gameObject);
    }
}