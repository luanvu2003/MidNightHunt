using UnityEngine;

public class HunterAiming : MonoBehaviour
{
    [Header("Xương Cột Sống")]
    public Transform spineBone; 

    [Header("Độ gập người (0 đến 1)")]
    [Range(0f, 1f)]
    public float aimWeight = 0.5f; // Chỉ cho gập 50% theo camera để tránh cắm đầu quá sâu

    private void LateUpdate()
    {
        if (spineBone == null || Camera.main == null) return;

        float camPitch = Camera.main.transform.eulerAngles.x;

        if (camPitch > 180f) camPitch -= 360f;

        // Nhân với aimWeight để gập người mượt mà và không quá đà
        float finalPitch = camPitch * aimWeight;

        // Dùng localRotation để tránh lỗi cộng dồn liên tục mỗi frame
        // Chúng ta sẽ Rotate quanh trục X gốc của xương
        spineBone.localRotation *= Quaternion.Euler(finalPitch, 0f, 0f);
    }
}