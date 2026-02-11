using System.Collections;
using UnityEngine;
using UnityEngine.AI;

public class AIBase : MonoBehaviour
{
    public enum State { Chase, Windup, Attack, Stunned }

    [Header("References")]
    [SerializeField] protected Character selfCharacter;
    [SerializeField] protected Transform target;
    [HideInInspector] protected Character targetCharacter;
    [SerializeField] protected NavMeshAgent agent;
    [SerializeField] protected Animator anim;

    [Header("Ranges")]
    [SerializeField] protected float chaseRange = 30f;
    [SerializeField] protected float attackRange = 2.2f;

    [Header("Line of Sight")]
    [SerializeField] protected Transform losOrigin;
    [SerializeField] protected LayerMask losBlockMask;
    [SerializeField] protected float losMaxDistance = 50f;

    [Header("Attack Settings")]
    [SerializeField] protected Faction faction = Faction.Player;
    [SerializeField] protected float windupTime = 0.3f;
    protected AIAttack[] attacks;
    private Coroutine windupRoutine;
    private Coroutine stunRoutine;

    [Header("Rotation")]
    [SerializeField] protected float faceTargetSpeed = 12f;

    [Header("Targeting")]
    [SerializeField] private bool autoAcquireTarget = true;
    [SerializeField] private float targetRefreshInterval = 0.25f;
    [SerializeField] private float targetSearchRange = 50f;

    private float targetRefreshTimer;

    protected State state;
    public Transform Target => target;
    public Animator Anim => anim;
    public Character SelfCharacter => selfCharacter;
    public float AttackRange => attackRange;

    protected virtual void Awake()
    {
        if (selfCharacter == null) selfCharacter = GetComponent<Character>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (anim == null) anim = GetComponentInChildren<Animator>();

        if (losOrigin == null) losOrigin = transform;
        
        RefreshTarget(Mathf.Infinity);

        attacks = GetComponents<AIAttack>();
    }

    public static Vector3 GetLeadDirection(Vector3 shooterPos, Vector3 targetPos, Vector3 targetVelocity, float projectileSpeed)
    {
        Vector3 toTarget = targetPos - shooterPos;

        float a = Vector3.Dot(targetVelocity, targetVelocity) - projectileSpeed * projectileSpeed;
        float b = 2f * Vector3.Dot(targetVelocity, toTarget);
        float c = Vector3.Dot(toTarget, toTarget);

        // If a is ~0 then speeds are similar; avoid divide by zero
        if (Mathf.Abs(a) < 0.0001f)
            return toTarget.normalized;

        float discriminant = b * b - 4f * a * c;

        // No real solution => can't intercept, just shoot at current position
        if (discriminant < 0f)
            return toTarget.normalized;

        float sqrt = Mathf.Sqrt(discriminant);

        // Two possible times
        float t1 = (-b - sqrt) / (2f * a);
        float t2 = (-b + sqrt) / (2f * a);

        // pick the smallest positive time
        float t = Mathf.Min(t1, t2);
        if (t < 0f) t = Mathf.Max(t1, t2);
        if (t < 0f) return toTarget.normalized;

        Vector3 aimPoint = targetPos + targetVelocity * t;
        Debug.DrawRay(shooterPos, aimPoint - shooterPos, Color.green);
        return (aimPoint - shooterPos).normalized;
    }

    public bool TryGetTargetVelocity(out Vector3 vel)
    {
        vel = Vector3.zero;

        if (targetCharacter == null) return false;

        if (targetCharacter.TryGetComponent(out Rigidbody rb))
        {
            vel = rb.linearVelocity;
            return true;
        }

        if (targetCharacter.TryGetComponent(out NavMeshAgent agent))
        {
            vel = agent.velocity;
            return true;
        }

        return false;
    }

    protected virtual void Update()
    {
        if (autoAcquireTarget)
        {
            targetRefreshTimer -= Time.deltaTime;
            if (targetRefreshTimer <= 0f || target == null)
            {
                targetRefreshTimer = targetRefreshInterval;
                RefreshTarget();
            }
        }

        if (target == null) return;

        if (attacks != null)
        {
            float dt = Time.deltaTime;
            for (int i = 0; i < attacks.Length; i++)
                if (attacks[i] != null)
                    attacks[i].Tick(dt);
        }

        if (state == State.Stunned)
            return;

        float dist = Vector3.Distance(transform.position, target.position);

        if (dist > chaseRange)
        {
            // optional: idle here
            agent.isStopped = true;
            if (anim) anim.SetBool("Moving", false);
            return;
        }

        bool inAttackRange = dist <= attackRange;
        bool hasLOS = HasLineOfSight();

        if (inAttackRange && hasLOS && HasAnyReadyAttack() && state != State.Windup)
        {
            StartWindup();
            return;
        }

        if (state != State.Windup)
            UpdateChase();
    }

    protected virtual void RefreshTarget(float overrideRange = -1f)
    {
        float range = overrideRange > 0f ? overrideRange : targetSearchRange;
        var best = AITargetManager.GetBestTarget(transform.position, faction, range);

        if (best != null)
        {
            target = best.transform;
            targetCharacter = target.GetComponent<Character>();
        }
    }

    protected virtual void StartWindup()
    {
        if (windupRoutine != null) return;
        windupRoutine = StartCoroutine(WindupRoutine());
    }

    public virtual void StartStun(float duration)
    {
        if (stunRoutine != null) return;
        stunRoutine = StartCoroutine(StunRoutine(duration));
    }

    protected virtual void UpdateChase()
    {
        state = State.Chase;
        agent.isStopped = false;
        agent.SetDestination(target.position);

        if (anim)
            anim.SetBool("Moving", agent.velocity.sqrMagnitude > 0.1f);
    }

    protected bool HasAnyReadyAttack()
    {
        if (attacks == null) return false;
        for (int i = 0; i < attacks.Length; i++)
            if (attacks[i] != null && attacks[i].CanUse(this))
                return true;
        return false;
    }

    protected virtual IEnumerator WindupRoutine()
    {
        state = State.Windup;
        agent.isStopped = true;

        if (anim)
        {
            anim.SetBool("Moving", false);
            anim.SetTrigger("Windup");
        }

        float t = 0f;
        while (t < windupTime)
        {
            if (state == State.Stunned) // safety bail
                yield break;

            t += Time.deltaTime;
            FaceTarget();
            yield return null;
        }

        // re-check before attacking
        float dist = Vector3.Distance(transform.position, target.position);
        if (dist <= attackRange && HasLineOfSight())
        {
            state = State.Attack;
            TryAttack();
        }

        // end windup
        state = State.Chase;
        windupRoutine = null;
    }

    protected virtual void TryAttack()
    {
        if (attacks == null || attacks.Length == 0) return;

        for (int i = 0; i < attacks.Length; i++)
        {
            if (attacks[i] == null) continue;
            if (!attacks[i].CanUse(this)) continue;

            attacks[i].Execute(this);
            return;
        }
    }

    protected virtual bool HasLineOfSight()
    {
        Vector3 origin = losOrigin.position;
        Vector3 dir = (target.position + Vector3.up * 1f) - origin;

        if (dir.sqrMagnitude < 0.01f) return true;
        if (dir.magnitude > losMaxDistance) return false;

        if (Physics.Raycast(origin, dir.normalized, out RaycastHit hit, dir.magnitude, losBlockMask))
        {
            // blocked by environment
            return false;
        }

        return true;
    }

    protected virtual void FaceTarget()
    {
        Vector3 dir = target.position - transform.position;
        dir.y = 0f;
        if (dir.sqrMagnitude < 0.0001f) return;

        Quaternion rot = Quaternion.LookRotation(dir);
        transform.rotation = Quaternion.Slerp(transform.rotation, rot, Time.deltaTime * faceTargetSpeed);
    }

    private IEnumerator StunRoutine(float duration)
    {
        state = State.Stunned;
        selfCharacter.ShowStunEffect(true);
        agent.isStopped = true;

        if (anim)
        {
            anim.SetBool("Moving", false);
            anim.SetTrigger("Stunned");
        }

        yield return new WaitForSeconds(duration);

        state = State.Chase;
        selfCharacter.ShowStunEffect(false);
        stunRoutine = null;
    }
}