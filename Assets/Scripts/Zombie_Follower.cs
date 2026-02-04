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
        if (!IsInvoking(nameof(GoToPlayer)))
            InvokeRepeating(nameof(GoToPlayer), 0f, 0.5f);
    }

    void OnEnable()
    {
        if (agent == null)
            agent = GetComponentInParent<NavMeshAgent>();
        if (animator == null)
            animator = GetComponent<Animator>();
        if (player == null)
            player = ResolvePlayerTransform();

        if (agent != null)
        {
            agent.isStopped = false;
            agent.ResetPath();
        }

        if (animator != null)
        {
            animator.Rebind();
            animator.Update(0f);
            animator.SetBool("isAttacking", false);
        }

        if (!IsInvoking(nameof(GoToPlayer)))
            InvokeRepeating(nameof(GoToPlayer), 0f, 0.5f);
    }

    void OnDisable()
    {
        CancelInvoke(nameof(GoToPlayer));
    }

    void GoToPlayer()
    {
        if (player == null)
            player = ResolvePlayerTransform();
        if (player == null) return;
        if (agent == null) return;
        if (!agent.isOnNavMesh) return;

        float dist = Vector3.Distance(player.position, transform.position);

        if (dist > attackDistance)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);
            if (animator != null)
                animator.SetBool("isAttacking", false);
        }
        else
        {
            agent.isStopped = true;
            if (animator != null)
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
            if (hp == null)
                hp = player.GetComponentInChildren<Character_Properties>();
            if (hp != null)
                hp.GetDamage(damage);
        }
    }

    Transform ResolvePlayerTransform()
    {
        GameObject p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
            return p.transform;

        // Fallback: in this project player isn't always tagged as "Player".
        // Character_Properties exists only on the player.
        var props = FindAnyCharacterProperties();
        if (props != null)
            return props.transform;

        return null;
    }

    static Character_Properties FindAnyCharacterProperties()
    {
#if UNITY_2023_1_OR_NEWER
        return Object.FindFirstObjectByType<Character_Properties>();
#else
        return Object.FindObjectOfType<Character_Properties>();
#endif
    }
}
