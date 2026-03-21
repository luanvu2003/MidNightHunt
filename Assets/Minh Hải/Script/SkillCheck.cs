using UnityEngine;
using UnityEngine.UI;

public class SkillCheck : MonoBehaviour
{
    public RectTransform needle;
    public RectTransform successZone;
    private Image successZoneImage;

    public Generator generator;

    [Header("Skill Check Settings")]
    public float rotateSpeed = 250f;
    
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
    public float baseProgressReward = 5f; // Tiến độ tăng lên ở lần bấm trúng đầu tiên
    public float bonusPerCombo = 2f;      // Điểm cộng dồn thêm cho các lần bấm sau
    public float maxGeneratorProgress = 100f; // Giới hạn tối đa của thanh sửa máy

    // Khoảng du di (sai số) giúp việc bấm ở sát mép dễ ăn hơn
    private float tolerance = 3f; 

    void Awake()
    {
        if (successZone != null)
        {
            successZoneImage = successZone.GetComponent<Image>();
            
            // ÉP BUỘC CÀI ĐẶT UI BẰNG CODE ĐỂ TRÁNH LỖI LỆCH GIAO DIỆN:
            successZoneImage.type = Image.Type.Filled;
            successZoneImage.fillMethod = Image.FillMethod.Radial360;
            successZoneImage.fillOrigin = (int)Image.Origin360.Top; // Luôn bắt đầu từ 12h
            successZoneImage.fillClockwise = true; // Luôn thuận chiều kim đồng hồ
        }
    }

    void OnEnable()
    {
        currentSuccessCount = 0;
        currentZoneWidth = startZoneWidth;
        SetupNextRound();
    }

    void SetupNextRound()
    {
        angle = 0; // Reset kim về 0
        isChecking = true;

        float maxStartAngle = 360f - currentZoneWidth - 10f; 
        successMin = Random.Range(45f, maxStartAngle);
        successMax = successMin + currentZoneWidth;

        if (successZoneImage != null)
        {
            successZoneImage.fillAmount = currentZoneWidth / 360f;
        }
        successZone.localRotation = Quaternion.Euler(0, 0, -successMin);
    }

    void Update()
    {
        if (!isChecking) return;

        angle += rotateSpeed * Time.deltaTime;

        // Vòng lặp vô hạn: Nếu kim quay hết 1 vòng (360 độ) thì reset lại góc
        if (angle >= 360f)
        {
            angle -= 360f;
        }

        // Cập nhật vị trí xoay của kim
        needle.localRotation = Quaternion.Euler(0, 0, -angle);

        // Chỉ kiểm tra Thắng/Thua khi người chơi thực sự bấm phím Space
        if (Input.GetKeyDown(KeyCode.Space))
        {
            CheckHit();
        }
    }

    void CheckHit()
    {
        // KIỂM TRA TRÚNG: Áp dụng dung sai (tolerance) cho cả Min và Max
        if (angle >= (successMin - tolerance) && angle <= (successMax + tolerance))
        {
            currentSuccessCount++;
            Debug.Log("TRÚNG! Lần " + currentSuccessCount + "/" + requiredSuccesses + ".");

            // ================= THÊM LOGIC TĂNG TIẾN ĐỘ Ở ĐÂY =================
            if (generator != null)
            {
                // Tính điểm thưởng: Lần 1 = 5. Lần 2 = 5 + 2 = 7. Lần 3 = 5 + 4 = 9...
                float reward = baseProgressReward + ((currentSuccessCount - 1) * bonusPerCombo);
                generator.progress += reward;
                
                // Đảm bảo tiến độ không vượt quá mức tối đa
                if (generator.progress > maxGeneratorProgress) 
                {
                    generator.progress = maxGeneratorProgress;
                }

                // Cập nhật giao diện thanh Slider của Generator
                if (generator.progressBar != null) 
                {
                    // ĐÃ SỬA: Chia cho maxGeneratorProgress để thanh Slider chạy đúng tỷ lệ 0 -> 1
                    generator.progressBar.value = generator.progress / maxGeneratorProgress; 
                }
                
                Debug.Log("Được thưởng " + reward + " tiến độ! Tổng: " + generator.progress);
            }
            // =================================================================

            if (currentSuccessCount >= requiredSuccesses)
            {
                FullSuccess();
            }
            else
            {
                // Qua màn: Thu nhỏ vùng xanh và đi tiếp
                currentZoneWidth -= shrinkPerRound;
                if (currentZoneWidth < 10f) currentZoneWidth = 10f; 
                SetupNextRound();
            }
        }
        else
        {
            // Bấm trượt
            if (angle < successMin - tolerance)
                Debug.LogWarning("THẤT BẠI: Bấm quá SỚM!");
            else
                Debug.LogWarning("THẤT BẠI: Bấm quá TRỄ!");
            
            Fail();
        }
    }

    void FullSuccess()
    {
        Debug.Log("================ HOÀN THÀNH COMBO! ================");
        isChecking = false;
        gameObject.SetActive(false);
        // Có thể thêm code cộng thêm tiến độ sửa máy ở đây
    }

    void Fail()
    {
        isChecking = false;
        if (generator != null)
        {
            if (generator.explosionFX != null) generator.explosionFX.Play();
            if (generator.explosionSound != null) generator.explosionSound.Play();

            // Reset thanh tiến độ
            generator.progress = 0f;
            if (generator.progressBar != null) generator.progressBar.value = generator.progress;
        }
        gameObject.SetActive(false);
    }
}