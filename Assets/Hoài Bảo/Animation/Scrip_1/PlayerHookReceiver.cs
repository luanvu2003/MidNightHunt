// using UnityEngine;

// public class PlayerHookReceiver : MonoBehaviour
// {
//     [Header("Trạng Thái Bị Vác / Treo")]
//     public bool isBeingCarried = false;
//     public Transform targetFollow;  
//     public float followSpeed = 8f; // Tăng tốc độ hút lên một chút cho lực

//     private CharacterController controller;

//     private void Awake()
//     {
//         controller = GetComponent<CharacterController>();
//     }

//     public void GetPickedUpOrHooked(Transform targetPoint)
//     {
//         targetFollow = targetPoint;
//         isBeingCarried = true;

//         if (controller != null) controller.enabled = false;
//         Debug.Log("😱 Player: Đang bám chặt vào -> " + targetPoint.name);
//     }

//     public void ReleaseFromHunter()
//     {
//         isBeingCarried = false;
//         targetFollow = null;
//         if (controller != null) controller.enabled = true;
//     }

//     // 🚨 SỬ DỤNG LATEUPDATE ĐỂ CHỐNG TRÔI/LƠ LỬNG
//     private void LateUpdate()
//     {
//         if (isBeingCarried && targetFollow != null)
//         {
//             // Tính khoảng cách hiện tại giữa người và vai
//             float distance = Vector3.Distance(transform.position, targetFollow.position);

//             // CHIÊU TRÒ: Nếu khoảng cách bé hơn 0.1m, ép dính cứng luôn (Snap)
//             if (distance < 0.1f)
//             {
//                 transform.position = targetFollow.position;
//                 transform.rotation = targetFollow.rotation;
//             }
//             else
//             {
//                 // Nếu còn ở xa thì hút mượt mà vào
//                 transform.position = Vector3.Lerp(transform.position, targetFollow.position, followSpeed * Time.deltaTime);
//                 transform.rotation = Quaternion.Lerp(transform.rotation, targetFollow.rotation, followSpeed * Time.deltaTime);
//             }
//         }
//     }
// }
using UnityEngine;
using Fusion;

public class PlayerHookReceiver : NetworkBehaviour
{
    [Header("Trạng Thái Bị Vác / Treo")]
    [Networked, OnChangedRender(nameof(OnCarryStateChanged))]
    public NetworkBool isBeingCarried { get; set; }

    public Transform targetFollow;
    public float followSpeed = 8f;

    private CharacterController controller;

    private void Awake()
    {
        controller = GetComponent<CharacterController>();
    }

    public void GetPickedUpOrHooked(Transform targetPoint)
    {
        if (!Object.HasStateAuthority) return;

        targetFollow = targetPoint;
        isBeingCarried = true;

        // 🚨 FIX 1: ÉP TẮT VẬT LÝ NGAY LẬP TỨC TRÊN SERVER
        if (controller != null) controller.enabled = false;

        Debug.Log("😱 Player: Đang bám chặt vào -> " + targetPoint.name);
    }

    public void ReleaseFromHunter()
    {
        if (!Object.HasStateAuthority) return;

        isBeingCarried = false;
        targetFollow = null;
    }

    public void OnCarryStateChanged()
    {
        if (controller != null)
        {
            // 🚨 FIX TỪ BƯỚC TRƯỚC: Giữ cho Client cũng tắt vật lý chuẩn xác
            IShowSpeedController_Fusion survivor = GetComponent<IShowSpeedController_Fusion>();
            if (isBeingCarried || (survivor != null && survivor.IsHooked))
            {
                controller.enabled = false;
            }
            else
            {
                controller.enabled = true;
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority) return;

        if (isBeingCarried && targetFollow != null)
        {
            float distance = Vector3.Distance(transform.position, targetFollow.position);

            // 🚨 FIX 2: CHỐNG LỘN CỔ 
            // Không lấy góc xoay của cái xương vai nữa, mà lấy góc xoay của cả cơ thể Hunter (root)
            // Hoặc ép nhân vật luôn đứng thẳng tương đối với Hunter
            Quaternion safeRotation = targetFollow.root.rotation;

            if (distance < 0.1f)
            {
                transform.position = targetFollow.position;
                transform.rotation = safeRotation; // Dùng góc xoay an toàn
            }
            else
            {
                transform.position = Vector3.Lerp(transform.position, targetFollow.position, followSpeed * Runner.DeltaTime);
                transform.rotation = Quaternion.Lerp(transform.rotation, safeRotation, followSpeed * Runner.DeltaTime); // Dùng góc xoay an toàn
            }
        }
    }
}