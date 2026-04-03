using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
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
            // Chạy Coroutine an toàn hơn
            StartCoroutine(UpdateRoomDetailsRoutine());

            if (startGameButton != null)
                startGameButton.SetActive(_runner.IsServer);
        }
    }

    IEnumerator UpdateRoomDetailsRoutine()
    {
        while (_runner == null || _runner.SessionInfo == null || !_runner.SessionInfo.IsValid)
        {
            yield return null;
        }

        // 🚨 THÊM BẢO VỆ Ở ĐÂY: Nếu có Khung chứa thì mới dọn rác
        if (playerListContainer != null)
        {
            foreach (Transform child in playerListContainer)
            {
                Destroy(child.gameObject);
            }
        }

        string idPhong = _runner.SessionInfo.Name;

        if (roomIDText != null)
        {
            roomIDText.text = "ID: " + idPhong;
        }

        if (roomNameText != null)
        {
            if (_runner.SessionInfo.Properties != null && _runner.SessionInfo.Properties.TryGetValue("HostName", out var hostName))
            {
                roomNameText.text = "Phòng của: " + hostName;
            }
            else
            {
                roomNameText.text = "Phòng: Đang tải...";
            }
        }
    }

    public void AddPlayer(string playerName, int charID = -1)
    {
        // 🚨 THÊM BẢO VỆ Ở ĐÂY: Tránh lỗi khi đang chuyển Scene mà vẫn ráng đẻ tên
        if (playerListContainer == null) return;

        Transform existingPlayer = playerListContainer.Find(playerName);

        if (existingPlayer != null) return;

        GameObject newPlayerItem = Instantiate(playerItemPrefab, playerListContainer);
        TextMeshProUGUI nameText = newPlayerItem.GetComponentInChildren<TextMeshProUGUI>();

        if (nameText != null)
        {
            nameText.text = playerName;
        }

        newPlayerItem.name = playerName;
    }

    public void CopyRoomID()
    {
        if (_runner != null && _runner.SessionInfo != null && _runner.SessionInfo.IsValid)
        {
            GUIUtility.systemCopyBuffer = _runner.SessionInfo.Name;
            Debug.Log("Đã copy ID: " + _runner.SessionInfo.Name);
        }
    }

    public void RemovePlayer(string playerName)
    {
        Transform playerItem = playerListContainer.Find(playerName);

        if (playerItem != null)
        {
            Destroy(playerItem.gameObject);
        }
    }

    public void OnClickStartGame()
    {
        if (_runner.IsServer)
        {
            // Chuyển sang Scene 2 (Quay xổ số)
            _runner.LoadScene(SceneRef.FromIndex(2));
        }
    }

    public void OnClickLeave()
    {
        _runner.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}