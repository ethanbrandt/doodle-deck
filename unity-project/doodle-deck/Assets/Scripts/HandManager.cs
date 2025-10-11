using System;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class HandManager : NetworkBehaviour
{
    [SerializeField] Camera cam1;
    [SerializeField] Camera cam2;
    [SerializeField] TextMeshProUGUI playerNumText;
    
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            playerNumText.text = "Server";
            return;
        }
        print("CONNECTED");
        if (NetworkManager.Singleton.LocalClientId == 1)
        {
            cam2.enabled = false;
        }
        else
        {
            cam1.enabled = false;
        }

        playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
    }

    void Update()
    {
        if (IsClient)
        {
            if (Input.GetMouseButtonDown(0))
            {
                Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit))
                    TestServerRpc(hit.collider.name, (int)NetworkManager.Singleton.LocalClientId);
            }
        }

    }
    
    [Rpc(SendTo.Server)]
    private void TestServerRpc(string _colName, int _clientId)
    {
        print(_clientId + " Hit: " + _colName);
    }
}

