using UnityEngine;

[CreateAssetMenu(menuName = "Character/Form")]
public class CharacterForm : ScriptableObject
{
    [Header("Stats")]
    public float moveSpeedMultiplier = 1.5f;
    public float damageMultiplier = 1.5f;

    [Header("Flight")]
    public bool canFly = false;

    [Header("Abilities")]
    public Abilities.Ability[] abilities;

    void Awake()
    {
        
    }
}
