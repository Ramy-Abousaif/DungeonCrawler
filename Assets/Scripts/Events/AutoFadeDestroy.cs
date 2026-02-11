using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering.Universal;

public class AutoFadeDestroy : MonoBehaviour
{
    public float lifeTime = 5f;
    public float fadeDuration = 1f;
    public enum FadeStyle { Fade, Shrink }
    public FadeStyle fadeStyle = FadeStyle.Fade;

    private List<Material> _instancedMats = new List<Material>();
    private Vector3 _initialScale;
    private List<DecalProjector> _decals = new List<DecalProjector>();
    private List<Vector3> _decalsInitialSize = new List<Vector3>();

    private void OnEnable()
    {
        _initialScale = transform.localScale;

        // gather decal projectors in children (URP)
        _decals.Clear();
        _decalsInitialSize.Clear();
        var decs = GetComponentsInChildren<DecalProjector>(true);
        foreach (var d in decs)
        {
            _decals.Add(d);
            _decalsInitialSize.Add(d.size);
        }

        StartCoroutine(Run());
    }

    private IEnumerator Run()
    {
        if (lifeTime <= 0f)
        {
            Destroy(gameObject);
            yield break;
        }

        float wait = Mathf.Max(0f, lifeTime - fadeDuration);
        if (wait > 0f)
            yield return new WaitForSeconds(wait);

        // duplicate materials so we can modify them without affecting shared assets
        _instancedMats.Clear();
        if (fadeStyle == FadeStyle.Fade)
        {
            var rends = GetComponentsInChildren<Renderer>(true);
            foreach (var r in rends)
            {
                var shared = r.sharedMaterials;
                Material[] copies = new Material[shared.Length];
                for (int i = 0; i < shared.Length; ++i)
                {
                    Material src = shared[i];
                    if (src == null) { copies[i] = null; continue; }
                    Material inst = new Material(src);
                    copies[i] = inst;
                    _instancedMats.Add(inst);
                }
                r.materials = copies;
            }

            // also duplicate DecalProjector materials if present so we can fade them
            for (int i = 0; i < _decals.Count; ++i)
            {
                var d = _decals[i];
                if (d == null) continue;
                var mat = d.material;
                if (mat != null)
                {
                    var inst = new Material(mat);
                    d.material = inst;
                    _instancedMats.Add(inst);
                }
            }
        }

        float t = 0f;
        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Clamp01(1f - (t / fadeDuration));

            if (fadeStyle == FadeStyle.Fade)
            {
                // update materials' alpha
                foreach (var m in _instancedMats)
                {
                    if (m == null) continue;
                    if (m.HasProperty("_Color"))
                    {
                        Color c = m.color;
                        c.a = alpha;
                        m.color = c;
                    }
                    else if (m.HasProperty("_BaseColor"))
                    {
                        Color c = m.GetColor("_BaseColor");
                        c.a = alpha;
                        m.SetColor("_BaseColor", c);
                    }
                }
            }
            else // Shrink mode
            {
                // update decal projectors by scaling their size
                for (int i = 0; i < _decals.Count; ++i)
                {
                    var d = _decals[i];
                    if (d == null) continue;
                    Vector3 init = _decalsInitialSize[i];
                    d.size = init * alpha;
                }
            }

            yield return null;
        }

        // ensure final state is fully faded/shrunk (guard against loop precision issues)
        float finalAlpha = 0f;
        if (fadeStyle == FadeStyle.Fade)
        {
            foreach (var m in _instancedMats)
            {
                if (m == null) continue;
                if (m.HasProperty("_Color"))
                {
                    Color c = m.color;
                    c.a = finalAlpha;
                    m.color = c;
                }
                else if (m.HasProperty("_BaseColor"))
                {
                    Color c = m.GetColor("_BaseColor");
                    c.a = finalAlpha;
                    m.SetColor("_BaseColor", c);
                }
            }
        }
        else
        {
            // Shrink finalization: set decal projector sizes to zero and scale to zero if requested
            for (int i = 0; i < _decals.Count; ++i)
            {
                var d = _decals[i];
                if (d == null) continue;
                d.size = Vector3.zero;
            }
        }

        // cleanup
        foreach (var m in _instancedMats)
        {
            if (m != null)
                Destroy(m);
        }
        _instancedMats.Clear();

        Destroy(gameObject);
    }
}