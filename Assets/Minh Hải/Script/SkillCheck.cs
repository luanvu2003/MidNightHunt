using UnityEngine;
using UnityEngine.UI;

public class SkillCheck : MonoBehaviour
{
    public RectTransform needle;

    public float rotateSpeed = 200f;

    public float successMin = 60f;
    public float successMax = 90f;

    float angle = 0;

    public Generator generator;

    void OnEnable()
    {
        angle = 0;

    }

    void Update()
    {
        angle += rotateSpeed * Time.deltaTime;

        if (angle >= 360)
        {
            angle = 0; // reset vòng quay
        }

        needle.rotation = Quaternion.Euler(0, 0, -angle);

        if (Input.GetKeyDown(KeyCode.Space))
        {
            Check();
        }
    }

    void Check()
    {
        float current = angle % 360;

        if (current >= successMin && current <= successMax)
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
        Debug.Log("Skill Check Success");

        gameObject.SetActive(false);
    }

    void Fail()
    {
        Debug.Log("Skill Check Fail");

        generator.progress = 0f; // reset thanh sửa
        generator.progressBar.value = 0f;

        gameObject.SetActive(false);
    }
}