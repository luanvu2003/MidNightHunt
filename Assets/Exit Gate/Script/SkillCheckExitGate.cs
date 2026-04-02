using UnityEngine;
using UnityEngine.UI;

public class SkillCheckExitGate : MonoBehaviour
{
    [Header("UI Elements")]
    public RectTransform needle;
    public RectTransform successZone;
    private Image successZoneImage; // Để điều chỉnh Fill Amount tự động

    [Header("Settings")]
    public float rotateSpeed = 300f; // Tăng tốc độ cho kịch tính
    public float successZoneSize = 30f; 

    [HideInInspector] public ExitGateLogic exitGate; 

    private float angle = 0;
    private float successMin;
    private float successMax;
    private float tolerance = 5f; // Khoảng dung sai nhỏ cho người chơi

    void OnEnable()
    {
        if (successZone != null && successZoneImage == null)
        {
            successZoneImage = successZone.GetComponent<Image>();
        }
        SetupSkillCheck();
    }

    void SetupSkillCheck()
    {
        angle = 0;
        needle.localRotation = Quaternion.identity;

        // 1. Tính toán vùng xanh (Random từ 45 đến 300 độ)
        successMin = Random.Range(45f, 300f);
        successMax = successMin + successZoneSize;

        // 2. Cập nhật UI Vùng Xanh
        if (successZoneImage != null)
        {
            // Tự động chỉnh Fill Amount dựa trên độ rộng zone
            successZoneImage.fillAmount = successZoneSize / 360f;
            // Xoay vùng xanh đúng vị trí (Dùng số âm để quay theo chiều kim đồng hồ)
            successZone.localRotation = Quaternion.Euler(0, 0, -successMin);
        }
    }

    void Update()
    {
        // Kim chạy theo chiều kim đồng hồ (tăng góc angle)
        angle += rotateSpeed * Time.deltaTime;

        if (angle >= 360f)
        {
            FailAction();
            return;
        }

        // Hiển thị kim quay (xoay âm để quay phải)
        needle.localRotation = Quaternion.Euler(0, 0, -angle);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Check();
        }
    }

    void Check()
    {
        // Logic so sánh: Nếu angle hiện tại nằm trong khoảng [Min, Max]
        if (angle >= (successMin - tolerance) && angle <= (successMax + tolerance))
        {
            SuccessAction();
        }
        else
        {
            FailAction();
        }
    }

    void SuccessAction()
    {
        Debug.Log("<color=green>EXIT GATE: Skill Check Success!</color>");
        if (exitGate != null)
        {
            exitGate.progressSlider.value = Mathf.Clamp01(exitGate.progressSlider.value + 0.05f);
        }
        gameObject.SetActive(false);
    }

    void FailAction()
    {
        Debug.Log("<color=red>EXIT GATE: Skill Check Failed!</color>");
        if (exitGate != null)
        {
            exitGate.progressSlider.value = Mathf.Clamp01(exitGate.progressSlider.value - 0.1f);
            exitGate.StopInteracting(); 
        }
        gameObject.SetActive(false);
    }
}