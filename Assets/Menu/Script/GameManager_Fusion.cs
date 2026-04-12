using Fusion;
using TMPro;
using UnityEngine;

public class GameManager_Fusion : NetworkBehaviour
{
    public static GameManager_Fusion Instance;

    [Header("UI Reference")]
    public TextMeshProUGUI generatorsRemainingText;

    [Header("Game Settings")]
    [Networked] public int GeneratorsRemaining { get; set; } = 4;

    [Header("Âm Thanh Thông Báo Mở Cửa")]
    public AudioSource globalAudioSource;
    public AudioClip sirenSound; // Tiếng hú khi sửa xong 4 máy

    public override void Spawned()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    public override void Render()
    {
        if (generatorsRemainingText != null)
        {
            generatorsRemainingText.text = GeneratorsRemaining.ToString();
        }
    }

    public void OnGeneratorRepaired()
    {
        if (!Object.HasStateAuthority) return; 

        if (GeneratorsRemaining > 0)
        {
            GeneratorsRemaining--;

            if (GeneratorsRemaining <= 0)
            {
                GeneratorsRemaining = 0;
                Debug.Log("--- ĐÃ SỬA XONG 4 MÁY! CẤP ĐIỆN CHO CỬA THOÁT HIỂM! ---");
                RPC_PowerUpGates(); // Gọi lệnh phát tiếng còi và bật điện cửa
            }
        }
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PowerUpGates()
    {
        // 1. Phát tiếng còi hú toàn map
        if (globalAudioSource != null && sirenSound != null)
        {
            globalAudioSource.PlayOneShot(sirenSound);
        }

        // 2. Tìm tất cả các cửa trên Map và kích hoạt Aura
        ExitGate_Fusion[] gates = FindObjectsOfType<ExitGate_Fusion>();
        foreach (var gate in gates)
        {
            gate.OnGatesPoweredUp();
        }
    }
}