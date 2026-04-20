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
    public AudioClip sirenSound; 
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
                RPC_PowerUpGates(); 
            }
        }
    }
    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void RPC_PowerUpGates()
    {
        if (globalAudioSource != null && sirenSound != null)
        {
            globalAudioSource.PlayOneShot(sirenSound);
        }
        ExitGate_Fusion[] gates = FindObjectsOfType<ExitGate_Fusion>();
        foreach (var gate in gates)
        {
            gate.OnGatesPoweredUp();
        }
    }
}