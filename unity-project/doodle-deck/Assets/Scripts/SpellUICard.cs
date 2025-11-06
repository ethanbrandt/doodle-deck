using UnityEngine;

public class SpellUICard : UICard
{
    public override void InitializeCard(HandManager _handManager, CardBaseSO _card)
    {
        SpellCardSO spell = (SpellCardSO)_card;

        cardTypeText.text = spell.isSwift ? "SWIFT SPELL" : "SPELL";
    }
}
