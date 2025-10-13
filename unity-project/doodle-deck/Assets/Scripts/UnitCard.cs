using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UnitCard : NetworkBehaviour
{
    [SerializeField] TextMeshProUGUI cardNameText;
    [SerializeField] TextMeshProUGUI cardEnergyCostText;
    [SerializeField] TextMeshProUGUI cardTypeText;
    [SerializeField] TextMeshProUGUI cardDescriptionText;
    [SerializeField] TextMeshProUGUI cardAttackDamageText;
    [SerializeField] TextMeshProUGUI cardCurrentHealthText;
    [SerializeField] Image cardImage;
    
    private NetworkVariable<int> currentHealth = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int maxHealth;
    private int attackDamage;
    private string cardName;

    [Rpc(SendTo.Everyone)]
    public void InitializeCardRpc(NetworkCardData _cardData)
    {
        print(_cardData.cardName.ToString() + " : " + IsServer);
        UnitCardSO unitSO = (UnitCardSO)GameManager.Instance.cardDict[_cardData.cardName.ToString()];
        
        if (IsClient)
        {
            cardNameText.text = unitSO.cardName;
            cardEnergyCostText.text = unitSO.energyCost.ToString();
            cardTypeText.text = "UNIT";
            cardDescriptionText.text = unitSO.cardDescription;
            cardAttackDamageText.text = unitSO.attackDamage.ToString();
            cardCurrentHealthText.text = unitSO.maxHealth.ToString();
            cardImage.sprite = unitSO.cardImage;
        }

        if (IsServer)
        {
            cardName = unitSO.cardName;
            currentHealth.Value = unitSO.maxHealth;
            maxHealth = unitSO.maxHealth;
            attackDamage = unitSO.attackDamage;
        }
    }

    public int GetCurrentHealth()
    {
        return currentHealth.Value;
    }

    public int GetAttackDamage()
    {
        return attackDamage;
    }

    public string GetCardName()
    {
        return cardName;
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        UpdateUIRpc();
    }

    public void TakeDamage(int _incomingDamage)
    {
        currentHealth.Value -= _incomingDamage;
        if (currentHealth.Value <= 0)
            print(cardNameText.text + " DIED");
        UpdateUIRpc();
    }

    public void Heal(int _incomingHealing)
    {
        currentHealth.Value = Math.Min(maxHealth, currentHealth.Value + _incomingHealing);
        UpdateUIRpc();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateUIRpc()
    {
        cardCurrentHealthText.text = currentHealth.Value.ToString();
        cardAttackDamageText.text = attackDamage.ToString();
    }
}
