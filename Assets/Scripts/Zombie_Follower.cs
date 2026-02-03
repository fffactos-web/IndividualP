using UnityEngine;
using UnityEngine.AI;

public class Zombie_Follower : MonoBehaviour
{
    NavMeshAgent agent;
    Animator animator;
    Transform player;

    public float attackDistance = 7f;
    public float damage = 5f;

    void Start()
    {
        agent = GetComponentInParent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            player = p.transform;

        InvokeRepeating(nameof(GoToPlayer), 0f, 0.5f);
    }

    void GoToPlayer()
    {
        if (player == null) return;
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist > attackDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            animator.SetBool("isAttacking", false);
        }
        else
        {
            agent.isStopped = true;
            animator.SetBool("isAttacking", true);
        }
    }

    // вызывается анимацией
    public void Attack()
    {
        if (player == null) return;

        if (Vector3.Distance(player.position, transform.position) <= attackDistance)
        {
            var hp = player.GetComponent<Character_Properties>();
            if (hp != null)
                hp.GetDamage(damage);
        }
    }
}
