using UnityEngine;
using UnityEngine.EventSystems; 

public class CharacterRotator : MonoBehaviour
{
    [Header("Cài đặt Xoay")]
    public float rotationSpeed = 500f;
    public bool reverseDirection = false; 
    private bool isDragging = false;
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
            {
                return; 
            }
            isDragging = true;
        }
        if (Input.GetMouseButtonUp(0))
        {
            isDragging = false;
        }
        if (isDragging)
        {
            float mouseX = Input.GetAxis("Mouse X");
            float direction = reverseDirection ? 1f : -1f;
            transform.Rotate(Vector3.up, mouseX * rotationSpeed * direction * Time.deltaTime, Space.World);
        }
    }
}