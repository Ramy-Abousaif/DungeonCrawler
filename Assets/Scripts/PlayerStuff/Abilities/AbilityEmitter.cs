using UnityEngine;

/// <summary>
/// Interface for abilities that support continuous fire/hold mechanics.
/// Emitters implementing this can respond to player input being held and released.
/// </summary>
public interface IHoldableAbility
{
    void OnHoldStart(Abilities.Ability ability);
    void OnHoldEnd();
    bool IsHoldable { get; }
}

public abstract class AbilityEmitter : MonoBehaviour
{
    public AbilityType supportedType;
    [SerializeField] protected Transform firePoint;
    [SerializeField] public GameObject abilityEffect;
    [SerializeField] public GameObject optionalHeldItem;
    [SerializeField] protected int abilityIndex = 0;
    protected PhysicsBasedCharacterController player;
    protected Coroutine firingRoutine;
    protected bool isFiring = false;

    private void Awake()
    {
        player = GameObject.FindWithTag("Player").GetComponent<PhysicsBasedCharacterController>();
    }

    public virtual void Fire(Abilities.Ability ability)
    {
        PerformFire(ability);
    }

    protected abstract void PerformFire(Abilities.Ability ability);

    public bool Matches(AbilityType type, int index)
    {
        return supportedType == type && abilityIndex == index;
    }

    public virtual void StopFire()
    {
        if (!isFiring) return;

        isFiring = false;

        if (firingRoutine != null)
            StopCoroutine(firingRoutine);

        if (abilityEffect != null)
            abilityEffect.SetActive(false);
    }
}
