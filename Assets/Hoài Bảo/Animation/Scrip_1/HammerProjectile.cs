using UnityEngine;
using UnityEngine.VFX;
using Fusion;

[RequireComponent(typeof(Rigidbody))]
public class HammerProjectile : NetworkBehaviour
{
    [Header("Cài Đặt Vật Lý")]
    public float throwForce = 20f;
    public float lifeTime = 20f;
    [Header("Hiệu Ứng Particle/VFX (Prefabs)")]
    public GameObject flyingVFXPrefab;
    public GameObject impactParticlePrefab;
    private Rigidbody rb;
    [Networked] private NetworkBool hasExploded { get; set; }
    [Networked] private TickTimer lifeTimer { get; set; }
    [Networked] private TickTimer safeTimer { get; set; }
    private GameObject activeFlyingVFX;
    private bool isReady = false;
    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        if (Object.HasStateAuthority)
        {
            rb.linearVelocity = transform.forward * throwForce;
            rb.angularVelocity = new Vector3(20f, 0f, 0f);
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
            safeTimer = TickTimer.CreateFromSeconds(Runner, 0.2f); // 0.2s đầu để bay thoát khỏi người ném
        }

        if (flyingVFXPrefab != null)
        {
            activeFlyingVFX = Instantiate(flyingVFXPrefab, transform.position, transform.rotation);
        }

        isReady = true;
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || hasExploded) return;
        if (lifeTimer.Expired(Runner))
        {
            ExplodeAndDestroy();
        }
        SyncVFXPosition();
    }
    void LateUpdate()
    {
        SyncVFXPosition();
    }

    private void SyncVFXPosition()
    {
        if (activeFlyingVFX != null && !hasExploded)
        {
            activeFlyingVFX.transform.position = transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isReady || Object == null || !Object.IsValid) return;
        if (!Object.HasStateAuthority || hasExploded) return;
        if (other.isTrigger) return;
        if (safeTimer.IsRunning)
        {
            if (other.CompareTag("Hunter") || other.transform.root.CompareTag("Hunter")) return;
        }
        Debug.Log("💥 Búa va chạm và nổ tại: " + other.name);
        ExplodeAndDestroy();
    }
    private void ExplodeAndDestroy()
    {
        if (hasExploded) return;
        hasExploded = true;
        if (Runner != null && Object != null)
            Runner.Despawn(Object);
    }
    public override void Despawned(NetworkRunner runner, bool hasState)
    {
        if (impactParticlePrefab != null)
        {
            GameObject impactVFX = Instantiate(impactParticlePrefab, transform.position, transform.rotation);
            Destroy(impactVFX, 1.5f);
        }
        if (activeFlyingVFX != null)
        {
            var renderers = activeFlyingVFX.GetComponentsInChildren<Renderer>();
            foreach (var r in renderers)
            {
                if (!(r is ParticleSystemRenderer)) r.enabled = false;
            }
            var particles = activeFlyingVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles) ps.Stop();
            var vfxs = activeFlyingVFX.GetComponentsInChildren<VisualEffect>();
            foreach (var vfx in vfxs) vfx.Stop();
            Destroy(activeFlyingVFX, 1.5f);
            activeFlyingVFX = null;
        }
    }
}