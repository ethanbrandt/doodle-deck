using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UnitUICard : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardEnergyCostText;
    [SerializeField] TextMeshProUGUI cardTypeText;
    [SerializeField] TextMeshProUGUI cardDescriptionText;
    [SerializeField] TextMeshProUGUI cardAttackDamageText;
    [SerializeField] TextMeshProUGUI cardCurrentHealthText;
    [SerializeField] Image cardImage;

    private HandManager handManager;
    
    public void InitializeCard(HandManager _handManager, UnitCardSO _unitCard)
    {
        handManager = _handManager;
        
        cardNameText.text = _unitCard.cardName;
        cardEnergyCostText.text = _unitCard.energyCost.ToString();
        cardTypeText.text = "UNIT";
        cardDescriptionText.text = _unitCard.cardDescription;
        cardAttackDamageText.text = _unitCard.attackDamage.ToString();
        cardCurrentHealthText.text = _unitCard.maxHealth.ToString();
        cardImage.sprite = _unitCard.cardImage;
    }

    public void OnPointerClick(PointerEventData _eventData)
    {
        handManager.HandleUICardClick(this);
    }

    public string GetInfo()
    {
        return cardNameText.text;
    }
}
