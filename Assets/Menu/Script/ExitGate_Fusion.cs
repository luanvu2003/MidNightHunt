using UnityEngine;
using UnityEngine.UI;
using Fusion;

[RequireComponent(typeof(NetworkObject))]
public class ExitGate_Fusion : NetworkBehaviour
{
    [Header("Cài Đặt Cửa")]
    public float timeToOpen = 60f; 
    public float interactDistance = 3.5f; 
    [Header("UI & Tương Tác (Local)")]
    [Tooltip("Text 'Giữ E để mở' - Phải nằm trong Canvas của riêng Prefab này")]
    public GameObject interactText;
    [Tooltip("Thanh tiến trình - Phải nằm trong Canvas của riêng Prefab này")]
    public Slider progressBar;
    [Header("Object Cửa (Kéo thả vào đây)")]
    public GameObject switchBox; 
    public GameObject gateBlocker; 
    public GameObject escapeZone; 
    public GameObject[] indicatorLights = new GameObject[6]; 
    [Header("Hệ thống Aura Đỏ")]
    public GameObject auraSwitchObject;
    [Header("Âm thanh")]
    public AudioSource gateAudioSource; 
    public AudioClip globalPowerUpSound; 
    public AudioClip openingSound; 
    public AudioClip openedSound;  
    [Networked] public NetworkBool IsPowered { get; set; } 
    [Networked] public NetworkBool IsOpened { get; set; }  
    [Networked] public float Progress { get; set; }      
    [Networked, Capacity(4)] public NetworkLinkedList<NetworkId> ActiveOpeners => default;
    private bool _inRange = false;
    private ISurvivor _localPlayer;
    private ChangeDetector _changeDetector;
    private bool _isOpeningLocally = false;
    public override void Spawned()
    {
        _changeDetector = GetChangeDetector(ChangeDetector.Source.SimulationState);
        if (interactText != null) interactText.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(false);
        foreach (var light in indicatorLights)
            if (light != null) light.SetActive(false);
        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }
    public void OnGatesPoweredUp()
    {
        if (Object.HasStateAuthority) IsPowered = true;
        if (globalPowerUpSound != null)
        {
            Vector3 playPos = Camera.main != null ? Camera.main.transform.position : transform.position;
            AudioSource.PlayClipAtPoint(globalPowerUpSound, playPos, GetVFXVolume());
        }
        HandleAuraVisibility();
    }

    private void HandleAuraVisibility()
    {
        if (auraSwitchObject == null) return;
        bool isHunter = false;
        NetworkObject[] allNetObjects = FindObjectsOfType<NetworkObject>();
        foreach (var networkObj in allNetObjects)
        {
            if (networkObj.HasInputAuthority)
            {
                if (networkObj.CompareTag("Hunter") || networkObj.GetComponent<HunterInteraction>() != null)
                {
                    isHunter = true;
                    break;
                }
            }
        }
        if (isHunter)
        {
            auraSwitchObject.SetActive(true);
        }
        else
        {
            auraSwitchObject.SetActive(true);
            Invoke(nameof(HideAuraSwitch), 10f);
        }
    }
    public override void Render()
    {
        foreach (var change in _changeDetector.DetectChanges(this))
        {
            switch (change)
            {
                case nameof(IsOpened):
                    if (IsOpened) OpenGateVisuals();
                    break;
            }
        }
        int lightsToTurnOn = Mathf.FloorToInt(Progress / 10f);
        for (int i = 0; i < indicatorLights.Length; i++)
        {
            if (indicatorLights[i] != null)
            {
                indicatorLights[i].SetActive(i < lightsToTurnOn);
            }
        }
        if (progressBar != null && progressBar.gameObject.activeSelf)
        {
            progressBar.value = Progress / timeToOpen;
        }
        bool isSomeoneOpening = ActiveOpeners.Count > 0;
        if (gateAudioSource != null && openingSound != null)
        {
            if (isSomeoneOpening && !IsOpened)
            {
                if (!gateAudioSource.isPlaying || gateAudioSource.clip != openingSound)
                {
                    gateAudioSource.clip = openingSound;
                    gateAudioSource.loop = true;
                    gateAudioSource.volume = GetVFXVolume();
                    gateAudioSource.Play();
                }
                else
                {
                    gateAudioSource.volume = GetVFXVolume(); 
                }
            }
            else if ((!isSomeoneOpening || IsOpened) && gateAudioSource.clip == openingSound && gateAudioSource.isPlaying)
            {
                gateAudioSource.Stop();
            }
        }
    }
    private void Update()
    {
        if (Object == null || !Object.IsValid || !IsPowered || IsOpened) return;
        if (_localPlayer != null)
        {
            float dist = Vector3.Distance(switchBox.transform.position, _localPlayer.Object.transform.position);
            if (dist > interactDistance)
            {
                ResetInteractionLocal();
            }
        }

        if (_inRange)
        {
            if (Input.GetKeyDown(KeyCode.E) && !_isOpeningLocally)
            {
                _isOpeningLocally = true;
                if (interactText != null) interactText.SetActive(false);
                if (progressBar != null) progressBar.gameObject.SetActive(true);
                StartOpeningLocally();
            }

            if (Input.GetKeyUp(KeyCode.E) && _isOpeningLocally)
            {
                _isOpeningLocally = false;
                if (interactText != null) interactText.SetActive(true);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
                StopOpeningLocally();
            }
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || !IsPowered || IsOpened) return;

        if (ActiveOpeners.Count > 0)
        {
            Progress += Runner.DeltaTime * ActiveOpeners.Count;
            if (Progress >= timeToOpen)
            {
                Progress = timeToOpen;
                IsOpened = true;
                foreach (var playerId in ActiveOpeners)
                {
                    var playerObj = Runner.FindObject(playerId);
                    if (playerObj != null)
                    {
                        var survivor = playerObj.GetComponent<ISurvivor>();
                        if (survivor != null) survivor.SetRepairAnimation(false);
                    }
                }

                ActiveOpeners.Clear(); 
            }
        }
    }
    private void StartOpeningLocally()
    {
        if (_localPlayer == null) return;
        _localPlayer.OnStartRepair(); 
        RPC_SetOpeningState(_localPlayer.Object.Id, true);
    }
    private void StopOpeningLocally()
    {
        if (_localPlayer == null) return;
        _localPlayer.OnStopRepair();
        RPC_SetOpeningState(_localPlayer.Object.Id, false);
    }
    [Rpc(RpcSources.All, RpcTargets.StateAuthority)]
    private void RPC_SetOpeningState(NetworkId playerId, NetworkBool isOpening)
    {
        if (IsOpened) return;
        if (isOpening)
        {
            if (!ActiveOpeners.Contains(playerId)) ActiveOpeners.Add(playerId);
        }
        else
        {
            ActiveOpeners.Remove(playerId);
        }
        var playerObj = Runner.FindObject(playerId);
        if (playerObj != null)
        {
            var survivor = playerObj.GetComponent<ISurvivor>();
            if (survivor != null)
            {
                survivor.SetRepairAnimation(isOpening);
            }
        }
    }
    private void OpenGateVisuals()
    {
        if (_isOpeningLocally)
        {
            StopOpeningLocally();
        }
        _isOpeningLocally = false;
        if (interactText) interactText.SetActive(false);
        if (progressBar) progressBar.gameObject.SetActive(false);
        if (gateBlocker != null) gateBlocker.SetActive(false);
        if (escapeZone != null) escapeZone.SetActive(true);
        if (gateAudioSource && openedSound)
        {
            gateAudioSource.Stop();
            gateAudioSource.PlayOneShot(openedSound, GetVFXVolume());
        }
        HideAuraSwitch();
    }
    private void OnTriggerEnter(Collider other)
    {
        if (IsOpened || !IsPowered) return;
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
            if (player != null && player.Object.HasInputAuthority)
            {
                _inRange = true;
                _localPlayer = player;
                _isOpeningLocally = false;
                if (interactText != null) interactText.SetActive(true);
                if (progressBar != null) progressBar.gameObject.SetActive(false);
            }
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            var player = other.GetComponent<ISurvivor>();
            if (player != null && player.Object.HasInputAuthority)
            {
                ResetInteractionLocal();
            }
        }
    }
    private void ResetInteractionLocal()
    {
        _inRange = false;
        if (_isOpeningLocally) StopOpeningLocally();
        _isOpeningLocally = false;
        _localPlayer = null;
        if (interactText != null) interactText.SetActive(false);
        if (progressBar != null) progressBar.gameObject.SetActive(false);
    }
    private void HideAuraSwitch()
    {
        if (auraSwitchObject != null) auraSwitchObject.SetActive(false);
    }
    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}