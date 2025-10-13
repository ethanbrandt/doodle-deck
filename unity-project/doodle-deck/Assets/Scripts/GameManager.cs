using System;
using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;
using Random = UnityEngine.Random;

public class GameManager : NetworkBehaviour
{
    [SerializeField] GameObject unitCardPrefab;
    [SerializeField] Transform[] slots;
    [SerializeField] float cardHoverDist;
    
    [SerializeField] EnergyManager energyManager;
    [SerializeField] HandManager handManager;

    [SerializeField] HandCardData[] deck;
    
    private readonly Stack<HandCardData> player1Deck = new Stack<HandCardData>();
    private readonly Stack<HandCardData> player2Deck = new Stack<HandCardData>();

    private readonly Dictionary<string, int> player1Hand = new Dictionary<string, int>();
    private readonly Dictionary<string, int> player2Hand = new Dictionary<string, int>();

    public UnitCard[] player1Units = new UnitCard[5];
    public UnitCard[] player2Units = new UnitCard[5];
    
    public static GameManager Instance { get; private set; }

    public readonly Dictionary<string, CardBaseSO> cardDict = new Dictionary<string, CardBaseSO>();
    
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("MULTIPLE GAMEMANAGER INSTANCE (THIS WILL CAUSE ISSUES)");
        }
        
        Instance = this;

        CardBaseSO[] loadedUnits = Resources.LoadAll<CardBaseSO>("");

        foreach (var unit in loadedUnits)
        {
            cardDict.Add(unit.cardName, unit);
        }
    }

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
            energyManager.InitializeEnergy();
            foreach (var card in shuffledList)
                player2Deck.Push(new HandCardData(card.cardName.ToString(), card.cardType));
        }
    }

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
        if (!player1Hand.ContainsKey(_cardName))
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

        if (card is not UnitCardSO)
        {
            // TODO: INSERT SPELL LOGIC HERE
            Debug.LogWarning("UNIMPLEMENTED SPELL ATTEMPTED TO BE CAST");
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

        energyManager.UsePlayer1Energy(card.energyCost);
        player1Units[_slotIndex.y] = unitCard;
        player1Hand.Remove(_cardName);
        handManager.RemoveCardFromHandRpc(RpcTarget.Single(1, RpcTargetUse.Temp));
    }

    private void TryPlayPlayer2Card(string _cardName, Vector2Int _slotIndex)
    {
        if (!player2Hand.ContainsKey(_cardName))
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

        if (card is not UnitCardSO)
        {
            // TODO: INSERT SPELL LOGIC HERE
            Debug.LogWarning("UNIMPLEMENTED SPELL ATTEMPTED TO BE CAST");
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

        energyManager.UsePlayer2Energy(card.energyCost);
        player2Units[_slotIndex.y] = unitCard;
        player2Hand.Remove(_cardName);
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
        if (_clientId != _startSlotIndex.x + 1 || _clientId != _endSlotIndex.x + 1)
        {
            Debug.LogError($"Player {_clientId} ATTEMPTED TO MOVE CARD ON ENEMY SIDE");
            return;
        }

        if (Math.Abs(_startSlotIndex.y - _endSlotIndex.y) != 1)
        {
            Debug.LogWarning($"Player {_clientId} ATTEMPTED TO MOVE CARD MORE THAN 1 SLOT");
            return;
        }

        if (!IsValidSlotIndex(_startSlotIndex) || !IsValidSlotIndex(_endSlotIndex))
            return;
        
        UnitCard[] sideCards = _clientId == 1 ? player1Units : player2Units;

        
        print($"Moving card at {_startSlotIndex} to {_endSlotIndex}");
        print($"Updated transform from {sideCards[_startSlotIndex.y].transform.position} to {slots[(_startSlotIndex.x * 5) + _startSlotIndex.y].position}");
        (sideCards[_endSlotIndex.y], sideCards[_startSlotIndex.y]) = (sideCards[_startSlotIndex.y], sideCards[_endSlotIndex.y]); // SWAP THE CARDS SLOTS

        if (sideCards[_startSlotIndex.y])
            sideCards[_startSlotIndex.y].transform.position = slots[(_startSlotIndex.x * 5) + _startSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);

        if (sideCards[_endSlotIndex.y])
            sideCards[_endSlotIndex.y].transform.position = slots[(_endSlotIndex.x * 5) + _endSlotIndex.y].position + new Vector3(0, cardHoverDist, 0);
        
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
        HandCardData cardData = player1Deck.Pop();

        if (!player1Hand.TryAdd(cardData.cardName, 1))
            player1Hand[cardData.cardName]++;
        
        handManager.DrawCardRpc(cardData.ToNetworkCardData(), RpcTarget.Single(1, RpcTargetUse.Temp));
    }

    public void DrawPlayer2Card()
    {
        HandCardData cardData = player2Deck.Pop();
        
        if (!player2Hand.TryAdd(cardData.cardName, 1))
            player2Hand[cardData.cardName]++;
        
        handManager.DrawCardRpc(cardData.ToNetworkCardData(), RpcTarget.Single(2, RpcTargetUse.Temp));
    }
    
    void Update()
    {
        
    }
}
