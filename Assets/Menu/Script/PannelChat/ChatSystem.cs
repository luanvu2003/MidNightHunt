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
    private static string _globalChatHistory = "";
    private InputAction _toggleAction;
    private InputAction _sendAction;
    private readonly string[] _playerColors = new string[] 
    {
        "#FFD700", 
        "#00BFFF", 
        "#FF69B4", 
        "#32CD32", 
        "#FF8C00" 
    };
    public override void Spawned()
    {
        if (!HasInputAuthority) return;
        SceneManager.sceneLoaded += OnSceneLoaded;
        FindAndSetupUI();
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
            _globalChatHistory = "";
        }
    }
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
        int myPlayerId = Runner.LocalPlayer.PlayerId;
        Rpc_SendChat(myName, _inputField.text, myPlayerId);
        _inputField.text = "";
        _inputField.ActivateInputField(); 
    }

    [Rpc(RpcSources.All, RpcTargets.All)]
    public void Rpc_SendChat(string user, string message, int senderId)
    {
        string colorHex = _playerColors[Mathf.Abs(senderId) % _playerColors.Length];
        string time = DateTime.Now.ToString("HH:mm");
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