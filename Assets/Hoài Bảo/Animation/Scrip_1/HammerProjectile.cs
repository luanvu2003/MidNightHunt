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
            // Spawn VFX tại đúng vị trí búa hiện tại
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
        
        // Đồng bộ vị trí VFX trong nhịp mạng
        SyncVFXPosition();
    }

    // 🚨 Dùng LateUpdate để đảm bảo VFX bám sát búa sau khi Physics đã di chuyển búa
    void LateUpdate()
    {
        SyncVFXPosition();
    }

    private void SyncVFXPosition()
    {
        if (activeFlyingVFX != null && !hasExploded)
        {
            activeFlyingVFX.transform.position = transform.position;
            // Nếu bạn muốn khói cũng xoay theo búa thì bật dòng dưới, 
            // nếu muốn khói bay thẳng thì để im.
            // activeFlyingVFX.transform.rotation = transform.rotation; 
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isReady || Object == null || !Object.IsValid) return;
        if (!Object.HasStateAuthority || hasExploded) return;

        // 1. Xuyên qua các vùng trigger tàng hình
        if (other.isTrigger) return;

        // 2. Chống nổ ngay tại tay Hunter ném (Dựa trên Tag hoặc Root)
        if (safeTimer.IsRunning)
        {
            if (other.CompareTag("Hunter") || other.transform.root.CompareTag("Hunter")) return;
        }

        // 🚨 CHẠM LÀ NỔ: Bất kể là Player, Tường, Nhà, hay Đất...
        Debug.Log("💥 Búa va chạm và nổ tại: " + other.name);
        ExplodeAndDestroy();
    }

    private void ExplodeAndDestroy()
    {
        if (hasExploded) return;
        hasExploded = true;

        Rpc_PlayExplosionAndCleanUp();
        
        if (Runner != null && Object != null)
            Runner.Despawn(Object);
    }

    [Rpc(RpcSources.StateAuthority, RpcTargets.All)]
    private void Rpc_PlayExplosionAndCleanUp()
    {
        if (impactParticlePrefab != null)
        {
            GameObject impactVFX = Instantiate(impactParticlePrefab, transform.position, transform.rotation);
            Destroy(impactVFX, 1.5f); 
        }

        if (activeFlyingVFX != null)
        {
            // Ngắt các nguồn phát hạt để khói tan từ từ
            var particles = activeFlyingVFX.GetComponentsInChildren<ParticleSystem>();
            foreach (var ps in particles) ps.Stop();

            var vfxs = activeFlyingVFX.GetComponentsInChildren<VisualEffect>();
            foreach (var vfx in vfxs) vfx.Stop();

            Destroy(activeFlyingVFX, 1.5f);
            activeFlyingVFX = null; 
        }
    }
}