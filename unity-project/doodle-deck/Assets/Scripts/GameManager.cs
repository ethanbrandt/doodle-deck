using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.Netcode;
using UnityEngine;
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

    [SerializeField] HandCardData[] deck;
    
    private readonly Stack<HandCardData> player1Deck = new Stack<HandCardData>();
    private readonly Stack<HandCardData> player2Deck = new Stack<HandCardData>();

    private readonly Dictionary<string, int> player1Hand = new Dictionary<string, int>();
    private readonly Dictionary<string, int> player2Hand = new Dictionary<string, int>();

    public UnitCard[] player1Units = new UnitCard[5];
    public UnitCard[] player2Units = new UnitCard[5];

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
            
            NetworkCardData[] networkDeck = new NetworkCardData[deck.Length];

            for (int i = 0; i < deck.Length; i++)
            {
                networkDeck[i] = deck[i].ToNetworkCardData();
            }
            print("SENDING DECK");
            ClientConnectRpc((int)NetworkManager.Singleton.LocalClientId, networkDeck);
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
                MoveUnit(sideCards, _startSlotIndex, new Vector2Int(_endSlotIndex.x, firstSwap));
                HandleHealingTrail(sideCards, unit, _startSlotIndex);
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
        MoveUnit(sideCards, _startSlotIndex, _endSlotIndex);
        HandleHealingTrail(sideCards, unit, _startSlotIndex);
        sideCards[_startSlotIndex.y].UseAction();
    }

    private void MoveUnit(UnitCard[] sideCards, Vector2Int _startSlotIndex, Vector2Int _endSlotIndex)
    {
        (sideCards[_endSlotIndex.y], sideCards[_startSlotIndex.y]) = (sideCards[_startSlotIndex.y], sideCards[_endSlotIndex.y]); // SWAP THE CARDS SLOTS

        if (sideCards[_startSlotIndex.y])
            sideCards[_startSlotIndex.y].transform.position = slots[(_startSlotIndex.x * 5) + _startSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);

        if (sideCards[_endSlotIndex.y])
            sideCards[_endSlotIndex.y].transform.position = slots[(_endSlotIndex.x * 5) + _endSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);
    }

    private void HandleHealingTrail(UnitCard[] sideCards, UnitCardSO _unit, Vector2Int _movedOverSlotIndex)
    {
        if (!_unit.ContainsTrait(TraitsEnum.HealingTrail))
            return;

        if (sideCards[_movedOverSlotIndex.y])
            sideCards[_movedOverSlotIndex.y].Heal(_unit.GetTraitValue(TraitsEnum.HealingTrail));
    }

    private void HandleDriveBy(UnitCard[] enemySideCards, UnitCardSO _unit, Vector2Int _enemySlotIndex)
    {
        
    }

    private void HandleOpportunistic()
    {
        
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
        
        if (defenderSide[slotIndex])
        {
            defenderSide[slotIndex].TakeDamage(attackerSide[slotIndex].GetAttackDamage());
            attackerSide[slotIndex].TakeDamage(defenderSide[slotIndex].GetAttackDamage());

            if (defenderSide[slotIndex].GetCurrentHealth() <= 0)
            {
                Destroy(defenderSide[slotIndex].gameObject);
                defenderSide[slotIndex] = null;
            }

            if (attackerSide[slotIndex].GetCurrentHealth() <= 0)
            {
                Destroy(attackerSide[slotIndex].gameObject);
                attackerSide[slotIndex] = null;
            }
        }
        else
        {
            if (_clientId == 1)
            {
                player2Health -= attackerSide[slotIndex].GetAttackDamage();
                if (player2Health <= 0)
                    EndGameRpc(1);
            }
            else if (_clientId == 2)
            {
                player1Health -= attackerSide[slotIndex].GetAttackDamage();
                if (player1Health <= 0)
                    EndGameRpc(2);
            }
            UpdatePlayerHealthInfoRpc(player1Health, player2Health);
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
            
            ResetUnitResources(player1Units);
            DrawPlayer1Card();
            energyManager.ResetPlayer1CurrentEnergy();
        }
        else if (currentPlayerTurn == 2)
        {
            ResetUnitResources(player2Units);
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

    private void ResetUnitResources(UnitCard[] _units)
    {
        foreach (var unit in _units)
        {
            if (!unit)
                continue;
            unit.RestoreAction();
            unit.ResetHealth();
            // TODO RESET ALL TEMP EFFECTS
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
