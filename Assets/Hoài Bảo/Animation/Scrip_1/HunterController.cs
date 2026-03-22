// using System; // Khai báo thư viện hệ thống cơ bản của C#
// using UnityEngine; // Khai báo thư viện cốt lõi của Unity để dùng các hàm MonoBehaviour, GameObject, v.v.
// using UnityEngine.InputSystem; // Khai báo thư viện Input System mới của Unity để xử lý bàn phím/chuột
// using UnityEngine.UI; // Khai báo thư viện UI để làm việc với Canvas, Slider, Text...

// [RequireComponent(typeof(CharacterController), typeof(Animator))] // Bắt buộc GameObject chứa script này phải có sẵn CharacterController và Animator. Nếu quên chưa có, Unity sẽ tự động gán vào.
// public class HunterController : MonoBehaviour // Khai báo lớp HunterController kế thừa từ MonoBehaviour để có thể gắn lên các vật thể trong Unity
// {
//     [Header("Cai Dat Toc Do")] // Tạo tiêu đề "Cai Dat Toc Do" trên bảng Inspector của Unity để dễ nhìn
//     public float walkstraight = 5f; // Tốc độ di chuyển đi thẳng tới trước
//     public float walkbackward = 5f; // Tốc độ di chuyển đi lùi về sau

//     [Header("Trạng thái")] // Tạo tiêu đề "Trạng thái" trên Inspector
//     public bool isInteracting = false; // Biến cờ (flag) kiểm tra xem Hunter có đang bị khóa chân để thực hiện hành động tương tác không
//     public float currentSpeedMultiplier = 1f; // Hệ số nhân tốc độ, dùng để làm chậm (slow) Hunter khi dính đòn hoặc đang vác người

//     [Header("Hệ thống Vác Người")] // Tạo tiêu đề cho phần vác người
//     public Transform shoulderPoint; // Tọa độ cái vai của Hunter để hút cái xác Player lên đó
//     public bool isCarryingPlayer = false; // Biến cờ kiểm tra xem Hunter có đang vác ai trên vai không
//     private GameObject carriedPlayerObject; // Biến lưu trữ lại cái GameObject của Player đang bị vác

//     [Header("Tương tác (Đứng gần)")] // Tiêu đề phần tương tác UI
//     public GameObject interactUI; // Giao diện UI hiện lên chữ "Bấm Space" khi đứng gần đồ vật
//     public Slider interactionSlider; // Thanh trượt UI hiển thị tiến trình múa của hành động

//     public float timeDapMay = 2.0f; // Thời gian chạy thanh slider cho hành động Đạp Máy (2 giây)
//     public float timeTreoCUASO = 1.5f; // Thời gian chạy thanh slider cho hành động Trèo Cửa Sổ (1.5 giây)
//     public float timeTreoMoc = 3.0f;   // Thời gian chạy thanh slider cho hành động Treo Móc (3 giây)
//     public float timeNhatPlayer = 1.5f; // Thời gian chạy thanh slider cho hành động Nhặt Player (1.5 giây)

//     [Header("Cài đặt Cửa sổ")] // Tiêu đề phần setting leo cửa sổ
//     public float vaultDistance = 2.5f; // Khoảng cách dịch chuyển tịnh tiến lên phía trước khi Hunter bay xuyên qua cửa sổ

//     private float currentInteractionDuration = 1f; // Biến tạm lưu trữ tổng thời gian của hành động ĐANG thực hiện
//     private Collider currentInteractTarget; // Biến lưu trữ vật thể (Máy, Móc, Cửa sổ...) mà Hunter đang đứng gần nhất

//     private bool isSliderRunning = false; // Biến cờ kiểm tra xem thanh Slider có đang bật trạng thái chạy không
//     private float sliderTimer = 0f; // Bộ đếm thời gian thực để lấp đầy thanh Slider

//     private bool isVaulting = false; // Biến cờ kiểm tra xem Hunter có đang trong quá trình lướt qua cửa sổ không
//     private Vector3 vaultStartPos; // Lưu tọa độ vị trí lúc bắt đầu bấm nhảy qua cửa
//     private Vector3 vaultEndPos; // Tính toán tọa độ vị trí sẽ đáp xuống bên kia cửa sổ

//     [Header("Controller")] // Tiêu đề các thành phần điều khiển
//     private HunterControllerInput input; // Gọi file C# sinh ra từ Input Action Asset (hệ thống phím bấm)
//     private CharacterController controller; // Gọi component Character Controller (cái ống xanh lá) để xử lý va chạm và đi lại
//     private Animator animator; // Gọi component Animator để xử lý chuyển đổi các hoạt ảnh (animation)
//     private FPSCamera fpsCameraScript; // Gọi script FPSCamera để can thiệp khóa góc nhìn chuột khi đang tương tác

//     private float currentSpeed; // Vận tốc di chuyển thực tế hiện tại (sau khi đã tính toán gia tốc/đà)
//     private float velocityY; // Vận tốc rơi theo trục Y (dùng để mô phỏng trọng lực hút xuống đất)

//     [Header("Animation")] // Tiêu đề phần Hash Animation
//     // Chuyển đổi tên các Parameter trong Animator thành chuỗi Hash (số nguyên) để gọi cho nhẹ và mượt máy hơn là gọi bằng chữ String
//     private readonly int animSpeed = Animator.StringToHash("Speed"); 
//     private readonly int animWalkStraight = Animator.StringToHash("Dithang");
//     private readonly int animWalkBackward = Animator.StringToHash("Dilui");
//     private readonly int animpickup = Animator.StringToHash("Nhacplayer");
//     private readonly int animstep = Animator.StringToHash("Dapmay");
//     private readonly int animclimb = Animator.StringToHash("Treocuaso");
//     private readonly int animhang = Animator.StringToHash("Treomoc");

//     private void Awake() // Hàm chạy đầu tiên nhất ngay khi Hunter được sinh ra trong Scene, dùng để tìm component
//     {
//         controller = GetComponent<CharacterController>(); // Lấy CharacterController trên người Hunter
//         animator = GetComponent<Animator>(); // Lấy Animator trên người Hunter
//         input = new HunterControllerInput(); // Khởi tạo một đối tượng Input System mới để sẵn sàng đọc phím

//         if (Camera.main != null) // Nếu trên màn hình có Main Camera
//         {
//             fpsCameraScript = Camera.main.GetComponent<FPSCamera>(); // Tìm và gán script FPSCamera nằm trên Camera đó vào biến để lát sử dụng
//         }
//     }

//     private void Start() // Hàm chạy 1 lần lúc game vừa load xong
//     {
//         if (interactionSlider != null) interactionSlider.gameObject.SetActive(false); // Ẩn thanh Slider đi lúc mới vào game
//         if (interactUI != null) interactUI.SetActive(false); // Ẩn chữ gợi ý "Bấm phím" đi lúc mới vào game
//     }

//     private void OnEnable() => input.Enable(); // Khi object Hunter được bật lên, bật hệ thống lắng nghe phím bấm
//     private void OnDisable() => input.Disable(); // Khi object Hunter bị tắt đi, tắt hệ thống lắng nghe phím để tiết kiệm CPU

//     private void Update() // Hàm chạy liên tục mỗi khung hình (frame)
//     {
//         HandleSliderProgress(); // Gọi hàm xử lý lấp đầy thanh UI Slider
//         HandleVaultingMovement(); // Gọi hàm dịch chuyển thân hình qua cửa sổ

//         if (isInteracting) return; // CHỐT CHẶN: Nếu đang múa tương tác thì dừn lại, không chạy code di chuyển bên dưới nữa (tạo hiệu ứng khóa chân)

//         HandleMovement(); // Gọi hàm xử lý di chuyển bằng bàn phím (WASD)
//         HandleInteractionInput(); // Gọi hàm xử lý kiểm tra người chơi có bấm phím Space không
//         UpdateAnimator(); // Gọi hàm cập nhật thông số vận tốc vào Animator để đổi sang hoạt ảnh chạy/đi bộ
//     }

//     private void HandleInteractionInput() // Hàm xử lý logic ấn phím tương tác
//     {
//         if (currentInteractTarget != null) // Nếu đang đứng sát một vật thể nào đó
//         {
//             // LOGIC KIỂM TRA ĐIỀU KIỆN TRƯỚC KHI TƯƠNG TÁC
//             string tag = currentInteractTarget.tag; // Lấy cái Tag của vật thể đó ra (May, Moc, Player...)

//             if (tag == "Moc" && !isCarryingPlayer) // Nếu đứng cạnh cái Móc mà trên vai TRỐNG KHÔNG
//             {
//                 // Muốn treo móc nhưng không có người trên vai -> Bỏ qua
//                 return; // Thoát hàm, không cho bấm
//             }
//             if (tag == "Player" && isCarryingPlayer) // Nếu đứng cạnh 1 Player mà trên vai ĐÃ VÁC 1 người rồi
//             {
//                 // Muốn nhặt người nhưng vai đang vác 1 người rồi -> Bỏ qua
//                 return; // Thoát hàm, không cho nhặt 2 người
//             }

//             if (Keyboard.current.spaceKey.wasPressedThisFrame) // Nhận diện đúng khoảnh khắc người chơi đập phím Space
//             {
//                 if (interactUI != null) interactUI.SetActive(false); // Tắt cái chữ gợi ý đi cho đỡ vướng mắt
//                 PerformInteraction(currentInteractTarget); // Bắt đầu chạy chuỗi lệnh tương tác với vật thể đó
//             }
//         }
//     }

//     private void HandleSliderProgress() // Hàm xử lý làm đầy thanh UI Slider
//     {
//         if (isSliderRunning && interactionSlider != null) // Nếu cờ Slider đang bật và UI đã được nạp
//         {
//             sliderTimer += Time.deltaTime; // Cộng dồn thời gian thực tế trôi qua vào biến đếm
//             interactionSlider.value = sliderTimer / currentInteractionDuration; // Tính % hoàn thành bằng công thức: Thời gian đã trôi chia Tổng thời gian

//             if (sliderTimer >= currentInteractionDuration) // Nếu bộ đếm đã chạy xong bằng với thời gian quy định
//             {
//                 isSliderRunning = false; // Tắt cờ chạy Slider
//                 interactionSlider.gameObject.SetActive(false); // Cất cái Slider khỏi màn hình
//             }
//         }
//     }

//     private void HandleVaultingMovement() // Hàm xử lý lướt nhân vật đi xuyên qua cửa sổ (Parkour)
//     {
//         if (isVaulting) // Nếu cờ trèo cửa sổ đang bật
//         {
//             float progress = sliderTimer / currentInteractionDuration; // Tính % thời gian trèo (từ 0.0 đến 1.0) dựa trên Slider
//             transform.position = Vector3.Lerp(vaultStartPos, vaultEndPos, progress); // Nội suy (Lerp) tọa độ từ điểm A đến B dựa theo % ở trên để tạo cảm giác trượt mượt mà
//         }
//     }

//     public void PerformInteraction(Collider target) // Hàm thực thi hành động tương tác chính thức
//     {
//         isInteracting = true; // Bật cờ khóa chân không cho Hunter di chuyển
//         string tag = target.tag; // Lấy tag của đồ vật đang thao tác

//         Vector3 lookPosition = target.transform.position; // Lấy tọa độ của đồ vật
//         lookPosition.y = transform.position.y; // Ép tọa độ Y bằng với Hunter để Hunter không bị chúi đầu xuống đất nhìn
//         transform.LookAt(lookPosition); // Ép con Hunter xoay cả thân hình chĩa mặt thẳng vào đồ vật

//         if (fpsCameraScript != null) // Nếu lấy được script Camera
//         {
//             fpsCameraScript.isCameraLockedForAnim = true; // Bật cờ khóa chuột, không cho người chơi xoay màn hình
//             fpsCameraScript.SyncCameraAngles(transform.eulerAngles.y); // Gửi góc quay hiện tại của cái thân cho Camera để nó bám theo, không bị giật lùi góc nhìn
//         }

//         if (tag == "May") // Nếu đang đập máy phát điện
//         {
//             animator.SetTrigger(animstep); // Bật Hoạt ảnh Đập Máy
//             currentInteractionDuration = timeDapMay; // Lấy thời gian Đập máy gán vào tổng thời gian chờ
//         }
//         else if (tag == "Moc") // Nếu đang treo người lên móc
//         {
//             animator.SetTrigger(animhang); // Bật Hoạt ảnh Treo móc
//             currentInteractionDuration = timeTreoMoc; // Lấy thời gian Treo móc gán vào tổng thời gian chờ
//         }
//         else if (tag == "Player") // Nếu đang nhặt kẻ thù
//         {
//             animator.SetTrigger(animpickup); // Bật Hoạt ảnh Nhặt lên vai
//             currentInteractionDuration = timeNhatPlayer; // Lấy thời gian nhặt gán vào tổng thời gian chờ
//             // GHI NHỚ LẠI CÁI XÁC ĐỂ LÁT NỮA HÚT LÊN VAI
//             carriedPlayerObject = target.gameObject; // Lưu nguyên GameObject Player vào biến này
//         }
//         else if (tag == "Cuaso")  // Nếu đang nhảy cửa sổ
//         {
//             animator.SetTrigger(animclimb); // Bật Hoạt ảnh trèo cửa
//             currentInteractionDuration = timeTreoCUASO; // Lấy thời gian trèo cửa gán vào tổng thời gian chờ
            
//             isVaulting = true; // Bật cờ báo hiệu hệ thống Update hãy bắt đầu lướt người đi
//             controller.enabled = false; // BÍ QUYẾT: TẮT TẠM THỜI Character Controller để không bị vướng bức tường, bay lướt qua luôn
            
//             vaultStartPos = transform.position; // Lưu lại tọa độ vị trí đang đứng làm điểm khởi đầu
//             vaultEndPos = transform.position + transform.forward * vaultDistance; // Tính điểm đáp bằng cách lấy vị trí hiện tại cộng thêm một đoạn thẳng tịnh tiến về đằng trước
//         }

//         if (interactionSlider != null) // Kích hoạt UI Slider
//         {
//             interactionSlider.gameObject.SetActive(true); // Bật thanh Slider hiện lên
//             interactionSlider.value = 0f; // Reset vạch Slider về mức 0
//             sliderTimer = 0f; // Đặt đồng hồ đếm ngược về 0
//             isSliderRunning = true; // Phát cờ cho hàm Update bắt đầu đẩy thanh trượt
//         }
//     }

//     // =================================================================
//     // ANIMATION EVENT: GỌI LÚC HUNTER SỐC NGƯỜI CHƠI LÊN VAI
//     // =================================================================
//     public void AttachPlayerToShoulder() // Cần gắn vào frame ở giữa của hoạt ảnh Nhặt Player
//     {
//         if (carriedPlayerObject != null && shoulderPoint != null) // Đảm bảo đã nhặt được xác và đã setup điểm gắn trên vai
//         {
//             // Tắt va chạm của Player đi để Hunter chạy không bị vướng
//             Collider playerCol = carriedPlayerObject.GetComponent<Collider>(); // Lấy cục va chạm
//             if (playerCol != null) playerCol.enabled = false; // Tắt nó đi

//             // Hút Player vào cái xương vai
//             carriedPlayerObject.transform.SetParent(shoulderPoint); // Biến Player thành con của cái điểm trên vai Hunter
//             carriedPlayerObject.transform.localPosition = Vector3.zero; // Dời tâm tọa độ (x,y,z) của Player về 0 để trùng khớp 100% với tâm của vai
//             carriedPlayerObject.transform.localRotation = Quaternion.identity; // Reset góc xoay của Player cho thẳng thớm với cái vai

//             isCarryingPlayer = true; // Bật cờ báo hệ thống biết trên vai đang có hàng
//         }
//     }

//     // =================================================================
//     // ANIMATION EVENT: GỌI LÚC HUNTER PHÓNG NGƯỜI CHƠI VÀO MÓC
//     // =================================================================
//     public void HookPlayerToHook() // Cần gắn vào frame lúc đẩy người lên của hoạt ảnh Treo Móc
//     {
//         if (carriedPlayerObject != null && currentInteractTarget != null) // Kiểm tra an toàn: Đang vác người và đang đứng gần cái móc
//         {
//             // Tìm cái điểm treo trên cái Móc (HookPoint)
//             Transform hookPoint = currentInteractTarget.transform.Find("HookPoint"); // Lục tìm Object con có tên chính xác là HookPoint
            
//             // Nếu quên tạo HookPoint thì dán tạm vào gốc cái Móc luôn
//             if (hookPoint == null) hookPoint = currentInteractTarget.transform; // Tránh lỗi null, lấy thân cái móc làm điểm hút thay thế

//             // Chuyển Player từ vai sang Móc
//             carriedPlayerObject.transform.SetParent(hookPoint); // Ép Player rời vai Hunter, làm con của cái Móc
//             carriedPlayerObject.transform.localPosition = Vector3.zero; // Hút người vào tâm tọa độ của điểm treo móc
//             carriedPlayerObject.transform.localRotation = Quaternion.identity; // Đặt góc xoay về thẳng đứng

//             isCarryingPlayer = false; // Tắt cờ báo hiệu vai Hunter đã trống
//             carriedPlayerObject = null; // Quên cái xác đó đi để nhặt xác khác
//         }
//     }

//     public void FinishInteraction() // Gắn Event này vào frame cuối cùng của tất cả các hoạt ảnh tương tác
//     {
//         isInteracting = false; // Tắt cờ múa, trả lại quyền di chuyển bằng phím WASD cho Hunter

//         if (fpsCameraScript != null) // Mở khóa Camera
//         {
//             fpsCameraScript.isCameraLockedForAnim = false; // Cho phép xoay con chuột để đảo góc nhìn lại bình thường
//         }

//         if (isVaulting) // Nếu vừa mới trèo cửa sổ xong
//         {
//             isVaulting = false; // Tắt cờ nhảy
//             controller.enabled = true; // BẬT LẠI Character Controller để Hunter rơi phịch xuống đất và bị tường cản lại như bình thường
//         }

//         isSliderRunning = false; // Khóa thanh trượt
//         if (interactionSlider != null)
//         {
//             interactionSlider.value = 1f; // Ép thanh trượt đầy căng 100% trước khi biến mất cho đẹp mắt
//             interactionSlider.gameObject.SetActive(false); // Cất thanh trượt
//         }
//     }

//     private void OnTriggerEnter(Collider other) // Hệ thống cảm biến: Gọi khi Hunter bước VÀO VÙNG va chạm (Is Trigger) của một vật
//     {
//         if (other.CompareTag("May") || other.CompareTag("Moc") || other.CompareTag("Player") || other.CompareTag("Cuaso")) // Lọc rác, chỉ nhận những vật mang các Tag này
//         {
//             // ĐÃ THÊM: Logic UI thông minh
//             if (other.CompareTag("Moc") && !isCarryingPlayer) return; // Đứng gần Móc nhưng vai trống -> Chặn lại, không hiện chữ
//             if (other.CompareTag("Player") && isCarryingPlayer) return; // Đứng gần xác nhưng vai đang vác hàng -> Chặn lại, không hiện chữ

//             currentInteractTarget = other; // Lưu vật thể hợp lệ này vào bộ nhớ để khi bấm Space biết thao tác lên đâu
//             if (interactUI != null) interactUI.SetActive(true); // Hiển thị chữ "Bấm phím" lên màn hình
//         }
//     }

//     private void OnTriggerExit(Collider other) // Hệ thống cảm biến: Gọi khi Hunter bước RA KHỎI VÙNG va chạm
//     {
//         if (currentInteractTarget == other) // Nếu ra khỏi đúng cái vật đang ghi nhớ
//         {
//             currentInteractTarget = null; // Xóa trí nhớ, quên nó đi
//             if (interactUI != null) interactUI.SetActive(false); // Tắt chữ "Bấm phím"
//         }
//     }

//     private void HandleMovement() // Hàm xử lý di chuyển và trọng lực
//     {
//         Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>(); // Đọc nút bấm (A/D lấy trục X, W/S lấy trục Y) từ Input System
//         Vector3 moveDirection = (transform.forward * inputDir.y + transform.right * inputDir.x).normalized; // Chuyển đổi lệnh phím WASD thành một Vectơ 3D chỉ hướng đi thực tế, tính theo mặt của con Hunter

//         float targetSpeed = 0f; // Vận tốc lý tưởng muốn đạt tới
//         if (inputDir.y < 0) targetSpeed = walkbackward * currentSpeedMultiplier; // Bấm S (lùi) -> vận tốc = lùi * hệ số làm chậm
//         else if (moveDirection.magnitude > 0) targetSpeed = walkstraight * currentSpeedMultiplier; // Bấm đi tới/ngang -> vận tốc = tiến * hệ số làm chậm

//         if (controller.isGrounded && velocityY < 0) velocityY = -2f; // Nếu chân đang chạm đất, ép một lực nhỏ (-2) chĩa xuống sàn để chống nảy giật khung hình
//         velocityY += -9.81f * Time.deltaTime; // Áp dụng gia tốc trọng lực Trái Đất (-9.81 m/s) liên tục kéo nhân vật xuống để rớt khỏi mép vực

//         currentSpeed = Mathf.Lerp(currentSpeed, targetSpeed, Time.deltaTime * 15f); // Làm mượt đà di chuyển: Từ từ chuyển vận tốc hiện tại sang vận tốc đích bằng phép nội suy (Lerp)
        
//         // Gọi lệnh đi thực tế: Lấy hướng đi ngang (moveDirection) + hướng rơi dọc (velocityY). Áp dụng quy tắc nhân với Time.deltaTime để FPS cao hay thấp thì tốc độ vẫn đều nhau
//         controller.Move(moveDirection * (currentSpeed * Time.deltaTime) + new Vector3(0, velocityY, 0) * Time.deltaTime);
//     }

//     private void UpdateAnimator() // Hàm gửi thông số cho Animator điều khiển múa
//     {
//         float animationSpeedPercent = currentSpeed / walkstraight; // Tính ra chỉ số % (0 -> 1) bằng cách lấy tốc độ hiện hành chia cho tốc độ max
//         Vector2 inputDir = input.HunterControllerS.Move.ReadValue<Vector2>(); // Coi lại coi đang bấm lùi hay tới
//         if (inputDir.y < 0) animationSpeedPercent = -animationSpeedPercent; // Nếu đang bấm S (lùi), biến con số thành số âm (-) để Animator kéo phim chạy giật lùi
//         animator.SetFloat(animSpeed, animationSpeedPercent); // Bắn con số % đã tính vào Parameter "Speed" trong cửa sổ Animator
//     }

//     public void ApplySlow(float multiplier) => currentSpeedMultiplier = multiplier; // Hàm mở (Public) để các Script khác (như AttackController) gọi vào ép chậm Hunter
//     public void ResetSlow() => currentSpeedMultiplier = 1f; // Hàm mở trả hệ số nhân lại thành 1 (đi tốc độ bình thường)
// }