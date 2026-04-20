using Fusion;
using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;
using System.Linq;

public class RoomUI : MonoBehaviour
{
    public static RoomUI Instance;
    [Header("Cài Đặt Game")]
    [Tooltip("Số người cần thiết để bắt đầu. Đang test thì để 2, Build thật thì để 4")]
    public int requiredPlayers = 2; 
    [Header("UI Elements")]
    public TextMeshProUGUI roomNameText;
    public TextMeshProUGUI roomIDText;
    public GameObject startGameButton;
    [Header("UI Cảnh Báo")]
    public TextMeshProUGUI warningText;
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
            StartCoroutine(UpdateRoomDetailsRoutine());

            if (startGameButton != null)
                startGameButton.SetActive(_runner.IsServer);

            if (warningText != null) warningText.gameObject.SetActive(false);
        }
    }
    IEnumerator UpdateRoomDetailsRoutine()
    {
        while (_runner == null || _runner.SessionInfo == null || !_runner.SessionInfo.IsValid)
        {
            yield return null;
        }

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
            roomIDText.text = "" + idPhong;
        }
        if (roomNameText != null)
        {
            if (_runner.SessionInfo.Properties != null && _runner.SessionInfo.Properties.TryGetValue("HostName", out var hostName))
            {
                roomNameText.text = "" + hostName;
            }
            else
            {
                roomNameText.text = "Phòng: Đang tải...";
            }
        }
    }
    public void CopyRoomID()
    {
        if (_runner != null && _runner.SessionInfo != null && _runner.SessionInfo.IsValid)
        {
            GUIUtility.systemCopyBuffer = _runner.SessionInfo.Name;
            Debug.Log("Đã copy ID: " + _runner.SessionInfo.Name);
        }
    }
    public void AddPlayer(string playerName, int charID = -1)
    {
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
    public void RemovePlayer(string playerName)
    {
        if (playerListContainer == null) return;
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
            int currentPlayerCount = _runner.SessionInfo.PlayerCount;

            if (currentPlayerCount >= requiredPlayers)
            {
                if (RoomPlayer.Local != null)
                {
                    RoomPlayer.Local.RPC_ShowLoadingAndTransition();
                }
            }
            else
            {
                if (warningText != null)
                {
                    warningText.text = $"Chưa đủ người! Cần {requiredPlayers} người để bắt đầu (Hiện tại: {currentPlayerCount}).";
                    warningText.gameObject.SetActive(true);
                    
                    StopAllCoroutines();
                    StartCoroutine(HideWarningRoutine());
                }
            }
        }
    }
    IEnumerator HideWarningRoutine()
    {
        yield return new WaitForSeconds(3f);
        if (warningText != null) warningText.gameObject.SetActive(false);
    }
    public void OnClickLeave()
    {
        _runner.Shutdown();
        UnityEngine.SceneManagement.SceneManager.LoadScene(0);
    }
}