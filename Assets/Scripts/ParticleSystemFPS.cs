using System.Collections;
using UnityEngine;

[ExecuteAlways]
public class ParticleSystemFPS : MonoBehaviour
{
    [SerializeField]
    private ParticleSystem _particleSystem;

    [SerializeField]
    [Range(1, 30)]
    private int _fps;

    private float _stepDuration;

    private void OnValidate()
    {
        if (_particleSystem == null)
        {
            _particleSystem = GetComponent<ParticleSystem>();
        }

        _stepDuration = 1f / _fps;
    }

    public void Start()
    {
        Debug.Assert(_particleSystem != null, "ParticleSystem is missing.", this);

        _stepDuration = 1f / _fps;

        StartCoroutine(RunSteps());
    }

    public void OnDestroy()
    {
        StopAllCoroutines();
    }

    public IEnumerator RunSteps()
    {
        while(true)
        {
            while (!_particleSystem.isStopped)
            {
                _particleSystem.Simulate(_stepDuration, true, false);
                yield return new WaitForSeconds(_stepDuration);
            }

            yield return null;
        }
    }

#if UNITY_EDITOR
    // Hacky way to support the editor
    public void Update()
    {
        // Don't run in play mode
        if (UnityEditor.EditorApplication.isPlaying)
        {
            return;
        }

        // Mimic the editor particle system behavior
        if (UnityEditor.Selection.activeGameObject != gameObject)
        {
            _particleSystem.Stop();
            _particleSystem.Clear();
        }
    }
#endif
}