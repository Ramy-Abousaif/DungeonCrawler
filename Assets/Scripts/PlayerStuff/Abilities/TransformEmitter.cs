using UnityEngine;

public class TransformEmitter : AbilityEmitter
{
    [SerializeField] private CharacterForm targetForm;
    [SerializeField] private float blendSpeed = 0.25f;

    protected override void PerformFire(Abilities.Ability ability)
    {
        var abilities = player.GetComponent<Abilities>();
        abilities.ApplyTimedForm(targetForm, blendSpeed, player.duration, blendSpeed);
    }
}