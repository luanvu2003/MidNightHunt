using System.Collections.Generic;
using UnityEngine;
using Fusion;
public class MeleeHitbox : NetworkBehaviour
{
    [Header("Cài đặt Sát thương")]
    public int damageAmount = 1;
    private Collider hitboxCollider;
    private Dictionary<NetworkId, int> victimHitHistory = new Dictionary<NetworkId, int>();
    private AttackController ownerController;
    private void Awake()
    {
        hitboxCollider = GetComponent<Collider>();
        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
        ownerController = GetComponentInParent<AttackController>();
    }
    public void TurnOnHitbox()
    {
        hitboxCollider.enabled = true;
    }
    public void TurnOffHitbox()
    {
        hitboxCollider.enabled = false;
    }
    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (other.CompareTag("Player"))
        {
            NetworkObject victimNetObj = other.GetComponent<NetworkObject>();
            if (victimNetObj == null) return;
            int currentAttackId = ownerController.attackCounter;
            NetworkId victimId = victimNetObj.Id;
            if (!victimHitHistory.ContainsKey(victimId) || victimHitHistory[victimId] < currentAttackId)
            {
                victimHitHistory[victimId] = currentAttackId;
                Debug.Log($"<color=red>[HIT]</color> Nhát chém #{currentAttackId} trúng {other.name}");
                ownerController.OnHitSuccess(other.gameObject);
            }
            else
            {
                Debug.Log($"<color=yellow>[STAY]</color> Nhát chém #{currentAttackId} đang chạm {other.name} nhưng đã tính sát thương trước đó.");
            }
        }
    }
}