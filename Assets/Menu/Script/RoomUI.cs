using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI; // Thêm thư viện này nếu cần xử lý Image
using System.Collections;

public class RoomUI : MonoBehaviour
{
    public static RoomUI Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomIDText;
    public GameObject startGameButton;

    [Header("Player List")]
    public Transform playerListContainer;

    // THAY ĐỔI Ở ĐÂY: Đổi sang GameObject để chứa cả khung lẫn text
    public GameObject playerItemPrefab;

    private NetworkRunner _runner;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        _runner = FindFirstObjectByType<NetworkRunner>();

        if (_runner != null)
        {
            // Chạy một Coroutine để đợi dữ liệu phòng sẵn sàng
            StartCoroutine(UpdateRoomDetailsRoutine());

            if (startGameButton != null)
                startGameButton.SetActive(_runner.IsServer);
        }
    }

    IEnumerator UpdateRoomDetailsRoutine()
    {
        yield return new WaitForSeconds(0.5f); // Đợi Fusion đồng bộ properties

        if (_runner != null && _runner.SessionInfo.IsValid)
        {
            // 1. Hiển thị ID (Mã số dùng để Join)
            string idPhong = _runner.SessionInfo.Name;
            roomIDText.text = "ID: " + idPhong;

            // 2. Hiển thị Tên chủ phòng (Lấy từ Properties)
            if (_runner.SessionInfo.Properties.TryGetValue("HostName", out var hostName))
            {
                roomNameText.text = "Phòng của: " + hostName;
            }
            else
            {
                roomNameText.text = "Phòng: Đang tải...";
            }

            Debug.Log($"ID: {idPhong} | Chủ phòng: {hostName}");
        }
    }

    public void CopyRoomID()
    {
        if (_runner != null)
        {
            GUIUtility.systemCopyBuffer = _runner.SessionInfo.Name;
            Debug.Log("Đã copy ID: " + _runner.SessionInfo.Name);
        }
    }

    // --- CẬP NHẬT HÀM THÊM NGƯỜI CHƠI ---
    public void AddPlayer(string playerName)
    {
        // 1. Sinh ra toàn bộ cái object khung gỗ
        GameObject newPlayerItem = Instantiate(playerItemPrefab, playerListContainer);

        // 2. Tìm thành phần TextMeshProUGUI nằm con bên trong cái khung đó
        TextMeshProUGUI nameText = newPlayerItem.GetComponentInChildren<TextMeshProUGUI>();

        if (nameText != null)
        {
            // 3. Đổi chữ thành tên người chơi
            nameText.text = playerName;
        }

        // 4. Đặt tên cho object ngoài cùng để sau này tìm và xóa dễ dàng
        newPlayerItem.name = playerName;
    }

    // --- CẬP NHẬT HÀM XÓA NGƯỜI CHƠI ---
    public void RemovePlayer(string playerName)
    {
        // Tìm object (cái khung) mang tên người chơi vừa thoát
        Transform playerItem = playerListContainer.Find(playerName);

        // Nếu tìm thấy thì xóa nguyên cái khung đó đi
        if (playerItem != null)
        {
            Destroy(playerItem.gameObject);
        }
    }

    public void OnClickStartGame()
    {
        if (_runner.IsServer)
        {
            // Sử dụng LoadScene thay cho SetActiveScene
            _runner.LoadScene(SceneRef.FromIndex(2));
        }
    }

    public void OnClickLeave()
    {
        _runner.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}