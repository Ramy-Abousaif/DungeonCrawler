using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>
/// A floating-capsule oriented physics based character controller. Based on the approach devised by Toyful Games for Very Very Valet.
/// </summary>
public class PhysicsBasedCharacterController : Character
{
    [Header("Player Specific Stats")]
    public List<ItemList> items = new List<ItemList>();
    public float attackSpeed = 2f;
    public Abilities abilities;

    private Rigidbody _rb;
    private Vector3 _gravitationalForce;
    private Vector3 _rayDir = Vector3.down;
    private Vector3 _previousVelocity = Vector3.zero;
    private Vector2 _moveContext;
    private bool movementEnabled = true;

    [Header("Other:")]
    [SerializeField] private bool _adjustInputsToCameraAngle = false;
    [SerializeField] private LayerMask _terrainLayer;
    [SerializeField] private Animator anim;
    public Animator Anim { get { return anim; } set { anim = value; } }

    [Header("Animation Smoothing:")]
    [SerializeField] private float _forwardSpeedSmoothTime = 0.08f;
    private float _forwardSpeedVelocity = 0f;
    private float _currentForwardSpeed = 0f;

    private bool _shouldMaintainHeight = true;

    [Header("Height Spring:")]
    [SerializeField] private float _rideHeight = 1.75f; // rideHeight: desired distance to ground (Note, this is distance from the original raycast position (currently centre of transform)). 
    [SerializeField] private float _rayToGroundLength = 3f; // rayToGroundLength: max distance of raycast to ground (Note, this should be greater than the rideHeight).
    [SerializeField] public float _rideSpringStrength = 50f; // rideSpringStrength: strength of spring. (?)
    [SerializeField] private float _rideSpringDamper = 5f; // rideSpringDampener: dampener of spring. (?)
    [SerializeField] private Oscillator _squashAndStretchOcillator;


    private enum lookDirectionOptions { velocity, acceleration, moveInput };
    private Quaternion _uprightTargetRot = Quaternion.identity; // Adjust y value to match the desired direction to face.
    private Quaternion _lastTargetRot;
    private Vector3 _platformInitRot;
    private bool didLastRayHit;

    [Header("Upright Spring:")]
    [SerializeField] private lookDirectionOptions _characterLookDirection = lookDirectionOptions.velocity;
    [SerializeField] private float _uprightSpringStrength = 40f;
    [SerializeField] private float _uprightSpringDamper = 5f;


    private Vector3 _moveInput;
    private float _speedFactor = 1f;
    private float _maxAccelForceFactor = 1f;
    private Vector3 _m_GoalVel = Vector3.zero;
    
    [Header("Interaction")]
    [Tooltip("Radius around the interact point to overlap-check")]
    [SerializeField] private float interactRadius = 0.6f;
    [Tooltip("Layer mask for interactable objects (optional)")]
    [SerializeField] private LayerMask interactLayerMask = ~0;

    // Tracks objects that have been temporarily switched to the "SelectedInteractable" layer
    private HashSet<GameObject> _selectedObjects = new HashSet<GameObject>();
    // Stores original layers for objects we change so we can restore them when they leave the overlap
    private Dictionary<GameObject, int> _originalLayers = new Dictionary<GameObject, int>();
    [Header("Movement:")]
    [SerializeField] private float _baseMaxSpeed = 8f;
    public float BaseMaxSpeed { get { return _baseMaxSpeed; } set { _baseMaxSpeed = value; } }
    private float _currentMaxSpeed = 8f;
    public float CurrentMaxSpeed { get { return _currentMaxSpeed; } set { _currentMaxSpeed = value; } }
    [SerializeField] private float _acceleration = 200f;
    [SerializeField] private float _maxAccelForce = 150f;
    [SerializeField] private float _leanFactor = 0.25f;
    [SerializeField] private AnimationCurve _accelerationFactorFromDot;
    [SerializeField] private AnimationCurve _maxAccelerationForceFactorFromDot;
    [SerializeField] private Vector3 _moveForceScale = new Vector3(1f, 0f, 1f);
    private Vector3 _lastMoveDir;


    private Vector3 _jumpInput;
    private float _timeSinceJumpPressed = 0f;
    private float _timeSinceUngrounded = 0f;
    private float _timeSinceJump = 0f;
    private bool _jumpReady = true;
    private bool _isJumping = false;
    private bool _isGrounded = false;

    [Header("Jump:")]
    [SerializeField] private float _jumpForceFactor = 10f;
    [SerializeField] private float _riseGravityFactor = 5f;
    [SerializeField] private float _fallGravityFactor = 10f; // typically > 1f (i.e. 5f).
    [SerializeField] private float _lowJumpFactor = 2.5f;
    [SerializeField] private float _jumpBuffer = 0.15f; // Note, jumpBuffer shouldn't really exceed the time of the jump.
    [SerializeField] private float _coyoteTime = 0.25f;
    [SerializeField] private int _maxExtraJumps = 0;
    private int _remainingExtraJumps = 0;

    [Header("Form / Movement Mode")]
    public float duration = 15f;
    public bool IsFlying { get; private set; }
    [SerializeField] float hoverHeight = 3f;
    [SerializeField] float hoverForce = 20f;
    [SerializeField] float hoverDamping = 2f;
    [SerializeField] float hoverBobAmount = 0.02f;
    [SerializeField] float hoverBobSpeed = 1.5f;
    [SerializeField] private float flightAcceleration = 25f;
    [SerializeField] private float flightLeanAngle = 20f;
    [SerializeField] private float flightLeanSmooth = 6f;


    protected override void Awake()
    {
        base.Awake();
    }

    /// <summary>
    /// Prepare frequently used variables.
    /// </summary>
    private void Start()
    {
        abilities = GetComponent<Abilities>();
        _currentMaxSpeed = _baseMaxSpeed;
        _rb = GetComponent<Rigidbody>();
        _gravitationalForce = Physics.gravity * _rb.mass;
        // Prevent immediate auto-jump on scene start by initializing the jump-press timer
        _timeSinceJumpPressed = _jumpBuffer;

        StartCoroutine(CallItemUpdate());
    }

    /// <summary>
    /// Use the result of a Raycast to determine if the capsules distance from the ground is sufficiently close to the desired ride height such that the character can be considered 'grounded'.
    /// </summary>
    /// <param name="rayHitGround">Whether or not the Raycast hit anything.</param>
    /// <param name="rayHit">Information about the ray.</param>
    /// <returns>Whether or not the player is considered grounded.</returns>
    private bool CheckIfGrounded(bool rayHitGround, RaycastHit rayHit)
    {
        bool grounded;
        if (rayHitGround == true)
        {
            grounded = rayHit.distance <= _rideHeight * 1.3f; // 1.3f allows for greater leniancy (as the value will oscillate about the rideHeight).
        }
        else
        {
            grounded = false;
        }
        return grounded;
    }

    public void EnableFlight(bool value)
    {
        IsFlying = value;
        _isJumping = false;
        _isGrounded = false;
        _timeSinceUngrounded = Mathf.Infinity;
        _timeSinceJump = Mathf.Infinity;
        _rb.useGravity = !value;
        _rb.freezeRotation = value;
        
        if (anim != null)
            anim.SetBool("isFlying", IsFlying);
    }

    /// <summary>
    /// Gets the look desired direction for the character to look.
    /// The method for determining the look direction is depends on the lookDirectionOption.
    /// </summary>
    /// <param name="lookDirectionOption">The factor which determines the look direction: velocity, acceleration or moveInput.</param>
    /// <returns>The desired look direction.</returns>
    private Vector3 GetLookDirection(lookDirectionOptions lookDirectionOption)
    {
        Vector3 lookDirection = Vector3.zero;
        if (lookDirectionOption == lookDirectionOptions.velocity || lookDirectionOption == lookDirectionOptions.acceleration)
        {
            Vector3 velocity = _rb.linearVelocity;
            velocity.y = 0f;
            if (lookDirectionOption == lookDirectionOptions.velocity)
            {
                lookDirection = velocity;
            }
            else if (lookDirectionOption == lookDirectionOptions.acceleration)
            {
                Vector3 deltaVelocity = velocity - _previousVelocity;
                _previousVelocity = velocity;
                Vector3 acceleration = deltaVelocity / Time.fixedDeltaTime;
                lookDirection = acceleration;
            }
        }
        else if (lookDirectionOption == lookDirectionOptions.moveInput)
        {
            lookDirection = _moveInput;
        }
        return lookDirection;
    }

    protected override void Update()
    {
        base.Update();
        UIManager.Instance.UpdateHealth(currentHealth, maxHealth);
        float targetForward = Vector3.Dot(transform.forward, _m_GoalVel) / _currentMaxSpeed;
        targetForward = Mathf.Clamp(targetForward, -1f, 1f);
        _currentForwardSpeed = Mathf.SmoothDamp(_currentForwardSpeed, targetForward, ref _forwardSpeedVelocity, _forwardSpeedSmoothTime);
        anim.SetFloat("ForwardSpeed", _currentForwardSpeed);
        anim.SetBool("IsGrounded", _isGrounded);
        anim.SetBool("JumpRequested", _isJumping);
        // Update selection highlight using the same overlap used for interaction
        UpdateSelectionOverlap();
    }

    IEnumerator CallItemUpdate()
    {
        foreach (ItemList i in items)
        {
            i.item.Update(this, i.stacks);
        }
        yield return new WaitForSeconds(1f);
        StartCoroutine(CallItemUpdate());
    }

    public void CallItemOnJump()
    {
        foreach (ItemList i in items)
        {
            i.item.OnJump(this, i.stacks);
        }
    }

    public void CallItemOnPickup()
    {
        foreach (ItemList i in items)
        {
            i.item.OnPickup(this, i.stacks);
        }
    }

    public void CallItemOnHit(Enemy enemy)
    {
        foreach (ItemList i in items)
        {
            i.item.OnHit(this, enemy, i.stacks);
        }
    }

    private bool _prevGrounded = false;
    /// <summary>
    /// Determines and plays the appropriate character sounds, particle effects, then calls the appropriate methods to move and float the character.
    /// </summary>
    private void FixedUpdate()
    {
        _moveInput = new Vector3(_moveContext.x, 0, _moveContext.y);

        if (_adjustInputsToCameraAngle)
        {
            _moveInput = AdjustInputToFaceCamera(_moveInput);
        }

        if (IsFlying)
        {
            FlyMove();
            return;
        }

        (bool rayHitGround, RaycastHit rayHit) = RaycastToGround();
        SetPlatform(rayHit);

        bool grounded = CheckIfGrounded(rayHitGround, rayHit);
        _isGrounded = grounded;
        if (grounded == true)
        {
            _timeSinceUngrounded = 0f;

            if (_timeSinceJump > 0.2f)
            {
                _isJumping = false;
            }
        }
        else
        {
            _timeSinceUngrounded += Time.fixedDeltaTime;
        }

        CharacterMove(_moveInput, rayHit);
        CharacterJump(_jumpInput, grounded, rayHit);

        if (rayHitGround && _shouldMaintainHeight)
        {
            MaintainHeight(rayHit);
        }

        Vector3 lookDirection = GetLookDirection(_characterLookDirection);
        MaintainUpright(lookDirection, rayHit);

        _prevGrounded = grounded;

        if (grounded)
        {
            _remainingExtraJumps = _maxExtraJumps;
        }
    }

    private void FlyMove()
    {
        if (cam == null)
            return;

        // -------- INPUT --------
        Vector3 input = new Vector3(_moveContext.x, 0f, _moveContext.y);

        Vector3 camForward = Vector3.ProjectOnPlane(cam.transform.forward, Vector3.up).normalized;
        Vector3 camRight   = Vector3.ProjectOnPlane(cam.transform.right,   Vector3.up).normalized;

        camForward.Normalize();
        camRight.Normalize();

        // Horizontal movement
        Vector3 horizontalMove =
            camForward * input.z +
            camRight * input.x;

        Vector3 desiredVelocity =
            horizontalMove.normalized *
            CurrentMaxSpeed *
            Mathf.Clamp01(horizontalMove.magnitude);

        // -------- SMOOTH VELOCITY --------
        _rb.linearVelocity = Vector3.MoveTowards(
            _rb.linearVelocity,
            desiredVelocity,
            flightAcceleration * Time.fixedDeltaTime
        );

        float bobOffset = Mathf.Sin(Time.time * hoverBobSpeed) * hoverBobAmount;

        float targetHeight = hoverHeight + bobOffset;

        float heightError = targetHeight - _rb.position.y;

        float upwardForce =
            heightError * hoverForce
            - _rb.linearVelocity.y * hoverDamping;

        _rb.AddForce(Vector3.up * upwardForce, ForceMode.Acceleration);

        // -------- LEAN --------
        Vector3 flatVel = _rb.linearVelocity;
        flatVel.y = 0f;

        if (flatVel.sqrMagnitude > 0.01f)
        {
            _lastMoveDir = flatVel.normalized;

            Vector3 leanAxis = Vector3.Cross(Vector3.up, _lastMoveDir);

            float speed01 = Mathf.Clamp01(flatVel.magnitude / CurrentMaxSpeed);
            float leanAmount = flightLeanAngle * speed01;

            Quaternion leanRot = Quaternion.AngleAxis(leanAmount, leanAxis);
            Quaternion targetRot =
                leanRot * Quaternion.LookRotation(_lastMoveDir, Vector3.up);

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                targetRot,
                flightLeanSmooth * Time.fixedDeltaTime
            );
        }
        else if (_lastMoveDir.sqrMagnitude > 0.001f)
        {
            Quaternion uprightRot = Quaternion.LookRotation(
                _lastMoveDir,
                Vector3.up
            );

            transform.rotation = Quaternion.Slerp(
                transform.rotation,
                uprightRot,
                flightLeanSmooth * Time.fixedDeltaTime
            );
        }
    }

    protected override void onHeal(float amount)
    {
        ShowNumber(amount, true);
    }

    protected override void Die()
    {
        currentHealth = 0f;
        // TODO: player death behavior (respawn, game over, etc.)
        Debug.Log("Player has died!");
    }

    protected override void OnDamageTaken(float damage, bool direct = false)
    {
        // Optional: show other UI feedback
        if(direct)
        {
            camShake.ShakeCamera(2f, 2f, 0.3f);
        }
    }

    public void SetExtraJumps(int amount)
    {
        _maxExtraJumps = amount;
        _remainingExtraJumps = amount;
    }

    /// <summary>
    /// Runs the same overlap used for interaction and temporarily moves hit objects to the
    /// "SelectedInteractable" layer, storing original layers so they can be restored when
    /// they leave the overlap.
    /// </summary>
    private void UpdateSelectionOverlap()
    {
        int selectedLayer = LayerMask.NameToLayer("SelectedInteractable");
        if (selectedLayer == -1)
        {
            return;
        }

        Vector3 point = transform.position;
        Collider[] hits = Physics.OverlapSphere(point, interactRadius, interactLayerMask);
        HashSet<GameObject> hitObjects = new HashSet<GameObject>();
        foreach (var hit in hits)
        {
            var go = hit.gameObject;
            hitObjects.Add(go);
            if (!_selectedObjects.Contains(go))
            {
                // store original layer if not already stored
                if (!_originalLayers.ContainsKey(go))
                    _originalLayers[go] = go.layer;

                go.layer = selectedLayer;
                go.transform.GetComponent<Renderer>().material.SetFloat("_Selected", 1);
                _selectedObjects.Add(go);
            }
        }

        // Find previously selected objects that are no longer hit and restore their layer
        var toRestore = new List<GameObject>();
        foreach (var go in _selectedObjects)
        {
            if (!hitObjects.Contains(go) || go == null)
                toRestore.Add(go);
        }

        foreach (var go in toRestore)
        {
            if (go == null)
            {
                _selectedObjects.Remove(go);
                _originalLayers.Remove(go);
                continue;
            }

            if (_originalLayers.TryGetValue(go, out int origLayer))
            {
                go.layer = origLayer;
                go.transform.GetComponent<Renderer>().material.SetFloat("_Selected", 0);
                _originalLayers.Remove(go);
            }
            else
            {
                int interactLayer = LayerMask.NameToLayer("Interactable");
                if (interactLayer != -1)
                {
                    go.transform.GetComponent<Renderer>().material.SetFloat("_Selected", 0);
                    go.layer = interactLayer; // best-effort fallback   
                }
            }
            _selectedObjects.Remove(go);
        }
    }

    /// <summary>
    /// Perfom raycast towards the ground.
    /// </summary>
    /// <returns>Whether the ray hit the ground, and information about the ray.</returns>
    private (bool, RaycastHit) RaycastToGround()
    {
        RaycastHit rayHit;
        Ray rayToGround = new Ray(transform.position, _rayDir);
        bool rayHitGround = Physics.Raycast(rayToGround, out rayHit, _rayToGroundLength, _terrainLayer.value);
        //Debug.DrawRay(transform.position, _rayDir * _rayToGroundLength, Color.blue);
        return (rayHitGround, rayHit);
    }

    /// <summary>
    /// Determines the relative velocity of the character to the ground beneath,
    /// Calculates and applies the oscillator force to bring the character towards the desired ride height.
    /// Additionally applies the oscillator force to the squash and stretch oscillator, and any object beneath.
    /// </summary>
    /// <param name="rayHit">Information about the RaycastToGround.</param>
    private void MaintainHeight(RaycastHit rayHit)
    {
        Vector3 vel = _rb.linearVelocity;
        Vector3 otherVel = Vector3.zero;
        Rigidbody hitBody = rayHit.rigidbody;
        if (hitBody != null)
        {
            otherVel = hitBody.linearVelocity;
        }
        float rayDirVel = Vector3.Dot(_rayDir, vel);
        float otherDirVel = Vector3.Dot(_rayDir, otherVel);

        float relVel = rayDirVel - otherDirVel;
        float currHeight = rayHit.distance - _rideHeight;
        float springForce = (currHeight * _rideSpringStrength) - (relVel * _rideSpringDamper);
        Vector3 maintainHeightForce = - _gravitationalForce + springForce * Vector3.down;
        Vector3 oscillationForce = springForce * Vector3.down;
        _rb.AddForce(maintainHeightForce);
        _squashAndStretchOcillator.ApplyForce(oscillationForce);
        //Debug.DrawLine(transform.position, transform.position + (_rayDir * springForce), Color.yellow);

        // Apply force to objects beneath
        if (hitBody != null)
        {
            hitBody.AddForceAtPosition(-maintainHeightForce, rayHit.point);
        }
    }

    /// <summary>
    /// Determines the desired y rotation for the character, with account for platform rotation.
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CalculateTargetRotation(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        if (didLastRayHit)
        {
            _lastTargetRot = _uprightTargetRot;
            try
            {
                _platformInitRot = transform.parent.rotation.eulerAngles;
            }
            catch
            {
                _platformInitRot = Vector3.zero;
            }
        }
        if (rayHit.rigidbody == null)
        {
            didLastRayHit = true;
        }
        else
        {
            didLastRayHit = false;
        }

        if (yLookAt != Vector3.zero)
        {
            _uprightTargetRot = Quaternion.LookRotation(yLookAt, Vector3.up);
            _lastTargetRot = _uprightTargetRot;
            try
            {
                _platformInitRot = transform.parent.rotation.eulerAngles;
            }
            catch
            {
                _platformInitRot = Vector3.zero;
            }
        }
        else
        {
            try
            {
                Vector3 platformRot = transform.parent.rotation.eulerAngles;
                Vector3 deltaPlatformRot = platformRot - _platformInitRot;
                float yAngle = _lastTargetRot.eulerAngles.y + deltaPlatformRot.y;
                _uprightTargetRot = Quaternion.Euler(new Vector3(0f, yAngle, 0f));
            }
            catch
            {

            }
        }
    }

    public void SetMovementEnabled(bool enabled)
    {
        movementEnabled = enabled;
    }

    /// <summary>
    /// Adds torque to the character to keep the character upright, acting as a torsional oscillator (i.e. vertically flipped pendulum).
    /// </summary>
    /// <param name="yLookAt">The input look rotation.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void MaintainUpright(Vector3 yLookAt, RaycastHit rayHit = new RaycastHit())
    {
        CalculateTargetRotation(yLookAt, rayHit);

        Quaternion currentRot = transform.rotation;
        Quaternion toGoal = MathsUtils.ShortestRotation(_uprightTargetRot, currentRot);

        Vector3 rotAxis;
        float rotDegrees;

        toGoal.ToAngleAxis(out rotDegrees, out rotAxis);
        rotAxis.Normalize();

        float rotRadians = rotDegrees * Mathf.Deg2Rad;

        _rb.AddTorque((rotAxis * (rotRadians * _uprightSpringStrength)) - (_rb.angularVelocity * _uprightSpringDamper));
    }

    /// <summary>
    /// Reads the player movement input.
    /// </summary>
    /// <param name="context">The move input's context.</param>
    public void MoveInputAction(InputAction.CallbackContext context)
    {
        if(!movementEnabled)
            return;
            
        _moveContext = context.ReadValue<Vector2>();
    }

    /// <summary>
    /// Reads the player jump input.
    /// </summary>
    /// <param name="context">The jump input's context.</param>
    public void JumpInputAction(InputAction.CallbackContext context)
    {
        float jumpContext = context.ReadValue<float>();
        _jumpInput = new Vector3(0, jumpContext, 0);

        if (context.started) // button down
        {
            _timeSinceJumpPressed = 0f;
        }
    }

    /// <summary>
    /// Called by the InputSystem when the interact button is used.
    /// Performs an overlap in front of the player and calls Interact on found targets.
    /// </summary>
    public void InteractInputAction(InputAction.CallbackContext context)
    {
        if (context.started)
        {
            PerformInteract();
        }
    }

    private void PerformInteract()
    {
        Vector3 point = transform.position;

        Collider[] hits = Physics.OverlapSphere(point, interactRadius, interactLayerMask);
        foreach (var hit in hits)
        {
            var interactable = hit.GetComponent<Interactable>();
            if (interactable != null)
            {
                interactable.OnInteract(this);
            }
        }
    }

    /// <summary>
    /// Adjusts the input, so that the movement matches input regardless of camera rotation.
    /// </summary>
    /// <param name="moveInput">The player movement input.</param>
    /// <returns>The camera corrected movement input.</returns>
    private Vector3 AdjustInputToFaceCamera(Vector3 moveInput)
    {
        float facing = cam.transform.eulerAngles.y;
        return (Quaternion.Euler(0, facing, 0) * moveInput);
    }

    /// <summary>
    /// Set the transform parent to be the result of RaycastToGround.
    /// If the raycast didn't hit, then unset the transform parent.
    /// </summary>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void SetPlatform(RaycastHit rayHit)
    {
        try
        {
            RigidPlatform rigidPlatform = rayHit.transform.GetComponent<RigidPlatform>();
            RigidParent rigidParent = rigidPlatform.rigidParent;
            transform.SetParent(rigidParent.transform);
        }
        catch
        {
            transform.SetParent(null);
        }
    }

    /// <summary>
    /// Apply forces to move the character up to a maximum acceleration, with consideration to acceleration graphs.
    /// </summary>
    /// <param name="moveInput">The player movement input.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CharacterMove(Vector3 moveInput, RaycastHit rayHit)
    {
        Vector3 m_UnitGoal = moveInput;
        Vector3 unitVel = _m_GoalVel.normalized;
        float velDot = Vector3.Dot(m_UnitGoal, unitVel);
        float accel = _acceleration * _accelerationFactorFromDot.Evaluate(velDot);
        Vector3 goalVel = m_UnitGoal * _currentMaxSpeed * _speedFactor;
        _m_GoalVel = Vector3.MoveTowards(_m_GoalVel,
                                        goalVel,
                                        accel * Time.fixedDeltaTime);
        Vector3 neededAccel = (_m_GoalVel - _rb.linearVelocity) / Time.fixedDeltaTime;
        float maxAccel = _maxAccelForce * _maxAccelerationForceFactorFromDot.Evaluate(velDot) * _maxAccelForceFactor;
        neededAccel = Vector3.ClampMagnitude(neededAccel, maxAccel);
        _rb.AddForceAtPosition(Vector3.Scale(neededAccel * _rb.mass, _moveForceScale), transform.position + new Vector3(0f, transform.localScale.y * _leanFactor, 0f)); // Using AddForceAtPosition in order to both move the player and cause the play to lean in the direction of input.
    }

    /// <summary>
    /// Apply force to cause the character to perform a single jump, including coyote time and a jump input buffer.
    /// </summary>
    /// <param name="jumpInput">The player jump input.</param>
    /// <param name="grounded">Whether or not the player is considered grounded.</param>
    /// <param name="rayHit">The rayHit towards the platform.</param>
    private void CharacterJump(Vector3 jumpInput, bool grounded, RaycastHit rayHit)
    {
        _timeSinceJumpPressed += Time.fixedDeltaTime;
        _timeSinceJump += Time.fixedDeltaTime;
        if (_rb.linearVelocity.y < 0)
        {
            _shouldMaintainHeight = true;
            _jumpReady = true;
            if (!grounded)
            {
                // Increase downforce for a sudden plummet.
                _rb.AddForce(_gravitationalForce * (_fallGravityFactor - 1f)); // Hmm... this feels a bit weird. I want a reactive jump, but I don't want it to dive all the time...
            }
        }
        else if (_rb.linearVelocity.y > 0)
        {
            if (!grounded)
            {
                if (_isJumping)
                {
                    _rb.AddForce(_gravitationalForce * (_riseGravityFactor - 1f));
                }
                if (jumpInput == Vector3.zero)
                {
                    // Impede the jump height to achieve a low jump.
                    _rb.AddForce(_gravitationalForce * (_lowJumpFactor - 1f));
                }
            }
        }

        if (_timeSinceJumpPressed < _jumpBuffer)
        {
            bool canGroundJump = _timeSinceUngrounded < _coyoteTime;
            bool canAirJump = !grounded && _remainingExtraJumps > 0;

            if ((canGroundJump || canAirJump) && _jumpReady)
            {
                _jumpReady = false;
                _shouldMaintainHeight = false;
                _isJumping = true;

                CallItemOnJump();

                if (canAirJump)
                {
                    _remainingExtraJumps--;
                }

                // Reset vertical velocity for consistent jump height
                _rb.linearVelocity = new Vector3(
                    _rb.linearVelocity.x,
                    0f,
                    _rb.linearVelocity.z
                );

                if (canGroundJump && rayHit.distance != 0)
                {
                    _rb.position = new Vector3(
                        _rb.position.x,
                        _rb.position.y - (rayHit.distance - _rideHeight),
                        _rb.position.z
                    );
                }

                _rb.AddForce(Vector3.up * _jumpForceFactor, ForceMode.Impulse);

                _timeSinceJumpPressed = _jumpBuffer;
                _timeSinceJump = 0f;
            }
        }
    }
}