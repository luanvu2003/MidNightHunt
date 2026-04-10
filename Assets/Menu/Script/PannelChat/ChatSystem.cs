using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;

public class ChatSystem : NetworkBehaviour
{
    [Header("UI References")]
    private GameObject _pannelChatRoot;
    private GameObject _pannelcon;
    private TextMeshProUGUI _textDisplay;
    private TMP_InputField _inputField;
    private Button _buttonSend;
    private Button _iconChat;
    private ScrollRect _scrollRect;

    // Biến static để lưu lịch sử chat xuyên Scene
    private static string _globalChatHistory = "";

    public override void Spawned()
    {
        // CHỈ chạy trên máy của chính mình
        if (!HasInputAuthority) return;

        // Lắng nghe sự kiện mỗi khi chuyển Scene thì tự động tìm lại UI Chat
        SceneManager.sceneLoaded += OnSceneLoaded;

        // Tìm UI lần đầu tiên ở Scene hiện tại
        FindAndSetupUI();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            // Hủy lắng nghe khi thoát game để tránh lỗi bộ nhớ
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    // Hàm này tự động chạy mỗi khi sang map mới (Lobby -> Quay tướng -> Play)
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndSetupUI();
    }

    private void FindAndSetupUI()
    {
        // 1. Tìm cái gốc ngoài cùng
        _pannelChatRoot = GameObject.Find("Pannel_Chat");
        if (_pannelChatRoot == null) return;

        // 2. Map các thành phần UI THEO ĐÚNG HIERARCHY CỦA BẠN

        // IconChat là con của Pannel_Chat
        Transform iconChatTransform = _pannelChatRoot.transform.Find("IconChat");
        _iconChat = iconChatTransform.GetComponent<Button>();

        // Pannelcon là con của IconChat
        _pannelcon = iconChatTransform.Find("Pannelcon").gameObject;

        // Các nút bấm là con của Pannelcon
        _buttonSend = _pannelcon.transform.Find("Button_SEND").GetComponent<Button>();
        _inputField = _pannelcon.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
        _scrollRect = _pannelcon.transform.Find("PanelMesage").GetComponent<ScrollRect>();

        // Text_Mesage là con trực tiếp của PanelMesage
        _textDisplay = _pannelcon.transform.Find("PanelMesage/Text_Mesage").GetComponent<TextMeshProUGUI>();

        // 3. Xóa các event cũ và gán event mới
        _iconChat.onClick.RemoveAllListeners();
        _iconChat.onClick.AddListener(ToggleChat);

        _buttonSend.onClick.RemoveAllListeners();
        _buttonSend.onClick.AddListener(SendMessageChat);

        // 4. Ẩn Pannelcon lúc mới vào Scene
        _pannelcon.SetActive(false);

        // 5. Kiểm tra quyền Hunter (Tắt chat nếu là Hunter ở màn HBao_Map)
        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "HBao_Map")
        {
            if (RoomPlayer.Local != null && RoomPlayer.Local.IsHunter)
            {
                _pannelChatRoot.SetActive(false);
                return;
            }
        }

        // 6. Cập nhật lại lịch sử chat từ Scene trước
        if (_textDisplay != null)
        {
            _textDisplay.text = _globalChatHistory;
        }
    }

    public void ToggleChat()
    {
        if (_pannelcon != null)
        {
            bool isActive = !_pannelcon.activeSelf;
            _pannelcon.SetActive(isActive);
            if (isActive)
            {
                _inputField.ActivateInputField();
                StartCoroutine(ForceScrollDown());
            }
        }
    }

    public void SendMessageChat()
    {
        if (string.IsNullOrWhiteSpace(_inputField.text)) return;
        if (RoomPlayer.Local == null) return;

        // Lấy tên thật của người chơi từ file RoomPlayer
        string myName = RoomPlayer.Local.PlayerName.ToString();

        // Gửi tin nhắn qua mạng
        Rpc_SendChat(myName, _inputField.text);

        // Xóa ô nhập và focus lại
        _inputField.text = "";
        _inputField.ActivateInputField();
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendChat(string user, string message)
    {
        string time = DateTime.Now.ToString("HH:mm");
        string formatted = $"[{time}] <color=yellow><b>{user}:</b></color> {message}\n";

        // Lưu vào tổng lịch sử để sang map sau không bị mất
        _globalChatHistory += formatted;

        // Chỉ update màn hình nếu máy này có quyền điều khiển và đang bật UI
        if (HasInputAuthority && _textDisplay != null && _pannelChatRoot != null && _pannelChatRoot.activeSelf)
        {
            _textDisplay.text = _globalChatHistory;
            StartCoroutine(ForceScrollDown());
        }
    }

    IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
    }
}