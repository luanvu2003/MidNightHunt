using UnityEngine;

public class HunterAnimationBridge : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private AttackController attackScript;
    private void Awake()
    {
        movementScript = GetComponentInParent<HunterMovement>();
        interactionScript = GetComponentInParent<HunterInteraction>();
        attackScript = GetComponentInParent<AttackController>();
    }
    public void PlayFootstep()
    {
        if (movementScript != null) movementScript.PlayFootstep();
    }
    public void EventDapMay()
    {
        if (interactionScript != null) interactionScript.EventDapMay();
    }
    public void EventDapVan()
    {
        if (interactionScript != null) interactionScript.EventDapVan();
    }
    public void EventVault()
    {
        if (interactionScript != null) interactionScript.EventVault();
    }
    public void EventTreoMoc()
    {
        if (interactionScript != null) interactionScript.EventTreoMoc();
    }
    public void FinishInteraction()
    {
        if (interactionScript != null) interactionScript.FinishInteraction();
    }
    public void AttachPlayerToHand()
    {
        if (interactionScript != null) interactionScript.AttachPlayerToHand();
    }
    public void AttachPlayerToShoulder()
    {
        if (interactionScript != null) interactionScript.AttachPlayerToShoulder();
    }
    public void HookPlayerToHook()
    {
        if (interactionScript != null) interactionScript.HookPlayerToHook();
    }
    public void ResetAttack()
    {
        if (attackScript != null) attackScript.ResetAttack();
    }
    public void StarSlowEffect()
    {
        if (attackScript != null) attackScript.StarSlowEffect();
    }
    public void PlaySoundSwing()
    {
        if (attackScript != null) attackScript.PlaySoundSwing();
    }
    public void PlaySoundRelease()
    {
        if (attackScript != null) attackScript.PlaySoundRelease();
    }
    public void PlaySoundDatTrap()
    {
        if (attackScript != null) attackScript.PlaySoundDatTrap();
    }
    public void EnableDamageFrames()
    {
        if (attackScript != null) attackScript.EnableDamageFrames();
    }
    public void DisableDamageFrames()
    {
        if (attackScript != null) attackScript.DisableDamageFrames();
    }
    public void ReleaseHammer() 
    {
        if (attackScript != null) attackScript.ReleaseHammer();
    }
    public void SpawnTrapEvent()
    {
        if (attackScript != null) attackScript.SpawnTrapEvent();
    }
    public void StartVomitEvent() 
    {
        if (attackScript != null) attackScript.StartVomitEvent();
    }
    public void StopVomitEvent() 
    {
        if (attackScript != null) attackScript.StopVomitEvent();
    }
}