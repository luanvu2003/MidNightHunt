using UnityEngine;
using UnityEngine.UI;

public class SkillCheck : MonoBehaviour
{
    public RectTransform needle;
    public RectTransform successZone;
    private Image successZoneImage;

    public Generator generator;

    [Header("Difficulty Scaling (Tăng dần theo tiến độ)")]
    public float minRotateSpeed = 200f; // Tốc độ khi máy ở 0%
    public float maxRotateSpeed = 550f; // Tốc độ khi máy ở gần 100% (Rất nhanh)
    private float currentRotateSpeed;   // Tốc độ thực tế đang sử dụng

    [Header("Combo Settings")]
    public int requiredSuccesses = 4;
    public float startZoneWidth = 60f;
    public float shrinkPerRound = 12f;

    private int currentSuccessCount = 0;
    private float currentZoneWidth;
    private float successMin;
    private float successMax;
    private float angle = 0;
    private bool isChecking = false;

    [Header("Reward Settings")]
    public float baseProgressReward = 5f; 
    public float bonusPerCombo = 2f;      
    public float maxGeneratorProgress = 100f; 

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
        currentSuccessCount = 0; 
        currentZoneWidth = startZoneWidth; 
        gameObject.SetActive(true);
        SetupNextRound(); 
    }

    void SetupNextRound()
    {
        angle = 0; 
        isChecking = true;

        // 🔥 LOGIC TĂNG ĐỘ KHÓ: Tính tốc độ quay dựa trên tiến độ Slider
        if (generator != null)
        {
            // Tính tỷ lệ 0 -> 1
            float progressPercent = generator.progress / maxGeneratorProgress;
            // Tốc độ kim nhanh dần theo slider
            currentRotateSpeed = Mathf.Lerp(minRotateSpeed, maxRotateSpeed, progressPercent);
            
            // Tùy chọn: Vùng xanh (Success Zone) cũng hẹp lại một chút khi slider đầy
            // currentZoneWidth = Mathf.Lerp(startZoneWidth, 25f, progressPercent);
        }

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
        if (!isChecking) return;

        // Sử dụng currentRotateSpeed thay vì biến tĩnh
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
            currentSuccessCount++;
            
            if (generator != null)
            {
                // Cộng thêm tiến độ vào slider của generator
                float reward = baseProgressReward + ((currentSuccessCount - 1) * bonusPerCombo);
                generator.progress = Mathf.Min(generator.progress + reward, maxGeneratorProgress);
                
                if (generator.progressBar != null) 
                    generator.progressBar.value = generator.progress / maxGeneratorProgress; 
            }

            if (currentSuccessCount >= requiredSuccesses)
            {
                FullSuccess();
            }
            else
            {
                // Thu nhỏ vùng xanh sau mỗi lần bấm trúng trong 1 combo
                currentZoneWidth = Mathf.Max(currentZoneWidth - shrinkPerRound, 15f);
                SetupNextRound();
            }
        }
        else
        {
            Fail();
        }
    }

    void FullSuccess()
    {
        isChecking = false;
        // Đợi 0.5s hiện mini-game mới. Tốc độ sẽ tự cập nhật ở SetupNextRound
        Invoke("AutoRestart", 0.5f);
    }

    void AutoRestart()
    {
        if (generator != null && generator.progress < maxGeneratorProgress)
        {
            currentSuccessCount = 0;
            currentZoneWidth = startZoneWidth;
            SetupNextRound();
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    void Fail()
    {
        isChecking = false;
        if (generator != null)
        {
            if (generator.explosionFX != null) generator.explosionFX.Play();
            if (generator.explosionSound != null) generator.explosionSound.Play();
            
            // Phạt trừ tiến độ khi nổ
            generator.progress = Mathf.Max(0, generator.progress - 10f);
            if (generator.progressBar != null) generator.progressBar.value = generator.progress / maxGeneratorProgress;
        }
        
        Invoke("AutoRestart", 1.5f);
    }
}