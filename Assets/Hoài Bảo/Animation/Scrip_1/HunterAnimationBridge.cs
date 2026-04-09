using UnityEngine;

public class HunterAnimationBridge : MonoBehaviour
{
    private HunterMovement movementScript;
    private HunterInteraction interactionScript;
    private AttackController attackScript;

    private void Awake()
    {
        // Tự động tìm các script đang gắn ở cục cha (Hunter_Root)
        movementScript = GetComponentInParent<HunterMovement>();
        interactionScript = GetComponentInParent<HunterInteraction>();
        attackScript = GetComponentInParent<AttackController>();
    }

    // ==========================================
    // 1. EVENTS TỪ HUNTER MOVEMENT
    // ==========================================
    public void PlayFootstep()
    {
        if (movementScript != null) movementScript.PlayFootstep();
    }

    // ==========================================
    // 2. EVENTS TỪ HUNTER INTERACTION
    // ==========================================
    public void EventDapMay()
    {
        if (interactionScript != null) interactionScript.EventDapMay();
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

    // ==========================================
    // 3. EVENTS TỪ ATTACK CONTROLLER
    // ==========================================
    
    // --- Các sự kiện Tấn công chung ---
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

    // --- Các sự kiện riêng của từng loại Hunter ---
    public void ReleaseHammer() // Dành cho Hunter 1 (Ném búa)
    {
        if (attackScript != null) attackScript.ReleaseHammer();
    }

    public void SpawnTrapEvent() // Dành cho Hunter 2 (Đặt bẫy)
    {
        if (attackScript != null) attackScript.SpawnTrapEvent();
    }

    public void StartVomitEvent() // Dành cho Hunter 3 (Ói độc)
    {
        if (attackScript != null) attackScript.StartVomitEvent();
    }

    public void StopVomitEvent() // Dành cho Hunter 3 (Ói độc)
    {
        if (attackScript != null) attackScript.StopVomitEvent();
    }
}