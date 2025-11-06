using UnityEngine;
using TMPro;

public class UnitUICard : UICard
{
    [SerializeField] TextMeshProUGUI cardAttackDamageText;
    [SerializeField] TextMeshProUGUI cardCurrentHealthText;
    
    public override void InitializeCard(HandManager _handManager, CardBaseSO _card)
    {
        BaseInitialization(_handManager, _card);
        
        cardTypeText.text = "UNIT";
        
        UnitCardSO unitCard = (UnitCardSO)_card;

        cardAttackDamageText.text = unitCard.attackDamage.ToString();
        cardCurrentHealthText.text = unitCard.maxHealth.ToString();
    }
}
