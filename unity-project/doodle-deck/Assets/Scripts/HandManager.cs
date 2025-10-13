using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandManager : NetworkBehaviour
{
    [SerializeField] Camera cam1;
    [SerializeField] Camera cam2;
    [SerializeField] TextMeshProUGUI playerNumText;
    [SerializeField] GameObject handObject;
    [SerializeField] GameObject unitUICardPrefab;
    
    private List<HandCardData> cards = new List<HandCardData>();
    
    private Camera mainCam;
    
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
            mainCam = cam1;
        }
        else
        {
            cam1.enabled = false;
            mainCam = cam2;
        }

        playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
    }

    void Update()
    {
        if (IsClient)
        {
            if (Input.GetMouseButtonDown(0) && EventSystem.current && !EventSystem.current.IsPointerOverGameObject())
            {
                Ray ray = mainCam.ScreenPointToRay(Input.mousePosition);
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    if (hit.collider.TryGetComponent(out SlotHandler slotHandler))
                    {
                        print(slotHandler.HandleClick());
                    }
                    TestServerRpc(hit.collider.name, (int)NetworkManager.Singleton.LocalClientId);
                }
            }
        }
    }
    
    [Rpc(SendTo.Server)]
    private void TestServerRpc(string _colName, int _clientId)
    {
        print(_clientId + " Hit: " + _colName);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void DrawCardRpc(NetworkCardData _card, RpcParams _rpcParams)
    {
        HandCardData drawnCard = new HandCardData(_card.cardName.ToString(), _card.cardType);
        cards.Add(drawnCard);
        var x = GameManager.Instance.cardDict[drawnCard.cardName];
        UnitUICard uiCard = Instantiate(unitUICardPrefab, handObject.transform).GetComponent<UnitUICard>();
        uiCard.InitializeCard(x);
        print(drawnCard.cardName);
    }
}
