using UnityEngine;

public class PalletLogic : MonoBehaviour
{
    public bool isDropped = false;
    public float dropSpeed = 500f; // Tốc độ đổ ván
    public AudioSource dropSound;

    private bool canDrop = false;

    void Update()
    {
        // Nếu chưa đổ và người chơi đang đứng gần + nhấn Space (hoặc phím bạn chọn)
        if (!isDropped && canDrop && Input.GetKeyDown(KeyCode.Space))
        {
            DropPallet();
        }
    }

    void DropPallet()
    {
        isDropped = true;
        // Xoay ván nằm xuống (thường là xoay 90 độ quanh trục X hoặc Z)
        transform.localRotation = Quaternion.Euler(90, 0, 0); 
        
        if (dropSound != null) dropSound.Play();
        
        Debug.Log("Ván đã đổ! Killer sẽ bị chặn hoặc choáng.");
        
        // Thêm logic kiểm tra nếu Killer đang đứng trong vùng va chạm để gây choáng ở đây
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player")) canDrop = true;
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player")) canDrop = false;
    }
}