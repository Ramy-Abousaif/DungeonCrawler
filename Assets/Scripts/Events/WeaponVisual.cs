using System.Collections;
using UnityEngine;

public class WeaponVisual : MonoBehaviour
{
    [SerializeField] private GameObject heldModel;

    public void OnThrow()
    {
        heldModel.SetActive(false);
    }

    public void Restore()
    {
        heldModel.SetActive(true);
    }
}