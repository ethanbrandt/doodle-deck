using Unity.Netcode;
using UnityEngine;

public class GameManager : NetworkBehaviour
{
    [SerializeField] EnergyManager energyManager;

    public static GameManager Instance { get; private set; }
    
    void Awake()
    {
        if (Instance != null)
        {
            Debug.LogError("MULTIPLE GAMEMANAGER INSTANCE (THIS WILL CAUSE ISSUES)");
        }
        
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        if (IsClient)
        {
            ClientConnectRpc((int)NetworkManager.Singleton.LocalClientId);
        }
    }

    [Rpc(SendTo.Server)]
    private void ClientConnectRpc(int _clientId)
    {
        print(_clientId + " Connected to Server");
        if (_clientId == 1)
            SendSecretRpc("SUPER SECRET MESSAGE MEANT ONLY FOR 1", RpcTarget.Single(1, RpcTargetUse.Temp));
        if (_clientId == 2)
        {
            SendSecretRpc("ALSO COOL SECRET MEANT FOR 2", RpcTarget.Single(2, RpcTargetUse.Temp));
            print("BOTH PLAYERS CONNECTED");
            energyManager.InitializeEnergy();
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    private void SendSecretRpc(string _secret, RpcParams _rpcParams)
    {
        print(_secret);
    }

    void Update()
    {
        if (IsServer)
        {
            if (Input.GetKeyDown(KeyCode.F))
                energyManager.IncrementMaxEnergy();
            else if (Input.GetKeyDown(KeyCode.G))
                energyManager.ResetCurrentEnergy();
            else if (Input.GetKeyDown(KeyCode.H))
                energyManager.UsePlayer1Energy(1);
            else if (Input.GetKeyDown(KeyCode.J))
                energyManager.UsePlayer2Energy(1);
        }
    }
}
