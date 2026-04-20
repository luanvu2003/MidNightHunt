using UnityEngine;
using UnityEngine.UI;

public class SkillCheck : MonoBehaviour
{
    public RectTransform needle;
    public RectTransform successZone;
    private Image successZoneImage;
    public Generator generator;
    [Header("Difficulty Settings")]
    public int currentLevel = 1; 
    public float baseRotateSpeed = 600f; 
    [Header("Zone Settings")]
    public float startZoneWidth = 60f;
    public float shrinkPerLevel = 10f;
    private float currentZoneWidth;
    private float successMin;
    private float successMax;
    private float angle = 0;
    public bool isChecking = false; 
    private float tolerance = 3f; 
    void OnEnable()
    {
        if (successZone != null && successZoneImage == null)
        {
            successZoneImage = successZone.GetComponent<Image>();
        }
    }
    public void StartNewSkillCheck(Generator gen)
    {
        this.generator = gen;
        this.currentLevel = 1; 
        gameObject.SetActive(true);
        SetupNextRound(); 
    }
    void SetupNextRound()
    {
        angle = 0; 
        isChecking = true;
        currentZoneWidth = Mathf.Max(startZoneWidth - ((currentLevel - 1) * shrinkPerLevel), 15f);
        if (successZoneImage != null)
        {
            successZoneImage.fillAmount = currentZoneWidth / 360f;
        }
        float maxStartAngle = 360f - currentZoneWidth - 20f; 
        successMin = Random.Range(45f, maxStartAngle);
        successMax = successMin + currentZoneWidth;
        successZone.localRotation = Quaternion.Euler(0, 0, -successMin);
        needle.localRotation = Quaternion.Euler(0, 0, 0);
    }
    void Update()
    {
        if (!isChecking || generator == null) return;
        float speedMultiplier = currentLevel * 0.1f;
        float currentRotateSpeed = baseRotateSpeed * speedMultiplier;
        angle += currentRotateSpeed * Time.deltaTime;
        if (angle >= 360f)
        {
            Fail();
            return;
        }
        needle.localRotation = Quaternion.Euler(0, 0, -angle);
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckHit();
        }
    }
    void CheckHit()
    {
        if (angle >= (successMin - tolerance) && angle <= (successMax + tolerance))
        {
            Success();
        }
        else
        {
            Fail();
        }
    }
    void Success()
    {
        isChecking = false;

        if (currentLevel < 5)
        {
            currentLevel++; 
        }
        Invoke("AutoRestart", 0.5f);
    }
    public void Fail()
    {
        isChecking = false;

        if (generator != null)
        {
            generator.LocalFailSkillCheck(); 
        }

        gameObject.SetActive(false);
    }

    void AutoRestart()
    {
        if (generator != null && !generator.IsRepaired && generator.Progress < generator.repairTime)
        {
            SetupNextRound();
        }
        else
        {
            gameObject.SetActive(false); 
        }
    }
}