using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitUICard : MonoBehaviour
{
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardEnergyCostText;
    [SerializeField] TextMeshProUGUI cardTypeText;
    [SerializeField] TextMeshProUGUI cardDescriptionText;
    [SerializeField] TextMeshProUGUI cardAttackDamageText;
    [SerializeField] TextMeshProUGUI cardCurrentHealthText;
    [SerializeField] Image cardImage;

    public void InitializeCard(UnitCardSO _unitCard)
    {
        cardNameText.text = _unitCard.cardName;
        cardEnergyCostText.text = _unitCard.energyCost.ToString();
        cardTypeText.text = "UNIT";
        cardDescriptionText.text = _unitCard.cardDescription;
        cardAttackDamageText.text = _unitCard.attackDamage.ToString();
        cardCurrentHealthText.text = _unitCard.maxHealth.ToString();
        cardImage.sprite = _unitCard.cardImage;
    }

    public void CardClicked()
    {
        print(cardNameText.text);
    }
}
