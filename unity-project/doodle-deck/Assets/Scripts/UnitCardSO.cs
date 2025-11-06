using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitCard", menuName = "Cards/UnitCard")]
public class UnitCardSO : CardBaseSO
{
    public int attackDamage;
    public int maxHealth;
    public List<CardTrait> traits;

    public bool ContainsTrait(TraitsEnum _traitEnum)
    {
        foreach (var trait in traits)
        {
            if (trait.trait == _traitEnum)
                return true;
        }
        return false;
    }

    public int GetTraitValue(TraitsEnum _traitEnum)
    {
        foreach (var trait in traits)
        {
            if (trait.trait == _traitEnum)
                return trait.val;
        }
        return -1;
    }
}
