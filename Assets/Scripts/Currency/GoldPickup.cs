using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class GoldPickup : MonoBehaviour
{
    [Header("Value")]
    [SerializeField] [Min(1)] private int goldAmount = 1;

    [Header("Spawn")]
    [Tooltip("Height offset from world spawn position")]
    [SerializeField] private float spawnHeight = 0.5f;

    [Header("Launch")]
    [SerializeField] private PickupBezierLaunchSettings launchSettings = new PickupBezierLaunchSettings();

    [Header("Magnet")]
    [Tooltip("Distance where gold is instantly collected")]
    [SerializeField] private float collectDistance = 0.9f;
    [Tooltip("Distance where attraction starts")]
    [SerializeField] private float magnetDistance = 4f;
    [SerializeField] private float minMagnetSpeed = 5f;
    [SerializeField] private float maxMagnetSpeed = 18f;
    [Tooltip("How quickly velocity eases toward the player while magnetized")]
    [SerializeField] private float magnetSmoothTime = 0.12f;
    [Tooltip("Minimum time spent in magnet mode before pickup can be collected")]
    [SerializeField] private float minAttractTimeBeforeCollect = 0.08f;
    [Tooltip("Aims the gold slightly above player feet")]
    [SerializeField] private float targetHeightOffset = 0.9f;
    [SerializeField] private AnimationCurve magnetCurve = AnimationCurve.EaseInOut(0f, 0f, 1f, 1f);

    [Header("Spin")]
    [Tooltip("Rotate the coin while it is moving toward the player")]
    [SerializeField] private bool spinWhileMagnetized = true;
    [SerializeField] private float magnetSpinSpeed = 720f;
    [Tooltip("Local axis used for spin (set based on your coin mesh orientation)")]
    [SerializeField] private Vector3 magnetSpinAxis = Vector3.up;

    [Header("Physics")]
    [Tooltip("Forces all pickup colliders to be triggers so they never push the player")]
    [SerializeField] private bool forceTriggerColliders = true;
    [Tooltip("If this object has a Rigidbody, make it kinematic to avoid physics impulses")]
    [SerializeField] private bool makeRigidbodyKinematic = true;

    private PhysicsBasedCharacterController cachedPlayer;
    private bool launchCompleted;
    private bool isCollected;
    private bool initialized;
    private bool magnetActive;
    private float magnetActiveTime;
    private Vector3 magnetVelocity;

    private void Awake()
    {
        ConfigurePhysicsForPickup();
    }

    private void OnValidate()
    {
        collectDistance = Mathf.Max(0.01f, collectDistance);
        magnetDistance = Mathf.Max(collectDistance + 0.01f, magnetDistance);
        minMagnetSpeed = Mathf.Max(0f, minMagnetSpeed);
        maxMagnetSpeed = Mathf.Max(minMagnetSpeed, maxMagnetSpeed);
        magnetSmoothTime = Mathf.Max(0.01f, magnetSmoothTime);
        minAttractTimeBeforeCollect = Mathf.Max(0f, minAttractTimeBeforeCollect);
        magnetSpinSpeed = Mathf.Max(0f, magnetSpinSpeed);

        if (magnetSpinAxis.sqrMagnitude <= 0.0001f)
            magnetSpinAxis = Vector3.up;

        ConfigurePhysicsForPickup();
    }

    private void Start()
    {
        TryCachePlayer();

        if (!initialized)
            Initialize(goldAmount, transform.position);
    }

    public void Initialize(int amount, Vector3 worldSpawnPosition)
    {
        goldAmount = Mathf.Max(1, amount);
        transform.position = worldSpawnPosition + Vector3.up * spawnHeight;
        initialized = true;
        magnetActive = false;
        magnetActiveTime = 0f;
        magnetVelocity = Vector3.zero;

        StopAllCoroutines();
        StartCoroutine(LaunchRoutine());
    }

    private IEnumerator LaunchRoutine()
    {
        launchCompleted = false;
        yield return PickupMotionUtility.AnimateBezierLaunch(gameObject, launchSettings);
        launchCompleted = true;
    }

    private void Update()
    {
        if (!launchCompleted || isCollected)
            return;

        if (!TryCachePlayer())
            return;

        Vector3 targetPosition = cachedPlayer.transform.position + Vector3.up * targetHeightOffset;
        float distance = Vector3.Distance(transform.position, targetPosition);

        if (distance <= magnetDistance)
            magnetActive = true;

        if (!magnetActive)
            return;

        magnetActiveTime += Time.deltaTime;

        float t = 1f - Mathf.InverseLerp(magnetDistance, collectDistance, distance);
        float curve = magnetCurve != null ? magnetCurve.Evaluate(t) : t;
        float speed = Mathf.Lerp(minMagnetSpeed, maxMagnetSpeed, curve);
        float smoothTime = Mathf.Lerp(magnetSmoothTime * 1.4f, magnetSmoothTime * 0.7f, curve);
        Vector3 previousPosition = transform.position;

        transform.position = Vector3.SmoothDamp(
            transform.position,
            targetPosition,
            ref magnetVelocity,
            Mathf.Max(0.01f, smoothTime),
            speed,
            Time.deltaTime);

        if (spinWhileMagnetized)
        {
            float moveDelta = (transform.position - previousPosition).sqrMagnitude;
            if (moveDelta > 0.000001f)
            {
                Vector3 spinAxis = magnetSpinAxis.sqrMagnitude <= 0.0001f
                    ? Vector3.up
                    : magnetSpinAxis.normalized;

                transform.Rotate(spinAxis, magnetSpinSpeed * Time.deltaTime, Space.Self);
            }
        }

        float postMoveDistance = Vector3.Distance(transform.position, targetPosition);
        if (postMoveDistance <= collectDistance && magnetActiveTime >= minAttractTimeBeforeCollect)
            Collect();
    }

    private bool TryCachePlayer()
    {
        if (cachedPlayer != null)
            return true;

        if (GameManager.Instance != null)
            cachedPlayer = GameManager.Instance.Player;

        if (cachedPlayer == null)
            cachedPlayer = FindFirstObjectByType<PhysicsBasedCharacterController>();

        return cachedPlayer != null;
    }

    private void ConfigurePhysicsForPickup()
    {
        if (forceTriggerColliders)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>(true);
            for (int i = 0; i < colliders.Length; i++)
            {
                if (colliders[i] != null)
                    colliders[i].isTrigger = true;
            }
        }

        if (makeRigidbodyKinematic)
        {
            Rigidbody[] rigidbodies = GetComponentsInChildren<Rigidbody>(true);
            for (int i = 0; i < rigidbodies.Length; i++)
            {
                if (rigidbodies[i] == null)
                    continue;

                rigidbodies[i].isKinematic = true;
                rigidbodies[i].useGravity = false;
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!launchCompleted || isCollected)
            return;

        PhysicsBasedCharacterController player = other.GetComponentInParent<PhysicsBasedCharacterController>();
        if (player == null)
            return;

        cachedPlayer = player;
        magnetActive = true;
    }

    private void Collect()
    {
        if (isCollected)
            return;

        isCollected = true;

        if (PlayerInventory.Instance != null)
            PlayerInventory.Instance.AddGold(goldAmount);
        else
            Debug.LogWarning("No PlayerInventory instance found while collecting gold.", gameObject);

        Destroy(gameObject);
    }
}
