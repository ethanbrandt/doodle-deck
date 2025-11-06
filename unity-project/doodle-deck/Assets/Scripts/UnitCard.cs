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
    private NetworkVariable<int> attackDamage = new NetworkVariable<int>(0, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private string cardName;
    private NetworkVariable<bool> canAction = new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Server);
    private int overhealth = 0;
    private int inspiration = 0;

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
            attackDamage.Value = unitSO.attackDamage;
            canAction.Value = true;
        }
        
    }

    public int GetCurrentHealth()
    {
        return currentHealth.Value;
    }

    public int GetAttackDamage()
    {
        return attackDamage.Value;
    }

    public string GetCardName()
    {
        return cardName;
    }

    public bool GetCanAction()
    {
        return canAction.Value;
    }

    public void UseAction()
    {
        canAction.Value = false;
    }

    public void RestoreAction()
    {
        canAction.Value = true;
    }

    public void ResetHealth()
    {
        currentHealth.Value = maxHealth;
        UpdateUIRpc(currentHealth.Value, attackDamage.Value, canAction.Value);
    }

    public void TakeDamage(int _incomingDamage)
    {
        currentHealth.Value -= _incomingDamage;
        if (currentHealth.Value <= 0)
            print(cardNameText.text + " DIED");
        UpdateUIRpc(currentHealth.Value, attackDamage.Value, canAction.Value);
    }

    public void Heal(int _incomingHealing)
    {
        print(cardName + " healed for: " + _incomingHealing);
        currentHealth.Value = Math.Min(maxHealth, currentHealth.Value + _incomingHealing);
        UpdateUIRpc(currentHealth.Value, attackDamage.Value, canAction.Value);
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateUIRpc(int _currentHealth, int _attackDamage, bool _canAction)
    {
        cardCurrentHealthText.text = _currentHealth.ToString();
        cardAttackDamageText.text = _attackDamage.ToString();
    }
}
