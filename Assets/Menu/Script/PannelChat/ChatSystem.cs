using UnityEngine;
using Fusion;
using TMPro;
using UnityEngine.UI;
using System;
using System.Collections;
using UnityEngine.SceneManagement;
using UnityEngine.InputSystem;

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

    // KHAI BÁO BIẾN CHO INPUT ACTIONS
    private InputAction _toggleAction;
    private InputAction _sendAction;

    // Bảng màu cho Player (Bạn có thể đổi mã màu Hex ở đây tùy thích)
    private readonly string[] _playerColors = new string[] 
    {
        "#FFD700", // Vàng (Gold)
        "#00BFFF", // Xanh da trời (Deep Sky Blue)
        "#FF69B4", // Hồng (Hot Pink)
        "#32CD32", // Xanh lá (Lime Green)
        "#FF8C00"  // Cam (Dark Orange)
    };

    public override void Spawned()
    {
        if (!HasInputAuthority) return;

        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndSetupUI();

        // ==========================================
        // SETUP INPUT ACTIONS BẰNG CODE
        // ==========================================
        _toggleAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/tab");
        
        _sendAction = new InputAction(type: InputActionType.Button, binding: "<Keyboard>/enter");
        _sendAction.AddBinding("<Keyboard>/numpadEnter");

        _toggleAction.performed += ctx => OnToggleKeyPressed();
        _sendAction.performed += ctx => OnSendKeyPressed();

        _toggleAction.Enable();
        _sendAction.Enable();
    }

    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (HasInputAuthority)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;

            // DỌN DẸP INPUT ACTIONS KHI THOÁT ĐỂ TRÁNH LỖI BỘ NHỚ
            if (_toggleAction != null)
            {
                _toggleAction.Disable();
                _toggleAction.Dispose();
            }
            if (_sendAction != null)
            {
                _sendAction.Disable();
                _sendAction.Dispose();
            }

            // 🚨 TỰ ĐỘNG XÓA LỊCH SỬ CHAT KHI NGƯỜI CHƠI DISCONNECT / THOÁT GAME
            _globalChatHistory = "";
        }
    }

    // ==========================================
    // CÁC HÀM XỬ LÝ SỰ KIỆN TỪ INPUT ACTIONS
    // ==========================================
    private void OnToggleKeyPressed()
    {
        if (!HasInputAuthority) return;
        
        if (_pannelChatRoot != null && _pannelChatRoot.activeSelf)
        {
            ToggleChat();
        }
    }

    private void OnSendKeyPressed()
    {
        if (!HasInputAuthority) return;

        if (_pannelcon != null && _pannelcon.activeSelf)
        {
            SendMessageChat();
        }
    }

    // ==========================================
    // CÁC HÀM UI CŨ BÊN DƯỚI
    // ==========================================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        FindAndSetupUI();
    }

    private void FindAndSetupUI()
    {
        _pannelChatRoot = GameObject.Find("Pannel_Chat");
        if (_pannelChatRoot == null) return;

        Transform iconChatTransform = _pannelChatRoot.transform.Find("IconChat");
        _iconChat = iconChatTransform.GetComponent<Button>();

        _pannelcon = iconChatTransform.Find("Pannelcon").gameObject;

        _buttonSend = _pannelcon.transform.Find("Button_SEND").GetComponent<Button>();
        _inputField = _pannelcon.transform.Find("InputField (TMP)").GetComponent<TMP_InputField>();
        _scrollRect = _pannelcon.transform.Find("PanelMesage").GetComponent<ScrollRect>();
        _textDisplay = _pannelcon.transform.Find("PanelMesage/Text_Mesage").GetComponent<TextMeshProUGUI>();

        _iconChat.onClick.RemoveAllListeners();
        _iconChat.onClick.AddListener(ToggleChat);

        _buttonSend.onClick.RemoveAllListeners();
        _buttonSend.onClick.AddListener(SendMessageChat);

        _pannelcon.SetActive(false);

        string currentScene = SceneManager.GetActiveScene().name;
        if (currentScene == "HBao_Map")
        {
            if (RoomPlayer.Local != null && RoomPlayer.Local.IsHunter)
            {
                _pannelChatRoot.SetActive(false);
                return;
            }
        }

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

        string myName = RoomPlayer.Local.PlayerName.ToString();
        // 🚨 Lấy ID của người chơi để quyết định màu
        int myPlayerId = Runner.LocalPlayer.PlayerId;

        // Gửi tin nhắn qua mạng kèm theo ID
        Rpc_SendChat(myName, _inputField.text, myPlayerId);

        _inputField.text = "";
        _inputField.ActivateInputField(); 
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendChat(string user, string message, int senderId)
    {
        // 🚨 Lấy màu dựa trên ID của người gửi (dùng toán tử % để không bao giờ bị lỗi quá mảng)
        string colorHex = _playerColors[Mathf.Abs(senderId) % _playerColors.Length];

        string time = DateTime.Now.ToString("HH:mm");
        // 🚨 Áp dụng màu cho tên người gửi
        string formatted = $"[{time}] <color={colorHex}><b>{user}:</b></color> {message}\n";

        _globalChatHistory += formatted;

        if (RoomPlayer.Local != null)
        {
            var myLocalChatSystem = RoomPlayer.Local.GetComponent<ChatSystem>();
            if (myLocalChatSystem != null)
            {
                myLocalChatSystem.RefreshChatUI();
            }
        }
    }

    public void RefreshChatUI()
    {
        if (_textDisplay != null && _pannelChatRoot != null && _pannelChatRoot.activeSelf)
        {
            _textDisplay.text = _globalChatHistory;
            
            if (_pannelcon != null && _pannelcon.activeSelf)
            {
                StartCoroutine(ForceScrollDown());
            }
        }
    }

    IEnumerator ForceScrollDown()
    {
        yield return new WaitForEndOfFrame();
        if (_scrollRect != null) _scrollRect.verticalNormalizedPosition = 0f;
    }
}