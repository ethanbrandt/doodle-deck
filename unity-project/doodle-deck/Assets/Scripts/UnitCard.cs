using System;
using System.Collections.Generic;
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

    [Header("Status Effects")]
    [SerializeField] Image[] statusEffectIndicators;
    [SerializeField] Sprite wardIndicatorSprite;
    [SerializeField] Sprite intimidateIndicatorSprite;
    [SerializeField] Sprite fatigueIndicatorSprite;
    
    private int currentHealth = 0;
    private int maxHealth;
    private int attackDamage = 0;
    private string cardName;
    private bool canAction = true;
    
    //* STATUS EFFECT VARIABLES
    private int overhealth = 0;
    private int inspiration = 0;
    // bool = wasAppliedOnPlayerTurn
    private readonly Dictionary<StatusEffect, bool> statusEffectTracker = new Dictionary<StatusEffect, bool>();
    
    [Rpc(SendTo.Everyone)]
    public void InitializeCardRpc(NetworkCardData _cardData)
    {
        print(_cardData.cardName.ToString() + " : " + IsServer);
        UnitCardSO unitSO = (UnitCardSO)GameManager.Instance.cardDict[_cardData.cardName.ToString()];
        
        foreach (var indicator in statusEffectIndicators)
            indicator.enabled = false;

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
            currentHealth = unitSO.maxHealth;
            maxHealth = unitSO.maxHealth;
            attackDamage = unitSO.attackDamage;
            canAction = true;
        }
        
    }

    public int GetCurrentHealth()
    {
        if (currentHealth <= 0 && overhealth > 0)
            return overhealth;
        return currentHealth;
    }

    public int GetAttackDamage()
    {
        if (attackDamage <= 0)
            return 0;
        
        if (!statusEffectTracker.ContainsKey(StatusEffect.Intimidated))
            return attackDamage + inspiration;

        return Math.Max((attackDamage + inspiration) / 2, 1);
    }

    public string GetCardName()
    {
        return cardName;
    }

    public bool GetCanAction()
    {
        return canAction && !statusEffectTracker.ContainsKey(StatusEffect.Fatigued);
    }

    public void UseAction()
    {
        canAction = false;
    }

    public void RestoreAction()
    {
        canAction = true;
    }

    public void ResetHealth()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void GiveOverhealth(int _incomingOverhealth, bool _isPlayerTurn)
    {
        if (_incomingOverhealth <= 0)
            return;
        
        overhealth += _incomingOverhealth;
        statusEffectTracker.TryAdd(StatusEffect.Overhealthed, _isPlayerTurn);
        UpdateUI();
    }

    public void GiveInspiration(int _incomingInspiration, bool _isPlayerTurn)
    {
        if (_incomingInspiration <= 0)
            return;
        
        inspiration += _incomingInspiration;
        ApplyTempStatusEffect(StatusEffect.Inspired, _isPlayerTurn);
        UpdateUI();
    }

    public void ApplyTempStatusEffect(StatusEffect _effect, bool _isPlayerTurn)
    {
        statusEffectTracker[_effect] = _isPlayerTurn;
        UpdateUI();
    }

    public void ResetPlayerTurnTempStatusEffects()
    {
        if (statusEffectTracker.ContainsKey(StatusEffect.Inspired) && statusEffectTracker[StatusEffect.Inspired])
        {
            statusEffectTracker.Remove(StatusEffect.Inspired);
            inspiration = 0;
        }
        
        if (statusEffectTracker.ContainsKey(StatusEffect.Overhealthed) && statusEffectTracker[StatusEffect.Overhealthed])
        {
            statusEffectTracker.Remove(StatusEffect.Overhealthed);
            overhealth = 0;
        }

        foreach ((StatusEffect effect, bool wasAppliedOnPlayerTurn) in statusEffectTracker)
        {
            if (wasAppliedOnPlayerTurn)
                statusEffectTracker.Remove(effect);
        }
        
        UpdateUI();
    }
    
    public void ResetEnemyTurnTempStatusEffects()
    {
        if (statusEffectTracker.ContainsKey(StatusEffect.Inspired) && !statusEffectTracker[StatusEffect.Inspired])
        {
            statusEffectTracker.Remove(StatusEffect.Inspired);
            inspiration = 0;
        }

        if (statusEffectTracker.ContainsKey(StatusEffect.Overhealthed) && !statusEffectTracker[StatusEffect.Overhealthed])
        {
            statusEffectTracker.Remove(StatusEffect.Overhealthed);
            overhealth = 0;
        }

        StatusEffect[] effectsToRemove = new StatusEffect[statusEffectTracker.Count];
        int i = 0;
        foreach ((StatusEffect effect, bool wasAppliedOnPlayerTurn) in statusEffectTracker)
        {
            if (!wasAppliedOnPlayerTurn)
            {
                effectsToRemove[i] = effect;
                i++;
            }
        }

        for (int j = 0; j < i; j++)
            statusEffectTracker.Remove(effectsToRemove[j]);
        
        UpdateUI();
    }

    public bool TakeDamage(int _incomingDamage)
    {
        if (statusEffectTracker.ContainsKey(StatusEffect.Warded))
        {
            statusEffectTracker.Remove(StatusEffect.Warded);
            return false;
        }
        else if (overhealth > 0)
        {
            overhealth -= _incomingDamage;
            if (overhealth < 0)
            {
                currentHealth += overhealth;
                statusEffectTracker.Remove(StatusEffect.Overhealthed);
                overhealth = 0;
            }
        }
        else
        {
            currentHealth -= _incomingDamage;
        }
        
        if (currentHealth <= 0)
            print(cardNameText.text + " DIED");
        UpdateUI();
        return true;
    }

    public void Heal(int _incomingHealing)
    {
        print(cardName + " healed for: " + _incomingHealing);
        currentHealth = Math.Min(maxHealth, currentHealth + _incomingHealing);
        UpdateUI();
    }

    private void UpdateUI()
    {
        StatusEffect[] statusEffects = new StatusEffect[statusEffectTracker.Count];

        int i = 0;
        foreach ((StatusEffect effect, bool _) in statusEffectTracker)
        {
            statusEffects[i] = effect;
            i++;
        }
        
        UpdateUIRpc(currentHealth + overhealth, GetAttackDamage(), canAction, statusEffects);
    }

    private bool ContainsStatusEffect(StatusEffect[] _statusEffects, StatusEffect _effect)
    {
        foreach (var effect in _statusEffects)
        {
            if (effect == _effect)
                return true;
        }
        return false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdateUIRpc(int _displayHealth, int _displayAttackDamage, bool _canAction, StatusEffect[] _statusEffects)
    {
        cardCurrentHealthText.text = _displayHealth.ToString();
        cardCurrentHealthText.color = ContainsStatusEffect(_statusEffects, StatusEffect.Overhealthed) ? new Color(0.6f, 0.95f, 0.6f) : Color.white;
        
        cardAttackDamageText.text = _displayAttackDamage.ToString();
        cardAttackDamageText.color = ContainsStatusEffect(_statusEffects, StatusEffect.Inspired) ? new Color(0.95f, 0.95f, 0.7f) : Color.white;

        foreach (var indicator in statusEffectIndicators)
            indicator.enabled = false;

        int i = 0;
        if (ContainsStatusEffect(_statusEffects, StatusEffect.Warded))
        {
            statusEffectIndicators[i].enabled = true;
            statusEffectIndicators[i].sprite = wardIndicatorSprite;
            i++;
        }

        if (ContainsStatusEffect(_statusEffects, StatusEffect.Intimidated))
        {
            statusEffectIndicators[i].enabled = true;
            statusEffectIndicators[i].sprite = intimidateIndicatorSprite;
            i++;
        }

        if (ContainsStatusEffect(_statusEffects, StatusEffect.Fatigued))
        {
            statusEffectIndicators[i].enabled = true;
            statusEffectIndicators[i].sprite = fatigueIndicatorSprite;
        }
    }
}
