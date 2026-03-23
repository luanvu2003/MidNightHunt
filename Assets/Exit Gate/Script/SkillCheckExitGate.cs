using UnityEngine;
using UnityEngine.UI;

public class SkillCheckExitGate : MonoBehaviour
{
    public RectTransform needle;
    public RectTransform successZone;
    public float rotateSpeed = 200f;
    public float successMin = 60f;
    public float successMax = 90f;

    float angle = 0;

    // Chỉ nhận ExitGateLogic
    [HideInInspector] public ExitGateLogic exitGate; 

    void OnEnable()
    {
        angle = 0;
        // Xoay vùng xanh đến vị trí ngẫu nhiên mỗi lần hiện (tăng độ khó)
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
            Debug.Log("Cổng: Skill Check Thành Công!");
            gameObject.SetActive(false);
        }
        else
        {
            Debug.Log("Cổng: Skill Check Thất Bại!");
            Fail();
        }
    }

    void Fail()
    {
        if (exitGate != null)
        {
            // Phạt: Trừ 10% tiến độ trên thanh Slider của cổng
            exitGate.progressSlider.value = Mathf.Max(0, exitGate.progressSlider.value - 0.1f);
        }
        gameObject.SetActive(false);
    }
}