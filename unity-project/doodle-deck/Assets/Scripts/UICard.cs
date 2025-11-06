using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public abstract class UICard : MonoBehaviour, IPointerClickHandler
{ 
    [SerializeField] protected TextMeshProUGUI cardNameText;
    [SerializeField] protected TextMeshProUGUI cardEnergyCostText;
    [SerializeField] protected TextMeshProUGUI cardTypeText;
    [SerializeField] protected TextMeshProUGUI cardDescriptionText;
    [SerializeField] protected Image cardImage;
    
    protected HandManager handManager;

    public abstract void InitializeCard(HandManager _handManager, CardBaseSO _card);

    protected void BaseInitialization(HandManager _handManager, CardBaseSO _card)
    {
        handManager = _handManager;
        
        cardNameText.text = _card.cardName;
        cardEnergyCostText.text = _card.energyCost.ToString();
        cardDescriptionText.text = _card.cardDescription;
        cardImage.sprite = _card.cardImage;
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
