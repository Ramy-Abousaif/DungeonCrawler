using Unity.Cinemachine;
using UnityEngine;
using System.Collections;
using System;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance;

    private bool orthoCamera = true;
    public CinemachineCamera orthoVCam;
    public CinemachineCamera shopCam;
    
    [Header("Fade Settings")]
    public float fadeDuration = 0.5f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        
        if (UIManager.Instance.BlackScreen != null)
            UIManager.Instance.BlackScreen.color = new Color(0f, 0f, 0f, 0f);
    }

    public void SwitchState(Action onFadeComplete = null)
    {
        StartCoroutine(SwitchStateWithFade(onFadeComplete));
    }

    private IEnumerator SwitchStateWithFade(Action onFadeComplete = null)
    {
        yield return StartCoroutine(FadeIn());

        if(orthoCamera)
        {
            orthoVCam.Priority = 0;
            shopCam.Priority = 1;
        }
        else
        {
            orthoVCam.Priority = 1;
            shopCam.Priority = 0;
        }
        orthoCamera = !orthoCamera;
        
        onFadeComplete?.Invoke();

        yield return StartCoroutine(FadeOut());
    }

    private IEnumerator FadeIn()
    {
        if (UIManager.Instance.BlackScreen == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            UIManager.Instance.BlackScreen.color = new Color(0f, 0f, 0f, Mathf.Lerp(0f, 1f, elapsed / fadeDuration));
            yield return null;
        }
        
        UIManager.Instance.BlackScreen.color = new Color(0f, 0f, 0f, 1f);
    }

    private IEnumerator FadeOut()
    {
        if (UIManager.Instance.BlackScreen == null) yield break;
        
        float elapsed = 0f;
        
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            UIManager.Instance.BlackScreen.color = new Color(0f, 0f, 0f, Mathf.Lerp(1f, 0f, elapsed / fadeDuration));
            yield return null;
        }
        
        UIManager.Instance.BlackScreen.color = new Color(0f, 0f, 0f, 0f);
    }
}
