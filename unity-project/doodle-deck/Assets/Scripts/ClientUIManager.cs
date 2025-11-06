using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ClientUIManager : NetworkBehaviour
{
    [SerializeField] Camera cam1;
    [SerializeField] Camera cam2;
    [SerializeField] TextMeshProUGUI playerNumText;
    [SerializeField] TextMeshProUGUI roundCounterText;
    [SerializeField] GameObject nextTurnButton;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerNumText.text = "Server";
            EnableNextTurnButton(false);
            return;
        }
        print("CONNECTED");
        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            cam2.enabled = false;
            playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
        }
        else if (NetworkManager.Singleton.LocalClientId == 2)
        {
            cam1.enabled = false;
            playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
        }
        else
        {
            playerNumText.text = "Spectator " + (NetworkManager.Singleton.LocalClientId - 2);
            cam2.enabled = false;
            gameObject.SetActive(false);
        }
    }

    public void HandleNextTurnButtonClick()
    {
        GameManager.Instance.TryNextTurnRpc((int)NetworkManager.LocalClientId);
    }

    public void EnableNextTurnButton(bool _enable)
    {
        nextTurnButton.SetActive(_enable);
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void SetRoundCounterRpc(int _round)
    {
        roundCounterText.text = "ROUND " + _round;
    }
}
