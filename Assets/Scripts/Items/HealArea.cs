using System.Collections;
using UnityEngine;

public class HealArea : MonoBehaviour
{
    [Tooltip("Seconds between each heal tick")]
    [SerializeField] private float tickInterval = 1f;
    [Tooltip("Amount of health to restore each tick")]
    [SerializeField] private int healAmountPerTick = 5;

    private Coroutine _healCoroutine;
    private PhysicsBasedCharacterController _player;

    private void OnEnable()
    {
        // If this HealArea is spawned overlapping the player, ensure we detect that immediately.
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Collider[] hits = Physics.OverlapBox(col.bounds.center, col.bounds.extents, transform.rotation);
            foreach (var hit in hits)
            {
                var ctrl = hit.GetComponent<PhysicsBasedCharacterController>();
                if (ctrl != null)
                {
                    _player = ctrl;
                    if (_healCoroutine == null)
                        _healCoroutine = StartCoroutine(HealCoroutine());
                    break;
                }
            }
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        var ctrl = other.GetComponent<PhysicsBasedCharacterController>();
        if (ctrl != null)
        {
            _player = ctrl;
            if (_healCoroutine == null)
                _healCoroutine = StartCoroutine(HealCoroutine());
        }
    }

    private void OnTriggerStay(Collider other)
    {
        // Ensure that if the coroutine wasn't started for some reason (physics timing), we start it while the player remains in the area.
        var ctrl = other.GetComponent<PhysicsBasedCharacterController>();
        if (ctrl != null)
        {
            if (_player == null)
                _player = ctrl;
            if (_healCoroutine == null)
                _healCoroutine = StartCoroutine(HealCoroutine());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        var ctrl = other.GetComponentInParent<PhysicsBasedCharacterController>();
        if (ctrl != null && ctrl == _player)
        {
            if (_healCoroutine != null)
            {
                StopCoroutine(_healCoroutine);
                _healCoroutine = null;
            }
            _player = null;
        }
    }

    private IEnumerator HealCoroutine()
    {
        while (true)
        {
            if (_player == null)
                yield break;

            _player.Heal(healAmountPerTick);

            yield return new WaitForSeconds(tickInterval);
        }
    }
}
