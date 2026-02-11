using System.Collections;
using UnityEngine;

public class AbilitiyEffects : MonoBehaviour
{

    public void FireAbility(int index)
    {
        var _abilities = transform.root.GetComponent<Abilities>().abilities;
        if (_abilities == null || index < 0 || index >= _abilities.Length)
            return;

        Abilities.Ability a = _abilities[index];
        if (a.abilityEmitter == null)
            return;

        a.abilityEmitter.Fire(a);
    }

    public void OnThrow(int index)
    {
        var _abilities = transform.root.GetComponent<Abilities>().abilities;
        _abilities[index].abilityEmitter.optionalHeldItem.SetActive(false);
    }

    public void Restore(int index)
    {
        var _abilities = transform.root.GetComponent<Abilities>().abilities;
        _abilities[index].abilityEmitter.optionalHeldItem.SetActive(true);
    }
}