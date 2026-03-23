using UnityEngine;
using UnityEngine.UI;

public class SkillCheckExitGate : MonoBehaviour
{
    public RectTransform needle;
    public RectTransform successZone;
    public float rotateSpeed = 200f;
    
    [HideInInspector] public ExitGateLogic exitGate; 

    private float angle = 0;
    private float successMin;
    private float successMax;

    void OnEnable()
    {
        angle = 0;
        // Ngẫu nhiên vị trí vùng xanh (Success Zone)
        successMin = Random.Range(30f, 150f);
        successMax = successMin + 30f;
        successZone.localRotation = Quaternion.Euler(0, 0, -successMin);
    }

    void Update()
    {
        angle += rotateSpeed * Time.deltaTime;
        if (angle >= 360f) angle -= 360f;

        needle.localRotation = Quaternion.Euler(0, 0, -angle);

        if (Input.GetKeyDown(KeyCode.Space))
            Check();
    }

    void Check()
    {
        if (angle >= successMin && angle <= successMax)
        {
            // Kiểm tra xem đang phục vụ cho Cổng hay Máy dựa vào exitGate
            string target = (exitGate != null) ? "Cổng" : "Máy phát điện";
            Debug.Log($"{target}: Skill Check Thành Công!");
            
            SuccessAction();
            gameObject.SetActive(false);
        }
        else
        {
            string target = (exitGate != null) ? "Cổng" : "Máy phát điện";
            Debug.Log($"{target}: Skill Check Thất Bại!");
            FailAction();
        }
    }

    void SuccessAction()
    {
        // Thưởng thêm tí tiến độ nếu cần (tùy bạn)
        if (exitGate != null) exitGate.progressSlider.value += 0.02f;
    }

    void FailAction()
    {
        if (exitGate != null)
        {
            // Phạt cổng
            exitGate.progressSlider.value = Mathf.Max(0, exitGate.progressSlider.value - 0.1f);
            // Quan trọng: Gọi hàm Reset để người chơi phải nhấn T lại (tránh lỗi trượt xong thanh slider chạy tiếp)
            exitGate.StopInteracting(); 
        }
        else
        {
            // Logic phạt máy phát điện của bạn ở đây (ví dụ: nổ máy)
        }
        gameObject.SetActive(false);
    }
}