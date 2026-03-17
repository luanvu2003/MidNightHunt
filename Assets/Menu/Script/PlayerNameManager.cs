using UnityEngine;
using UnityEngine.UI;

public class PlayerNameManager : 
    [Header("UI References")]
    public InputField playerNameInput;

    private const string PLAYER_PREFS_NAME_KEY = "PlayerName";

    public object PhotonNetwork { get; private set; }

    void Start()
    {
        SetUpPlayerName();
    }

    private void SetUpPlayerName()
    {
        string defaultName = string.Empty;

        // Kiểm tra xem máy người chơi đã từng lưu tên chưa
        if (PlayerPrefs.HasKey(PLAYER_PREFS_NAME_KEY))
        {
            defaultName = PlayerPrefs.GetString(PLAYER_PREFS_NAME_KEY);
            playerNameInput.text = defaultName;
        }
        else
        {
            // Nếu chưa có, tạo một tên ngẫu nhiên (VD: Player_4512)
            defaultName = "Player_" + Random.Range(1000, 10000);
            playerNameInput.text = defaultName;
        }

        // Gán tên cho Photon
        PhotonNetwork.NickName = defaultName;
    }

    // Hàm này được gọi khi người chơi gõ xong tên hoặc bấm nút "Lưu/Xác nhận"
    public void OnPlayerNameInputValueChanged()
    {
        string newName = playerNameInput.text;

        // Tránh trường hợp người chơi để trống tên
        if (string.IsNullOrEmpty(newName))
        {
            newName = "Unknown";
        }

        // Cập nhật tên mới cho Photon và lưu lại vào máy
        PhotonNetwork.NickName = newName;
        PlayerPrefs.SetString(PLAYER_PREFS_NAME_KEY, newName);
    }
}