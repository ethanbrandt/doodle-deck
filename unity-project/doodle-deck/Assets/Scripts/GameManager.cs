using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.XR;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    [SerializeField] GameObject unitCardPrefab;
    [SerializeField] Transform[] slots;
    [SerializeField] float cardHoverDist;
    [SerializeField] int playerStartingHealth;
    [SerializeField] int playerStartCards;
    
    [SerializeField] TextMeshProUGUI player1HealthText;
    [SerializeField] TextMeshProUGUI player2HealthText;

    [SerializeField] TextMeshProUGUI outcomeText;
    
    [SerializeField] EnergyManager energyManager;
    [SerializeField] HandManager handManager;
    [SerializeField] ClientUIManager clientUIManager;

    [SerializeField] DeckSO deck;
    
    private readonly Stack<HandCardData> player1Deck = new Stack<HandCardData>();
    private readonly Stack<HandCardData> player2Deck = new Stack<HandCardData>();

    private readonly Dictionary<string, int> player1Hand = new Dictionary<string, int>();
    private readonly Dictionary<string, int> player2Hand = new Dictionary<string, int>();

    private readonly UnitCard[] player1Units = new UnitCard[5];
    private readonly UnitCard[] player2Units = new UnitCard[5];

    private int player1Health;
    private int player2Health;

    private int currentPlayerTurn = 0;
    private int round = 0;
    
    public static GameManager Instance { get; private set; }

    public readonly Dictionary<string, CardBaseSO> cardDict = new Dictionary<string, CardBaseSO>();
    
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("MULTIPLE GAMEMANAGER INSTANCE (THIS WILL CAUSE ISSUES)");
        }
        
        Instance = this;

        player1Health = playerStartingHealth;
        player2Health = playerStartingHealth;
        
        CardBaseSO[] loadedCards = Resources.LoadAll<CardBaseSO>("");

        foreach (var card in loadedCards)
        {
            cardDict.Add(card.cardName, card);
        }
    }

#region GAME_INTIALIZATION
    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            if (NetworkManager.Singleton.LocalClientId > 2)
            {
                ClientConnectRpc((int)NetworkManager.Singleton.LocalClientId, null);
                return;
            }
            
            print("SENDING DECK");
            ClientConnectRpc((int)NetworkManager.Singleton.LocalClientId, deck.GetNetowrkDeck());
        }
    }

    [Rpc(SendTo.Server)]
    private void ClientConnectRpc(int _clientId, NetworkCardData[] _deck)
    {
        UpdatePlayerHealthInfoRpc(player1Health, player2Health);
        print(_clientId + " Connected to Server");
        if (_clientId > 2)
        {
            print($"{_clientId} IS SPECTATING");
            return;
        }
        
        NetworkCardData[] shuffledList = _deck.OrderBy(_ => Random.value).ToArray();
        
        if (_clientId == 1)
        {
            foreach (var card in shuffledList)
                player1Deck.Push(new HandCardData(card.cardName.ToString(), card.cardType));
        }
        else if (_clientId == 2)
        {
            print("BOTH PLAYERS CONNECTED");
            
            foreach (var card in shuffledList)
                player2Deck.Push(new HandCardData(card.cardName.ToString(), card.cardType));
        
            InitializeFirstRound();
        }
    }

    private void InitializeFirstRound()
    {
        energyManager.InitializeEnergy();
        energyManager.ResetCurrentEnergy();
        
        round = 1;
        clientUIManager.SetRoundCounterRpc(round);
        
        currentPlayerTurn = 1;
        handManager.SetPlayerTurnRpc(true, RpcTarget.Single(1, RpcTargetUse.Temp));
        handManager.SetPlayerTurnRpc(false, RpcTarget.Single(2, RpcTargetUse.Temp));
        
        for (int i = 0; i < playerStartCards; i++)
        {
            DrawPlayer1Card();
            DrawPlayer2Card();
        }
    }
#endregion

#region USING_CARDS
    [Rpc(SendTo.Server)]
    public void TryPlayCardRpc(int _clientId, string _cardName, Vector2Int _slotIndex)
    {
        if (!cardDict.ContainsKey(_cardName))
        {
            Debug.LogError("INVALID CARD NAME TO BE PLAYED: " + _cardName);
            return;
        }

        if (!IsValidSlotIndex(_slotIndex))
            return;
        
        if (_clientId == 1)
            TryPlayPlayer1Card(_cardName, _slotIndex);
        
        if (_clientId == 2)
            TryPlayPlayer2Card(_cardName, _slotIndex);
    }

    private void TryPlayPlayer1Card(string _cardName, Vector2Int _slotIndex)
    {
        if (!player1Hand.ContainsKey(_cardName) || player1Hand[_cardName] <= 0)
        {
            print("Player 1 does not have card");
            return;
        }
        
        CardBaseSO card = cardDict[_cardName];

        if (energyManager.Player1CurrentEnergy < card.energyCost)
        {
            print("Player 1 does not have enough energy");
            return;
        }

        if (card is SpellCardSO)
        {
            SpellCardSO spell = (SpellCardSO)card;

            if (currentPlayerTurn != 1 && !spell.isSwift)
            {
                Debug.LogError("PLAYER TRIED TO PLAY SPELL ON ENEMY TURN");
                return;
            }

            TryUseSpell(1, spell);
        }
        else if (card is UnitCardSO)
        {
            if (currentPlayerTurn != 1)
            {
                Debug.LogError("PLAYER 1 TRIED TO PLAY UNIT ON ENEMY TURN");
                return;
            }
    
            if (_slotIndex.x != 0)
            {
                print("Player 1 Attempted to place unit in enemy territory");
                return;
            }
    
            if (player1Units[_slotIndex.y])
            {
                print("Player 1 Attempted to place unit on top of other unit");
                return;
            }
            
            var unitCard = SpawnUnitCard(_cardName, _slotIndex);
            player1Units[_slotIndex.y] = unitCard;
            HandleUnitPlacementTraits(1, _slotIndex.y);
        }
        
        energyManager.UsePlayer1Energy(card.energyCost);
        player1Hand[_cardName]--;
        handManager.RemoveCardFromHandRpc(RpcTarget.Single(1, RpcTargetUse.Temp));
    }

    private void TryPlayPlayer2Card(string _cardName, Vector2Int _slotIndex)
    {
         if (!player2Hand.ContainsKey(_cardName) || player2Hand[_cardName] <= 0)
         {
             print("Player 2 does not have card");
             return;
         }
         
         CardBaseSO card = cardDict[_cardName];
 
         if (energyManager.Player2CurrentEnergy < card.energyCost)
         {
             print("Player 2 does not have enough energy");
             return;
         }
 
         if (card is SpellCardSO)
         {
             SpellCardSO spell = (SpellCardSO)card;
 
             if (currentPlayerTurn != 2 && !spell.isSwift)
             {
                 Debug.LogError("PLAYER TRIED TO PLAY SPELL ON ENEMY TURN");
                 return;
             }
 
             TryUseSpell(2, spell);
         }
         else if (card is UnitCardSO)
         {
             if (currentPlayerTurn != 2)
             {
                 Debug.LogError("PLAYER 2 TRIED TO PLAY UNIT ON ENEMY TURN");
                 return;
             }
     
             if (_slotIndex.x != 1)
             {
                 print("Player 2 Attempted to place unit in enemy territory");
                 return;
             }
     
             if (player2Units[_slotIndex.y])
             {
                 print("Player 2 Attempted to place unit on top of other unit");
                 return;
             }
             
             var unitCard = SpawnUnitCard(_cardName, _slotIndex);
             player2Units[_slotIndex.y] = unitCard;
             HandleUnitPlacementTraits(2, _slotIndex.y);
         }
         
         energyManager.UsePlayer2Energy(card.energyCost);
         player2Hand[_cardName]--;
         handManager.RemoveCardFromHandRpc(RpcTarget.Single(2, RpcTargetUse.Temp));       
    }

    private UnitCard SpawnUnitCard(string _cardName, Vector2Int _slotIndex)
    {
        Vector3 spawnPos = slots[(_slotIndex.x * 5) + _slotIndex.y].position;
        spawnPos.y += cardHoverDist;
        var unitCard = Instantiate(unitCardPrefab, spawnPos, unitCardPrefab.transform.rotation).GetComponent<UnitCard>();
        unitCard.GetComponent<NetworkObject>().Spawn(true);
        unitCard.InitializeCardRpc(new NetworkCardData(_cardName, CardType.Unit));
        return unitCard;
    }

    private void HandleUnitPlacementTraits(int _clientId, int _slotIndex)
    {
        UnitCard[] sideCards = _clientId == 1 ? player1Units : player2Units;
        UnitCard[] enemySideCards = _clientId == 2 ? player1Units : player2Units;
        UnitCardSO unitCard = (UnitCardSO)cardDict[sideCards[_slotIndex].GetCardName()];

        if (unitCard.ContainsTrait(TraitsEnum.SneakAttack))
        {
            if (enemySideCards[_slotIndex])
            {
                DealDamageToUnit(enemySideCards, _slotIndex, unitCard.GetTraitValue(TraitsEnum.SneakAttack));
                CheckAndHandleAllUnitDeaths();
            }
            else
            {
                DealDamageToPlayer(_clientId, unitCard.GetTraitValue(TraitsEnum.SneakAttack));
            }
            
        }

        if (unitCard.ContainsTrait(TraitsEnum.Braced))
        {
            sideCards[_slotIndex].GiveOverhealth(unitCard.GetTraitValue(TraitsEnum.Braced), true);
        }

        if (unitCard.ContainsTrait(TraitsEnum.GrandEntrance))
        {
            if (enemySideCards[_slotIndex])
                enemySideCards[_slotIndex].ApplyTempStatusEffect(StatusEffect.Intimidated, false);
        }
    }
    
#region SPELLS
    private bool TryUseSpell(int _clientId, SpellCardSO _spell)
    {
        return true;
    }
#endregion

    [Rpc(SendTo.Server)]
    public void RequestSlotInfoRpc(int _clientId, Vector2Int _slotIndex)
    {
        if (!IsValidSlotIndex(_slotIndex))
            return;
        
        UnitCard slotInfo = _slotIndex.x == 0 ? player1Units[_slotIndex.y] : player2Units[_slotIndex.y];
        string cardName = slotInfo ? slotInfo.GetCardName() : "";
        handManager.ReceiveSlotInfoRpc(new NetworkCardData(cardName, CardType.Unit), _slotIndex, RpcTarget.Single((ulong)_clientId, RpcTargetUse.Temp));
    }

    [Rpc(SendTo.Server)]
    public void TryMoveUnitRpc(int _clientId, Vector2Int _startSlotIndex, Vector2Int _endSlotIndex)
    {
        if (_clientId != currentPlayerTurn)
        {
            Debug.LogError($"PLAYER {_clientId} TRIED TO MOVE UNIT ON ENEMY TURN");
            return;
        }

        if (_clientId != _startSlotIndex.x + 1 || _clientId != _endSlotIndex.x + 1)
        {
            Debug.LogError($"Player {_clientId} ATTEMPTED TO MOVE UNIT ON ENEMY SIDE");
            return;
        }
        
        if (!IsValidSlotIndex(_startSlotIndex) || !IsValidSlotIndex(_endSlotIndex))
            return;

        UnitCard[] sideCards = _clientId == 1 ? player1Units : player2Units;
        UnitCardSO unit = (UnitCardSO)cardDict[sideCards[_startSlotIndex.y].GetCardName()];

        if (unit.ContainsTrait(TraitsEnum.Immobile))
        {
            return;
        }

        if (!sideCards[_startSlotIndex.y].GetCanAction())
        {
            Debug.LogError($"Player {_clientId} ATTEMPTED TO MOVE UNIT WITHOUT ACTION");
            return;
        }
        
        if (Math.Abs(_startSlotIndex.y - _endSlotIndex.y) != 1)
        {
            if (unit.ContainsTrait(TraitsEnum.LightFooted))
            {
                int firstSwap = _startSlotIndex.y + (_endSlotIndex.y - _startSlotIndex.y > 0 ? 1 : -1);
                MoveUnit(_clientId, _startSlotIndex, new Vector2Int(_endSlotIndex.x, firstSwap));
                _startSlotIndex.y = firstSwap;
            }
            else
            {
                Debug.LogWarning($"Player {_clientId} ATTEMPTED TO MOVE UNIT MORE THAN 1 SLOT");
                return;
            }
        }
        
        print($"Moving card at {_startSlotIndex} to {_endSlotIndex}");
        print($"Updated transform from {sideCards[_startSlotIndex.y].transform.position} to {slots[(_startSlotIndex.x * 5) + _startSlotIndex.y].position}");
        sideCards[_startSlotIndex.y].UseAction();
        MoveUnit(_clientId, _startSlotIndex, _endSlotIndex);
    }

    private void MoveUnit(int _clientId, Vector2Int _startSlotIndex, Vector2Int _endSlotIndex)
    {
        UnitCard[] sideCards = _clientId == 1 ? player1Units : player2Units;
        UnitCard[] enemySideCards = _clientId == 2 ? player1Units : player2Units;
        UnitCardSO unit = (UnitCardSO)cardDict[sideCards[_startSlotIndex.y].GetCardName()];
        
        (sideCards[_endSlotIndex.y], sideCards[_startSlotIndex.y]) = (sideCards[_startSlotIndex.y], sideCards[_endSlotIndex.y]); // SWAP THE CARDS SLOTS

        if (sideCards[_startSlotIndex.y])
            sideCards[_startSlotIndex.y].transform.position = slots[(_startSlotIndex.x * 5) + _startSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);

        if (sideCards[_endSlotIndex.y])
            sideCards[_endSlotIndex.y].transform.position = slots[(_endSlotIndex.x * 5) + _endSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);
        
        HandleHealingTrail(unit, sideCards[_startSlotIndex.y]);
        HandleDriveBy(_clientId, unit, _startSlotIndex.y);
        HandleOpportunistic(sideCards, enemySideCards, _endSlotIndex.y, _startSlotIndex.y);
        
        CheckAndHandleAllUnitDeaths();
    }

    private void HandleHealingTrail(UnitCardSO _unitCard, UnitCard _movedOverUnit)
    {
        if (!_unitCard.ContainsTrait(TraitsEnum.HealingTrail))
            return;

        if (_movedOverUnit)
            _movedOverUnit.Heal(_unitCard.GetTraitValue(TraitsEnum.HealingTrail));
    }

    private void HandleDriveBy(int _clientId, UnitCardSO _unitCard, int _enemySlotIndex)
    {
        if (!_unitCard.ContainsTrait(TraitsEnum.DriveBy))
            return;
        
        UnitCard[] enemySideCards = _clientId == 2 ? player1Units : player2Units;

        if (enemySideCards[_enemySlotIndex])
        {
            DealDamageToUnit(enemySideCards, _enemySlotIndex, _unitCard.GetTraitValue(TraitsEnum.DriveBy));
        }
        else
        {
            DealDamageToPlayer(_clientId, _unitCard.GetTraitValue(TraitsEnum.DriveBy));
        }
    }

    private void HandleOpportunistic(UnitCard[] _sideCards, UnitCard[] _enemySideCards, int _movedCardUnitIndex, int _enemyUnitIndex)
    {
        if (!_enemySideCards[_enemyUnitIndex])
            return;
        
        UnitCardSO enemyUnitCard = (UnitCardSO)cardDict[_enemySideCards[_enemyUnitIndex].GetCardName()];
        
        if (!enemyUnitCard.ContainsTrait(TraitsEnum.Opportunistic))
            return;
        
        DealDamageToUnit(_sideCards, _movedCardUnitIndex, enemyUnitCard.GetTraitValue(TraitsEnum.Opportunistic));
    }

    [Rpc(SendTo.Server)]
    public void TryAttackUnitRpc(int _clientId, Vector2Int _attackerSlotIndex, Vector2Int _defenderSlotIndex)
    {
        if (_clientId != currentPlayerTurn)
        {
            Debug.LogError($"PLAYER {_clientId} TRIED TO ATTACK WITH UNIT ON ENEMY TURN");
            return;
        }

        if (_attackerSlotIndex.y != _defenderSlotIndex.y)
        {
            Debug.LogWarning($"Player {_clientId} ATTEMPTED TO ATTACK WRONG INDEX");
            return;
        }

        if (_clientId != _attackerSlotIndex.x + 1)
        {
            Debug.LogWarning($"Player {_clientId} ATTEMPTED TO ATTACK WITH ENEMY UNIT");
            return;
        }

        if (!IsValidSlotIndex(_attackerSlotIndex) || !IsValidSlotIndex(_defenderSlotIndex))
            return;

        UnitCard[] attackerSide = _clientId == 1 ? player1Units : player2Units;
        UnitCard[] defenderSide = _clientId == 1 ? player2Units : player1Units;

        int slotIndex = _defenderSlotIndex.y;

        if (!attackerSide[slotIndex].GetCanAction())
        {
            Debug.LogError($"PLAYER {_clientId} ATTEMPTED TO ATTACK WITH UNIT WITHOUT ACTION");
            return;
        }

        attackerSide[slotIndex].UseAction();

        UnitCardSO unitCard = (UnitCardSO)cardDict[attackerSide[slotIndex].GetCardName()];

        if (defenderSide[slotIndex])
        {
            bool x = DealDamageToUnit(defenderSide, slotIndex, attackerSide[slotIndex].GetAttackDamage());
            DealDamageToUnit(attackerSide, slotIndex, defenderSide[slotIndex].GetAttackDamage());

            HandleVampiric(unitCard, attackerSide[slotIndex], x ? attackerSide[slotIndex].GetAttackDamage() : 0);

            CheckAndHandleAllUnitDeaths();
        }
        else
        {
            DealDamageToPlayer(_clientId, attackerSide[slotIndex].GetAttackDamage());

            HandleVampiric(unitCard, attackerSide[slotIndex], attackerSide[slotIndex].GetAttackDamage());
        }

        if (unitCard.ContainsTrait(TraitsEnum.Cleave))
        {
            if (slotIndex > 0)
            {
                if (defenderSide[slotIndex - 1])
                {
                    bool x = DealDamageToUnit(defenderSide, slotIndex - 1, attackerSide[slotIndex].GetAttackDamage());

                    HandleVampiric(unitCard, attackerSide[slotIndex], x ? attackerSide[slotIndex].GetAttackDamage() : 0);

                    CheckAndHandleAllUnitDeaths();
                }
                else
                {
                    DealDamageToPlayer(_clientId, attackerSide[slotIndex].GetAttackDamage());

                    HandleVampiric(unitCard, attackerSide[slotIndex], attackerSide[slotIndex].GetAttackDamage());
                }
            }

            if (slotIndex < 4)
            {
                if (defenderSide[slotIndex + 1])
                {
                    bool x = DealDamageToUnit(defenderSide, slotIndex + 1, attackerSide[slotIndex].GetAttackDamage());

                    HandleVampiric(unitCard, attackerSide[slotIndex], x ? attackerSide[slotIndex].GetAttackDamage() : 0);

                    CheckAndHandleAllUnitDeaths();
                }
                else
                {
                    DealDamageToPlayer(_clientId, attackerSide[slotIndex].GetAttackDamage());

                    HandleVampiric(unitCard, attackerSide[slotIndex], attackerSide[slotIndex].GetAttackDamage());
                }
            }
        }
    }

    private void HandleVampiric(UnitCardSO _unitCard, UnitCard _unit, int _damage)
    {
        if (_unitCard.ContainsTrait(TraitsEnum.Vampiric))
            _unit.GiveOverhealth(_damage, true);
    }

    private bool DealDamageToUnit(UnitCard[] _sideCards, int _unitToDamageIndex, int _incomingDamage)
    {
        if (_unitToDamageIndex > 0 && _sideCards[_unitToDamageIndex - 1] && ((UnitCardSO)cardDict[_sideCards[_unitToDamageIndex - 1].GetCardName()]).ContainsTrait(TraitsEnum.Shielding))
        {
            return _sideCards[_unitToDamageIndex - 1].TakeDamage(_incomingDamage);
        }
        else if (_unitToDamageIndex < 4 && _sideCards[_unitToDamageIndex + 1] && ((UnitCardSO)cardDict[_sideCards[_unitToDamageIndex + 1].GetCardName()]).ContainsTrait(TraitsEnum.Shielding))
        {
            return _sideCards[_unitToDamageIndex + 1].TakeDamage(_incomingDamage);
        }
        else
        {
            return _sideCards[_unitToDamageIndex].TakeDamage(_incomingDamage);
        }
    }

    private void DealDamageToPlayer(int _attackingClientId, int _damage)
    {
        if (_attackingClientId == 1)
        {
            player2Health -= _damage;
            if (player2Health <= 0)
                EndGameRpc(1);
        }
        else if (_attackingClientId == 2)
        {
            player1Health -= _damage;
            if (player1Health <= 0)
                EndGameRpc(2);
        }
        UpdatePlayerHealthInfoRpc(player1Health, player2Health);
    }

    private void CheckAndHandleAllUnitDeaths()
    {
        for (int i = 0; i < player1Units.Length; i++)
        {
            if (player1Units[i] && player1Units[i].GetCurrentHealth() <= 0)
            {
                HandleDeathTraits(1, i);
                Destroy(player1Units[i].gameObject);
                player1Units[i] = null;
            }

            if (player2Units[i] && player2Units[i].GetCurrentHealth() <= 0)
            {
                HandleDeathTraits(2, i);
                Destroy(player2Units[i].gameObject);
                player2Units[i] = null;
            }
        }
    }
    
    private void HandleDeathTraits(int _clientId, int _unitSlotIndex)
    {
        UnitCard[] sideCards = _clientId == 1 ? player1Units : player2Units;
        UnitCard[] enemySideCards = _clientId == 2 ? player1Units : player2Units;

        UnitCardSO unitCard = (UnitCardSO)cardDict[sideCards[_unitSlotIndex].GetCardName()];

        if (unitCard.ContainsTrait(TraitsEnum.LastLaugh))
        {
            if (enemySideCards[_unitSlotIndex])
                DealDamageToUnit(enemySideCards, _unitSlotIndex, unitCard.GetTraitValue(TraitsEnum.LastLaugh));
            else
                DealDamageToPlayer(_clientId, unitCard.GetTraitValue(TraitsEnum.LastLaugh));
        }

        if (unitCard.ContainsTrait(TraitsEnum.PartingGift))
        {
            if (_unitSlotIndex > 0 && sideCards[_unitSlotIndex - 1])
                sideCards[_unitSlotIndex - 1].GiveOverhealth(unitCard.GetTraitValue(TraitsEnum.PartingGift), currentPlayerTurn == _clientId);

            if (_unitSlotIndex < 4 && sideCards[_unitSlotIndex + 1])
                sideCards[_unitSlotIndex + 1].GiveOverhealth(unitCard.GetTraitValue(TraitsEnum.PartingGift), currentPlayerTurn == _clientId);
        }
    }

    private void StartPositionTraits(UnitCard[] _sideCards)
    {
        for (int i = 0; i < _sideCards.Length; i++)
        {
            if (!_sideCards[i])
                continue;

            if (i > 0 && _sideCards[i - 1])
            {
                UnitCardSO leftUnit = (UnitCardSO)cardDict[_sideCards[i - 1].GetCardName()];

                if (leftUnit.ContainsTrait(TraitsEnum.ProtectiveAura))
                {
                    _sideCards[i].GiveOverhealth(leftUnit.GetTraitValue(TraitsEnum.ProtectiveAura), true);
                }

                if (leftUnit.ContainsTrait(TraitsEnum.InspiringAura))
                {
                    _sideCards[i].GiveInspiration(leftUnit.GetTraitValue(TraitsEnum.InspiringAura), true);
                }
            }

            if (i < 4 && _sideCards[i + 1])
            {
                UnitCardSO rightUnit = (UnitCardSO)cardDict[_sideCards[i + 1].GetCardName()];

                if (rightUnit.ContainsTrait(TraitsEnum.InspiringAura))
                {
                    _sideCards[i].GiveInspiration(rightUnit.GetTraitValue(TraitsEnum.InspiringAura), true);
                }

                if (rightUnit.ContainsTrait(TraitsEnum.ProtectiveAura))
                {
                    _sideCards[i].GiveOverhealth(rightUnit.GetTraitValue(TraitsEnum.ProtectiveAura), true);
                }
            }
        }
    }

    private bool IsValidSlotIndex(Vector2Int _slotIndex)
    {
        bool isValid = true;
        if (_slotIndex.x < 0 || _slotIndex.x > 1 || _slotIndex.y < 0 || _slotIndex.y > 4)
        {
            isValid = false;
            Debug.LogError("OUT OF BOUNDS SLOT INFO REQUESTED: " + _slotIndex);
        }
        return isValid;
    }

    public void DrawPlayer1Card()
    {
        if (player1Deck.Count == 0)
        {
            Debug.LogError("PLAYER 1 ATTEMPTED TO DRAW FROM EMPTY DECK");
            return;
        }
        
        HandCardData cardData = player1Deck.Pop();

        if (!player1Hand.TryAdd(cardData.cardName, 1))
            player1Hand[cardData.cardName]++;
        
        handManager.DrawCardRpc(cardData.ToNetworkCardData(), RpcTarget.Single(1, RpcTargetUse.Temp));
        print("Player 1 Deck Length: " + player1Deck.Count);
    }

    public void DrawPlayer2Card()
    {
        if (player2Deck.Count == 0)
        {
            Debug.LogError("PLAYER 2 ATTEMPTED TO DRAW FROM EMPTY DECK");
            return;
        }
        
        HandCardData cardData = player2Deck.Pop();
        
        if (!player2Hand.TryAdd(cardData.cardName, 1))
            player2Hand[cardData.cardName]++;
        
        handManager.DrawCardRpc(cardData.ToNetworkCardData(), RpcTarget.Single(2, RpcTargetUse.Temp));
        print("Player 2 Deck Length: " + player2Deck.Count);
    }
#endregion

#region GLOBAL_GAME_STATE
    [Rpc(SendTo.Server)]
    public void TryNextTurnRpc(int _clientId)
    {
        if (_clientId != currentPlayerTurn)
        {
            Debug.LogError("WRONG PLAYER ATTEMPTED TO GO TO NEXT TURN");
            return;
        }

        NextTurn();
    }

    public void NextTurn()
    {
        handManager.SetPlayerTurnRpc(false, RpcTarget.Single((ulong)currentPlayerTurn, RpcTargetUse.Temp));
        currentPlayerTurn = currentPlayerTurn == 1 ? 2 : 1;
        handManager.SetPlayerTurnRpc(true, RpcTarget.Single((ulong)currentPlayerTurn, RpcTargetUse.Temp));

        if (currentPlayerTurn == 1)
        {
            NextRound();

            ResetUnitResources(player1Units, player2Units);
            StartPositionTraits(player1Units);
            DrawPlayer1Card();
            energyManager.ResetPlayer1CurrentEnergy();
        }
        else if (currentPlayerTurn == 2)
        {
            ResetUnitResources(player2Units, player1Units);
            StartPositionTraits(player2Units);
            DrawPlayer2Card();
            energyManager.ResetPlayer2CurrentEnergy();
        }
    }

    private void NextRound()
    {
        round++;
        clientUIManager.SetRoundCounterRpc(round);
        energyManager.IncrementMaxEnergy();
    }

    private void ResetUnitResources(UnitCard[] _playerUnits, UnitCard[] _enemyUnits)
    {
        foreach (var unit in _playerUnits)
        {
            if (!unit)
                continue;
            unit.RestoreAction();
            unit.ResetHealth();
            unit.ResetPlayerTurnTempStatusEffects();
        }

        foreach (var unit in _enemyUnits)
        {
            if (!unit)
                continue;
            unit.ResetEnemyTurnTempStatusEffects();
        }
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void UpdatePlayerHealthInfoRpc(int _player1Health, int _player2Health)
    {
        player1HealthText.text = _player1Health.ToString();
        player2HealthText.text = _player2Health.ToString();
    }

    [Rpc(SendTo.ClientsAndHost)]
    private void EndGameRpc(int _winningClientId)
    {
        if (NetworkManager.Singleton.LocalClientId > 2)
        {
            outcomeText.text = $"PLAYER {_winningClientId} WINS";
            return;
        }
        
        handManager.HandleEndGame();
        handManager.gameObject.SetActive(false);

        if ((int)NetworkManager.Singleton.LocalClientId == _winningClientId)
        {
            outcomeText.text = "YOU WIN";
            outcomeText.color = Color.green;
        }
        else
        {
            outcomeText.text = "YOU LOSE";
            outcomeText.color = Color.red;
        }
    }
#endregion
}
