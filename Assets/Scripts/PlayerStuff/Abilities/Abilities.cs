using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public enum AbilityType
{
    MELEE,
    TRANSFORM,
    PROJECTILE,
    HITSCAN,
    BLINK,
    SUMMON,
    BARRIER,
    AURA,
    BLANK
}

public class Abilities : MonoBehaviour
{
    [SerializeField] private Animator anim;
    [SerializeField] private PhysicsBasedCharacterController controller;
    [SerializeField] private AbilityEmitter[] baseEmitters;
    [SerializeField] private AbilityEmitter[] altEmitters;

    [System.Serializable]
    public class Ability
    {
        public string stateName = "Main Attack";
        public Sprite icon;
        public int layerIndex = 1;
        public bool useAnimationEvent = true;

        [Header("Attack")]
        public AbilityType abilityType;
        [HideInInspector] public AbilityEmitter abilityEmitter;

        public float baseAbilityCooldown = 1f;
        
        public float currentAbilityCooldown = 1f;

        public float baseAbilityRange = 1f;

        public float currentAbilityRange = 1f;

        public float baseAbilitySpeed = 1f;

        public float currentAbilitySpeed = 1f;

        public float baseAbilityDamage = 10f;
        public float currentAbilityDamage = 1f;
        public bool affectedByAttackSpeed = false;
        public bool stunnable = false;
        public float stunDuration = 0f;

        [Header("Ability Behavior")]
        [Tooltip("If true, ability goes on cooldown immediately when fired. Otherwise waits for animation end.")]
        public bool startCooldownOnFire = false;
        [Tooltip("If true, ability can fire without waiting for current ability to finish.")]
        public bool isInstantCast = false;

        [HideInInspector] public float cooldownTimer = 0f;
        [HideInInspector] public AnimationClip clip = null;
        [HideInInspector] public bool isPlaying = false;
        [HideInInspector] public float playTimer = 0f;
    }

    public Ability[] abilities { get { return _currentForm.abilities; } set { _currentForm.abilities = value; } }

    private float _primaryAttackTimer = 0f;   // time elapsed in current primary attack
    private bool _attackHeld = false;

    private bool _isAttacking = false;

    [Header("Layer Weight Smoothing")]
    [SerializeField] private float layerWeightSmoothTime = 0.08f;
    private float _currentLayerWeight = 0f;
    private float _layerWeightVelocity = 0f;
    private float _targetLayerWeight = 0f;

    // cached primary clip accessor
    private AnimationClip PrimaryClip => (_currentForm.abilities != null && _currentForm.abilities.Length > 0) ? _currentForm.abilities[0].clip : null;

    [Header("Forms")]
    [SerializeField] private CharacterForm baseForm;
    [SerializeField] private CharacterForm altForm;
    [SerializeField] private GameObject renderers;
    private List<Material> mats = new List<Material>();

    [SerializeField] private CharacterForm _currentForm;
    private float _currentFormAmount;
    private Coroutine _formRoutine;

    void Awake()
    {
        for(int i = 0; i < renderers.transform.childCount; i++)
        {
            if (renderers.transform.GetChild(i).TryGetComponent(out Renderer r))
            {
                foreach(var m in r.materials)
                {
                    mats.Add(m);
                }
            }       
        }
    }

    private void Start()
    {
        if (controller == null)
            controller = GetComponent<PhysicsBasedCharacterController>();

        // initialize clips for all abilities
        for (int i = 0; i < _currentForm.abilities.Length; i++)
        {
            SetupEmitter(i);
            _currentForm.abilities[i].currentAbilityCooldown = _currentForm.abilities[i].baseAbilityCooldown;
            _currentForm.abilities[i].currentAbilityRange = _currentForm.abilities[i].baseAbilityRange;
            _currentForm.abilities[i].currentAbilitySpeed = _currentForm.abilities[i].baseAbilitySpeed;
            _currentForm.abilities[i].currentAbilityDamage = _currentForm.abilities[i].baseAbilityDamage;
            _currentForm.abilities[i].cooldownTimer = 0.0f;
            controller.Anim.SetFloat("AbilitySpeed" + (i + 1), _currentForm.abilities[i].currentAbilitySpeed);
        }
    }

    private void SetupEmitter(int i)
    {
        var emitters = _currentForm == baseForm ? baseEmitters : altEmitters;
        var ability = _currentForm.abilities[i];

        UIManager.Instance.SetupAbilityIcons(i, _currentForm.abilities[i].cooldownTimer, _currentForm.abilities[i].icon);

        if (!string.IsNullOrEmpty(_currentForm.abilities[i].stateName) && anim != null)
            _currentForm.abilities[i].clip = FindAnimation(_currentForm.abilities[i].stateName);

        foreach (var e in emitters)
        {
            if (e.Matches(ability.abilityType, i))
            {
                ability.abilityEmitter = e;
                return;
            }
        }

        Debug.LogWarning(
            $"No emitter found for ability index {i} ({ability.abilityType}) in form {_currentForm.name}"
        );
    }

    /// <summary>
    /// Reads the player attack input.
    /// </summary>
    /// <param name="context">The attack input's context.</param>
    public void Ability1InputAction(InputAction.CallbackContext context)
    {
        if (context.performed)
        {
            _attackHeld = true;
            TryUseAbility(0);
        }

        if (context.canceled)
        {
            _attackHeld = false;
        }
    }

    public void Ability2InputAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
            TryUseAbility(1);
    }

    public void Ability3InputAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
            TryUseAbility(2);
    }

    public void Ability4InputAction(InputAction.CallbackContext context)
    {
        if (!context.performed) return;
            TryUseAbility(3);
    }

    private void StartAttack(int index)
    {
        _isAttacking = true;
        if (anim != null)
        {
            // set target weight to 1; actual layer weight will interpolate in Update (primary)
            int attackLayerIndex = _currentForm.abilities[index].layerIndex;
            if (anim.layerCount > attackLayerIndex)
                _targetLayerWeight = 1f;
            anim.SetBool("isAttacking", true);
            anim.SetInteger("AbilityIndex", index);
            // replay the attack state so the animation restarts even when the bool is already true
            anim.Play(_currentForm.abilities[index].stateName, attackLayerIndex, 0f);
        }

        _primaryAttackTimer = 0f;
    }

    public void ApplyForm(CharacterForm newForm, float blendTime)
    {
        if (_formRoutine != null)
            StopCoroutine(_formRoutine);

        _formRoutine = StartCoroutine(FormRoutine(newForm, blendTime));
    }

    public void ApplyTimedForm(CharacterForm form, float blendIn, float duration, float blendOut)
    {
        StartCoroutine(TimedFormRoutine(form, blendIn, duration, blendOut));
    }

    private IEnumerator TimedFormRoutine(CharacterForm form, float blendIn, float duration, float blendOut)
    {
        ApplyForm(form, blendIn);
        yield return new WaitForSeconds(duration);
        ApplyForm(baseForm, blendOut);
    }


    private IEnumerator FormRoutine(CharacterForm newForm, float blendTime)
    {
        for(int i = 0; i < _currentForm.abilities.Length; i++)
        {
            _currentForm.abilities[i].abilityEmitter.StopFire();
        }

        _currentForm = newForm;

        float start = _currentFormAmount;
        float target = (newForm == baseForm) ? 0f : 1f;

        // === APPLY GAMEPLAY STATE IMMEDIATELY ===
        controller.CurrentMaxSpeed =
            controller.BaseMaxSpeed * newForm.moveSpeedMultiplier;

        controller.attackSpeed = newForm.damageMultiplier;
        controller.EnableFlight(newForm.canFly);

        if(newForm == altForm)
        {
            anim.SetBool("isTransformed", true);
        }
        else
        {
            anim.SetBool("isTransformed", false);
        }

        if (newForm.abilities != null)
        {
            abilities = newForm.abilities;
        }

        for(int i = 0; i < newForm.abilities.Length; i++)
        {
            SetupEmitter(i);
        }

        // === BLEND VISUALS ===
        float elapsed = 0f;

        while (elapsed < blendTime)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / blendTime;

            _currentFormAmount = Mathf.Lerp(start, target, t);
            SetFormAmount(_currentFormAmount);

            yield return null;
        }

        _currentFormAmount = target;
        SetFormAmount(target);
    }

    private void SetFormAmount(float value)
    {
        foreach (var mat in mats)
        {
            if (mat.HasProperty("_FormAmount"))
                mat.SetFloat("_FormAmount", value);
        }
    }

    public void SetFresnelAmount(float value)
    {
        foreach (var mat in mats)
        {
            if (mat.HasProperty("_FresnelAmount"))
                mat.SetFloat("_FresnelAmount", value);
        }
    }

    private void OnApplicationQuit()
    {
        SetFormAmount(0);
    }

    private void StopAttack()
    {
        _isAttacking = false;
        if (anim != null)
        {
            // set target weight to 0; actual layer weight will interpolate in Update
            int attackLayerIndex = _currentForm.abilities[0].layerIndex;
            if (anim.layerCount > attackLayerIndex)
                _targetLayerWeight = 0f;
            anim.SetBool("isAttacking", false);
        }
    }

    public void OnHit(Enemy enemy, int abilityIndex, bool direct = false)
    {
        enemy.TakeDamage(_currentForm.abilities[abilityIndex].currentAbilityDamage, direct);
        if (_currentForm.abilities[abilityIndex].stunnable)
        {
            enemy.GetComponent<AIBase>()?.StartStun(_currentForm.abilities[abilityIndex].stunDuration);
        }
    }

    // Update is called once per frame
    void Update()
    {
        float dt = Time.deltaTime;
        // Primary attack handling (hold-to-repeat)
        if (_isAttacking && anim.GetInteger("AbilityIndex") == 0)
        {
            _primaryAttackTimer += dt;
            AnimationClip clip = PrimaryClip;
            float clipLen = (clip != null && anim != null) ? clip.length / Mathf.Max(0.0001f, anim.GetFloat("AbilitySpeed1")) : 0f;
            if (_primaryAttackTimer >= clipLen)
            {
                if (_attackHeld)
                {
                    _primaryAttackTimer = 0f;
                    if (anim != null)
                    {
                        int attackLayerIndex = _currentForm.abilities[0].layerIndex;
                        if (anim.layerCount > attackLayerIndex)
                        {
                            anim.Play(_currentForm.abilities[0].stateName, attackLayerIndex, 0f);
                        }
                    }
                }
                else
                {
                    StopAttack();
                }
            }
        }

        // Holdable/continuous abilities: fire while held
        if (_attackHeld && abilities[0].abilityEmitter is IHoldableAbility holdable && holdable.IsHoldable)
        {
            holdable.OnHoldStart(abilities[0]);
        }
        else if (!_attackHeld)
        {
            // stop holding
            if (abilities[0].abilityEmitter is IHoldableAbility holdable2)
            {
                holdable2.OnHoldEnd();
            }
        }

        // Other abilities: update cooldowns and playing timers
        if (_currentForm.abilities != null)
        {
            for (int i = 1; i < _currentForm.abilities.Length; i++)
            {
                UIManager.Instance.UpdateCooldown(i, _currentForm.abilities[i].cooldownTimer, _currentForm.abilities[i].currentAbilityCooldown);
                var a = _currentForm.abilities[i];
                if (a == null) continue;
                if (a.cooldownTimer > 0f)
                    a.cooldownTimer = Mathf.Max(0f, a.cooldownTimer - dt);

                if (a.isPlaying)
                {
                    if(a.clip != null)
                        Debug.Log("PLAYING ANIMATION " + a.clip.name);

                    anim.SetLayerWeight(a.layerIndex, 1f);

                    a.playTimer += dt;
                    float clipLen = (a.clip != null && anim != null) ? a.clip.length / anim.GetFloat("AbilitySpeed" + (i + 1)) : 0f;
                    if (a.playTimer >= clipLen)
                    {
                        StopAttack();
                        a.isPlaying = false;
                        a.playTimer = 0f;
                        // start cooldown only after the animation has finished (if not already started)
                        if (!a.startCooldownOnFire)
                            a.cooldownTimer = a.currentAbilityCooldown;
                        if (anim != null && anim.layerCount > a.layerIndex)
                            anim.SetLayerWeight(a.layerIndex, 0f);
                    }
                }
            }
        }

        // interpolate layer weight towards target
        // interpolate primary layer weight towards target
        if (_currentForm.abilities != null && _currentForm.abilities.Length > 0)
        {
            int attackLayerIndex = _currentForm.abilities[0].layerIndex;
            if (anim != null && anim.layerCount > attackLayerIndex)
            {
                _currentLayerWeight = Mathf.SmoothDamp(_currentLayerWeight, _targetLayerWeight, ref _layerWeightVelocity, layerWeightSmoothTime);
                anim.SetLayerWeight(attackLayerIndex, _currentLayerWeight);
                anim.SetBool("isAttacking", _isAttacking);
            }
        }
    }
    
    public AnimationClip FindAnimation (string name) 
    {
        foreach (AnimationClip clip in anim.runtimeAnimatorController.animationClips)
        {
            if (clip.name == name)
            {
                return clip;
            }
        }

        return null;
    }

    private void TryUseAbility(int index)
    {
        if (_currentForm.abilities == null || index < 0 || index >= _currentForm.abilities.Length) return;
        var a = _currentForm.abilities[index];
        if (a == null) return;

        if (index == 0)
        {
            _attackHeld = true;

            if (!_isAttacking)
            {
                StartAttack(0);
            }

            // Fire immediately ONLY if this primary does NOT rely on animation
            if (!a.useAnimationEvent)
            {
                a.abilityEmitter?.Fire(a);
            }

            return;
        }

        if (a.isInstantCast)
        {
            if (a.cooldownTimer > 0f)
                return;

            a.abilityEmitter.Fire(a);

            // START COOLDOWN IMMEDIATELY
            a.cooldownTimer = a.currentAbilityCooldown;

            return;
        }

        // For non-primary abilities: block if already playing, on cooldown, or currently attacking
        if (a.isPlaying || a.cooldownTimer > 0f || _isAttacking) return;

        // start ability playback (cooldown timing depends on startCooldownOnFire flag)
        a.isPlaying = true;
        a.playTimer = 0f;

        // if this ability should start cooldown immediately, do so now
        if (a.startCooldownOnFire)
            a.cooldownTimer = a.currentAbilityCooldown;

        if (anim != null)
        {
            if (anim.layerCount > a.layerIndex)
                anim.SetLayerWeight(a.layerIndex, 1f);
            if (!string.IsNullOrEmpty(a.stateName))
                anim.Play(a.stateName, a.layerIndex, 0f);
        }
        // Start the shared attack state
        StartAttack(index);
    }
}
