// using UnityEngine;
// using UnityEngine.VFX; 

// [RequireComponent(typeof(Rigidbody))]
// public class HammerProjectile : MonoBehaviour
// {
//     [Header("Cài Đặt Vật Lý")]
//     public float throwForce = 20f; 
//     public float lifeTime = 3f;    

//     [Header("Hiệu Ứng Particle/VFX (Prefabs)")]
//     public GameObject flyingVFXPrefab; 
//     public GameObject impactParticlePrefab; 

//     private Rigidbody rb;
//     private bool hasExploded = false; 
//     private GameObject activeFlyingVFX; 

//     void Start()
//     {
//         rb = GetComponent<Rigidbody>();
//         rb.linearVelocity = transform.forward * throwForce;
        
//         // Búa vẫn xoay tít thò lò
//         rb.angularVelocity = new Vector3(20f, 0f, 0f);

//         if (flyingVFXPrefab != null)
//         {
//             // Đẻ VFX ra, NHƯNG KHÔNG GẮN LÀM CON NỮA
//             activeFlyingVFX = Instantiate(flyingVFXPrefab, transform.position, transform.rotation);
//         }

//         Invoke(nameof(ExplodeAndDestroy), lifeTime);
//     }

//     void Update()
//     {
//         // Ép cục VFX luôn chạy theo tọa độ của búa, nhưng giữ nguyên góc xoay thẳng băng
//         if (activeFlyingVFX != null && !hasExploded)
//         {
//             activeFlyingVFX.transform.position = transform.position;
//         }
//     }

//     private void OnTriggerEnter(Collider other)
//     {
//         if (other.CompareTag("Hunter")) return;

//         if (other.CompareTag("Player"))
//         {
//             Debug.Log("Đã ném trúng đầu Player!");
//             // TODO: Trừ máu Player
//         }
        
//         ExplodeAndDestroy();
//     }

//     private void ExplodeAndDestroy()
//     {
//         if (hasExploded) return;
//         hasExploded = true;

//         CancelInvoke(nameof(ExplodeAndDestroy)); 

//         // 1. CHẠY VFX NỔ
//         if (impactParticlePrefab != null)
//         {
//             GameObject impactVFX = Instantiate(impactParticlePrefab, transform.position, transform.rotation);
//             Destroy(impactVFX, 1f); 
//         }

//         // 2. XỬ LÝ DỌN DẸP VFX BAY
//         if (activeFlyingVFX != null)
//         {
//             // Không cần hàm SetParent(null) nữa vì vốn dĩ nó đã độc lập rồi
            
//             // Tìm và ngắt phát hạt
//             ParticleSystem ps = activeFlyingVFX.GetComponentInChildren<ParticleSystem>();
//             if (ps != null) ps.Stop();

//             VisualEffect vfx = activeFlyingVFX.GetComponentInChildren<VisualEffect>();
//             if (vfx != null) vfx.Stop();

//             // Cho nó 1 giây để khói từ từ tan hết rồi mới xóa hẳn
//             Destroy(activeFlyingVFX, 1f);
            
//             // Ép biến này thành null để Update không cố chạy theo tọa độ của búa nữa
//             activeFlyingVFX = null; 
//         }

//         // 3. Xóa viên búa
//         Destroy(gameObject);
//     }
// }

using UnityEngine;
using UnityEngine.VFX;
using Fusion;

[RequireComponent(typeof(Rigidbody))]
public class HammerProjectile : NetworkBehaviour
{
    [Header("Cài Đặt Vật Lý")]
    public float throwForce = 20f; 
    public float lifeTime = 3f;    

    [Header("Hiệu Ứng Particle/VFX (Prefabs)")]
    public GameObject flyingVFXPrefab; 
    public GameObject impactParticlePrefab; 

    private Rigidbody rb;
    [Networked] private NetworkBool hasExploded { get; set; } 
    [Networked] private TickTimer lifeTimer { get; set; }

    private GameObject activeFlyingVFX; 

    public override void Spawned()
    {
        rb = GetComponent<Rigidbody>();
        rb.linearVelocity = transform.forward * throwForce;
        rb.angularVelocity = new Vector3(20f, 0f, 0f);

        if (Object.HasStateAuthority)
        {
            lifeTimer = TickTimer.CreateFromSeconds(Runner, lifeTime);
        }

        if (flyingVFXPrefab != null)
        {
            activeFlyingVFX = Instantiate(flyingVFXPrefab, transform.position, transform.rotation);
        }
    }

    public override void FixedUpdateNetwork()
    {
        if (!Object.HasStateAuthority || hasExploded) return;

        if (lifeTimer.Expired(Runner))
        {
            ExplodeAndDestroy();
        }
    }

    void Update()
    {
        if (activeFlyingVFX != null && !hasExploded)
        {
            activeFlyingVFX.transform.position = transform.position;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!Object.HasStateAuthority) return;
        if (other.CompareTag("Hunter")) return;

        if (other.CompareTag("Player")) Debug.Log("Đã ném trúng đầu Player!");
        
        ExplodeAndDestroy();
    }

    private void ExplodeAndDestroy()
    {
        if (hasExploded) return;
        hasExploded = true;

        Rpc_PlayExplosion();
        CleanUpFlyingVFX();
        
        Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayExplosion()
    {
        if (impactParticlePrefab != null)
        {
            GameObject impactVFX = Instantiate(impactParticlePrefab, transform.position, transform.rotation);
            Destroy(impactVFX, 1f); 
        }
    }

    private void CleanUpFlyingVFX()
    {
        if (activeFlyingVFX != null)
        {
            ParticleSystem ps = activeFlyingVFX.GetComponentInChildren<ParticleSystem>();
            if (ps != null) ps.Stop();
            VisualEffect vfx = activeFlyingVFX.GetComponentInChildren<VisualEffect>();
            if (vfx != null) vfx.Stop();
            Destroy(activeFlyingVFX, 1f);
            activeFlyingVFX = null; 
        }
    }
}