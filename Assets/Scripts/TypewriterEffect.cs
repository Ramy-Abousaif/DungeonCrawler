using System;
using System.Collections;
using UnityEngine;
using TMPro;
using Object = UnityEngine.Object;

[RequireComponent(typeof(TMP_Text))]
public class TypewriterEffect : MonoBehaviour
{
    private TMP_Text _textBox;

    private int _currentVisibleCharacterIndex;
    private Coroutine _typewriterCoroutine;
    private bool _readyForNewText = true;
    private bool _playOnEnableRequested;

    private WaitForSeconds _simpleDelay;
    private WaitForSeconds _interpunctuationDelay;

    [Header("Typewriter Settings")] 
    [SerializeField] private float charactersPerSecond = 20;
    [SerializeField] private float interpunctuationDelay = 0.5f;


    // Skipping Functionality
    public bool CurrentlySkipping { get; private set; }

    [Header("Skip options")] 
    [SerializeField] private bool quickSkip;
    [SerializeField] [Min(1)] private int skipSpeedup = 5;


    // Event Functionality
    private WaitForSeconds _textboxFullEventDelay;
    [SerializeField] [Range(0.1f, 0.5f)] private float sendDoneDelay = 0.25f;

    public static event Action CompleteTextRevealed;
    public static event Action<char> CharacterRevealed;


    private void Awake()
    {
        _textBox = GetComponent<TMP_Text>();

        _simpleDelay = new WaitForSeconds(1 / charactersPerSecond);
        _interpunctuationDelay = new WaitForSeconds(interpunctuationDelay);

        _textboxFullEventDelay = new WaitForSeconds(sendDoneDelay);
    }
    
    private void OnEnable()
    {
        _textBox.maxVisibleCharacters = 0;
        _currentVisibleCharacterIndex = 0;
        TMPro_EventManager.TEXT_CHANGED_EVENT.Add(PrepareForNewText);

        if (_playOnEnableRequested)
        {
            _playOnEnableRequested = false;
            _textBox.ForceMeshUpdate();

            if (_textBox.textInfo.characterCount > 0)
            {
                _readyForNewText = false;
                _typewriterCoroutine = StartCoroutine(Typewriter());
            }
            else
            {
                _readyForNewText = true;
            }
        }
    }

    private void OnDisable()
    {
        ForceCompleteDialogue(false);
        TMPro_EventManager.TEXT_CHANGED_EVENT.Remove(PrepareForNewText);
    }

    public void InvokeSkip()
    {
        if (_textBox.maxVisibleCharacters < _textBox.textInfo.characterCount)
            Skip();
    }

    public void PlayText(string dialogue, bool invokeCompleteEventIfEmpty = false)
    {
        if (_textBox == null)
            _textBox = GetComponent<TMP_Text>();

        if (_textBox == null)
            return;

        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);

        CurrentlySkipping = false;
        _readyForNewText = false;
        _currentVisibleCharacterIndex = 0;
        _textBox.maxVisibleCharacters = 0;
        _textBox.text = dialogue ?? string.Empty;
        _textBox.ForceMeshUpdate();

        if (_textBox.textInfo.characterCount == 0)
        {
            _readyForNewText = true;
            _playOnEnableRequested = false;
            if (invokeCompleteEventIfEmpty)
                CompleteTextRevealed?.Invoke();
            return;
        }

        if (!isActiveAndEnabled)
        {
            _readyForNewText = true;
            _playOnEnableRequested = true;
            return;
        }

        _typewriterCoroutine = StartCoroutine(Typewriter());
    }

    private void PrepareForNewText(Object obj)
    {
        if (obj != _textBox || !_readyForNewText)
            return;
        
        CurrentlySkipping = false;
        _readyForNewText = false;
    
        if (_typewriterCoroutine != null)
            StopCoroutine(_typewriterCoroutine);
    
        _textBox.maxVisibleCharacters = 0;
        _currentVisibleCharacterIndex = 0;

        _typewriterCoroutine = StartCoroutine(Typewriter());
    }

    private IEnumerator Typewriter()
    {
        TMP_TextInfo textInfo = _textBox.textInfo;

        while (_currentVisibleCharacterIndex < textInfo.characterCount + 1)
        {
            var lastCharacterIndex = textInfo.characterCount - 1;

            if (_currentVisibleCharacterIndex >= lastCharacterIndex)
            {
                _textBox.maxVisibleCharacters++;
                yield return _textboxFullEventDelay;
                CompleteTextRevealed?.Invoke();
                _readyForNewText = true;
                yield break;
            }

            if (CurrentlySkipping)
            {
                int charactersToRevealThisFrame = Mathf.Max(1, Mathf.CeilToInt(charactersPerSecond * skipSpeedup * Time.deltaTime));

                for (int i = 0; i < charactersToRevealThisFrame && _currentVisibleCharacterIndex < lastCharacterIndex; i++)
                {
                    char skipCharacter = textInfo.characterInfo[_currentVisibleCharacterIndex].character;
                    _textBox.maxVisibleCharacters++;
                    CharacterRevealed?.Invoke(skipCharacter);
                    _currentVisibleCharacterIndex++;
                }

                yield return null;
                continue;
            }

            char character = textInfo.characterInfo[_currentVisibleCharacterIndex].character;

            _textBox.maxVisibleCharacters++;
            
            if (character == '?' || character == '.' || character == ',' || character == ':' ||
                character == ';' || character == '!' || character == '-')
            {
                yield return _interpunctuationDelay;
            }
            else
            {
                yield return _simpleDelay;
            }
            
            CharacterRevealed?.Invoke(character);
            _currentVisibleCharacterIndex++;
        }
    }

    private void Skip()
    {
        if (CurrentlySkipping)
            return;

        if (quickSkip)
        {
            ForceCompleteDialogue();
            return;
        }

        CurrentlySkipping = true;
        StartCoroutine(SkipSpeedupReset());
    }

    public void ForceCompleteDialogue(bool invokeCompleteEvent = true)
    {
        if (_textBox == null)
            _textBox = GetComponent<TMP_Text>();

        if (_textBox == null)
            return;

        StopAllCoroutines();
        _typewriterCoroutine = null;

        _textBox.ForceMeshUpdate();
        int characterCount = _textBox.textInfo.characterCount;

        _textBox.maxVisibleCharacters = characterCount;
        _currentVisibleCharacterIndex = characterCount;
        CurrentlySkipping = false;
        _readyForNewText = true;

        if (invokeCompleteEvent)
            CompleteTextRevealed?.Invoke();
    }

    private IEnumerator SkipSpeedupReset()
    {
        yield return new WaitUntil(() => _textBox.maxVisibleCharacters >= _textBox.textInfo.characterCount);
        CurrentlySkipping = false;
    }
}