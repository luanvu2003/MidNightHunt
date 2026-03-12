using UnityEngine;
using UnityEngine.InputSystem;

public class AttackController : MonoBehaviour
{
    private Animator ani;
    [Header("Attack")]
    public bool isAttacking = false;
    private void Awake()
    {
        ani = GetComponent<Animator>();
    }
    void Update()
    {
        // chuot trai
        if(Mouse.current.leftButton.wasPressedThisFrame && !isAttacking)
        {
            isAttacking = true;
            ani.SetTrigger("Attack");
        }
        // chuot phai
        if(Mouse.current.rightButton.wasPressedThisFrame && !isAttacking)
        {
            isAttacking = true;
            ani.SetTrigger("Phibua");
        }
    }
    public void ResetAttack()
    {
        isAttacking = false;
    }


}
