using System.Collections.Generic;
using System.Linq;
using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] EnergyManager energyManager;
    [SerializeField] HandManager handManager;

    [SerializeField] HandCardData[] deck;
    
    private readonly Stack<HandCardData> player1Deck = new Stack<HandCardData>();
    private readonly Stack<HandCardData> player2Deck = new Stack<HandCardData>();
    
    public static GameManager Instance { get; private set; }

    public readonly Dictionary<string, UnitCardSO> cardDict = new Dictionary<string, UnitCardSO>();
    
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("MULTIPLE GAMEMANAGER INSTANCE (THIS WILL CAUSE ISSUES)");
        }
        
        Instance = this;

        UnitCardSO[] loadedUnits = Resources.LoadAll<UnitCardSO>("");

        foreach (var unit in loadedUnits)
        {
            cardDict.Add(unit.cardName, unit);
        }
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
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
        NetworkCardData[] shuffledList = _deck.OrderBy(_ => Random.value).ToArray();
        
        print(_clientId + " Connected to Server");
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


    public void DrawPlayer1CardDebug()
    {
        handManager.DrawCardRpc(player1Deck.Pop().ToNetworkCardData(), RpcTarget.Single(1, RpcTargetUse.Temp));
    }

    public void DrawPlayer2CardDebug()
    {
        handManager.DrawCardRpc(player2Deck.Pop().ToNetworkCardData(), RpcTarget.Single(2, RpcTargetUse.Temp));
    }
    
    void Update()
    {
        
    }
}
