using Fusion;
using TMPro;
using UnityEngine;
using System.Linq;

public class PlayerNameDisplay : NetworkBehaviour
{
    [Header("Kéo thả Text tên của nhân vật vào đây")]
    public TextMeshProUGUI nameText;
    [Networked] public NetworkString<_32> SyncedPlayerName { get; set; }
    private ChangeDetector _changeDetector;
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (Object.HasStateAuthority)
        {
            var roomPlayers = FindObjectsOfType<RoomPlayer>();
            var myData = roomPlayers.FirstOrDefault(p => p.Object.InputAuthority == Object.InputAuthority);
            if (myData != null)
            {
                SyncedPlayerName = myData.PlayerName.ToString();
            }
        }
        UpdateNameUI();
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(SyncedPlayerName):
                    UpdateNameUI();
                    break;
            }
        }
    }
    private void UpdateNameUI()
    {
        if (nameText != null && !string.IsNullOrEmpty(SyncedPlayerName.ToString()))
        {
            nameText.text = SyncedPlayerName.ToString();
            if (!nameText.gameObject.activeSelf)
            {
                nameText.gameObject.SetActive(true);
            }
        }
    }
}