using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "UnitCard", menuName = "Cards/UnitCard")]
public class UnitCardSO : CardBaseSO
{
    public int attackDamage;
    public int maxHealth;
    public List<CardTrait> traits;
}
