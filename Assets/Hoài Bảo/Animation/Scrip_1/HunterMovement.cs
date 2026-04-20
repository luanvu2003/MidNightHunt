using UnityEngine;
using Fusion; 
using System.Collections;

public class HunterMovement : NetworkBehaviour 
{
    public Animator animator;
    [Header("Cài Đặt Tốc Độ")]
    public float walkStraight = 5f;
    public float walkBackward = 4f;
    [Networked] public float currentSpeedMultiplier { get; set; }
    [Header("Âm Thanh")]
    public AudioSource audioSource;
    public AudioClip footstepClip;
    public AudioClip landingClip;
    [Header("Cài Đặt Rơi")]
    public float hardLandingThreshold = -12f;
    public float landSlowMult = 0.2f;
    public float landSlowDuration = 1.5f;
    private CharacterController controller;
    [Networked] private float velocityY { get; set; }
    [Networked] private float currentSpeed { get; set; }
    [Networked] private NetworkBool wasGrounded { get; set; }
    [Networked] private TickTimer slowTimer { get; set; }
    [Networked] public float AnimSpeedValue { get; set; }
    private readonly int animSpeed = Animator.StringToHash("Speed");
    public override void Spawned()
    {
        controller = GetComponent<CharacterController>();
        animator = GetComponentInChildren<Animator>();
        if (audioSource == null) audioSource = GetComponent<AudioSource>();
        if (audioSource != null)
        {
            audioSource.spatialBlend = 1f;
            audioSource.rolloffMode = AudioRolloffMode.Linear;
            audioSource.minDistance = 2f; 
            audioSource.maxDistance = 20f; 
        }
        if (controller != null)
        {
            StartCoroutine(LocalCCReset());
        }
        if (Object.HasStateAuthority)
        {
            currentSpeedMultiplier = 1f;
        }
    }
    IEnumerator LocalCCReset()
    {
        controller.enabled = false;
        yield return new WaitForFixedUpdate();
        yield return new WaitForFixedUpdate();
        if (Object.HasStateAuthority || Object.HasInputAuthority)
        {
            controller.enabled = true;
        }
    }
    public void HandleMove(Vector2 input, float camYaw)
    {
        if (controller == null || !controller.enabled) return;
        controller.enabled = false;
        controller.enabled = true;
        transform.rotation = Quaternion.Euler(0, camYaw, 0);
        if (slowTimer.Expired(Runner))
        {
            ResetSlow();
            slowTimer = TickTimer.None;
        }
        if (!wasGrounded && controller.isGrounded)
        {
            if (velocityY < hardLandingThreshold) TriggerHardLanding();
        }
        wasGrounded = controller.isGrounded;
        Vector3 move = (transform.forward * input.y + transform.right * input.x).normalized;
        float targetSpeed = 0f;
        if (input.y < 0) targetSpeed = walkBackward * currentSpeedMultiplier;
        else if (move.magnitude > 0) targetSpeed = walkStraight * currentSpeedMultiplier;
        if (controller.isGrounded && velocityY < 0) velocityY = -2f;
        velocityY += -9.81f * Runner.DeltaTime;
        currentSpeed = targetSpeed;
        controller.Move(move * (currentSpeed * Runner.DeltaTime) + new Vector3(0, velocityY, 0) * Runner.DeltaTime);
        float fakeSpeedForAnimator = currentSpeed;
        if (currentSpeedMultiplier > 0f) fakeSpeedForAnimator = currentSpeed / currentSpeedMultiplier;
        float targetAnimValue = fakeSpeedForAnimator / walkStraight;
        if (input.y < 0) targetAnimValue = -targetAnimValue;
        AnimSpeedValue = targetAnimValue;
    }
    public override void Render()
    {
        if (animator == null) return;
        float currentAnimValue = animator.GetFloat(animSpeed);
        animator.SetFloat(animSpeed, Mathf.Lerp(currentAnimValue, AnimSpeedValue, Time.deltaTime * 15f));
    }
    private void TriggerHardLanding()
    {
        if (Object.HasStateAuthority)
        {
            Rpc_PlayLandingSound();
        }
        ApplySlow(landSlowMult);
        slowTimer = TickTimer.CreateFromSeconds(Runner, landSlowDuration);
        Debug.Log("Tiếp đất mạnh! Bị làm chậm.");
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayLandingSound()
    {
        if (!Object.HasInputAuthority) return;
        if (audioSource != null && landingClip != null)
        {
            audioSource.PlayOneShot(landingClip, 1f * GetVFXVolume());
        }
    }
    public void PlayFootstep()
    {
        if (!Object.HasInputAuthority) return;
        if (controller.isGrounded && Mathf.Abs(currentSpeed) > 0.2f && footstepClip != null)
        {
            if (audioSource != null) audioSource.PlayOneShot(footstepClip, 0.6f * GetVFXVolume());
        }
    }
    public void ApplySlow(float mult) { currentSpeedMultiplier = mult; }
    public void ResetSlow() { currentSpeedMultiplier = 1f; }
    private float GetVFXVolume()
    {
        return AudioManager.Instance != null ? AudioManager.Instance.vfxVolume : 1f;
    }
}