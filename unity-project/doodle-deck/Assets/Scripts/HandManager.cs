using System;
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
    
    private List<UnitUICard> uiCards = new List<UnitUICard>();
    
    private Camera mainCam;

    private UnitUICard clickedCard;

    private Vector2Int? selectedSlotIndex = null;
    private bool isSelectedSlotOccupied = false;
    
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
            playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
        }
        else if (NetworkManager.Singleton.LocalClientId == 2)
        {
            cam1.enabled = false;
            mainCam = cam2;
            playerNumText.text = "Player " + NetworkManager.Singleton.LocalClientId;
        }
        else
        {
            playerNumText.text = "Spectator " + (NetworkManager.Singleton.LocalClientId - 2);
            cam2.enabled = false;
            gameObject.SetActive(false);
        }
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
                        Vector2Int slotInfo = slotHandler.HandleClick();
                        HandleSlotClick(slotInfo);
                    }
                }
            }
        }
    }

    private void HandleSlotClick(Vector2Int _slotIndex)
    {
        if (clickedCard)
        {
            GameManager.Instance.TryPlayCardRpc((int)NetworkManager.Singleton.LocalClientId, clickedCard.GetInfo(), _slotIndex);
        }
        else
        {
            GameManager.Instance.RequestSlotInfoRpc((int)NetworkManager.Singleton.LocalClientId, _slotIndex);
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void ReceiveSlotInfoRpc(NetworkCardData _cardData, Vector2Int _slotIndex, RpcParams _rpcParams)
    {
        string cardName = _cardData.cardName.ToString();
        print("Just received data on: " + cardName + " at slot: " + _slotIndex);

        int localClientID = (int)NetworkManager.Singleton.LocalClientId;
        if (selectedSlotIndex != null)
        {
            if (selectedSlotIndex.Value == _slotIndex)
            {
                selectedSlotIndex = null;
                isSelectedSlotOccupied = false;
                return;
            }

            if (!isSelectedSlotOccupied)
                return;

            
            if (Math.Abs(selectedSlotIndex.Value.y - _slotIndex.y) == 1 && localClientID == selectedSlotIndex.Value.x && localClientID == _slotIndex.x)
            {
                GameManager.Instance.TryMoveUnitRpc(localClientID, selectedSlotIndex.Value, _slotIndex);
            }
            else if (selectedSlotIndex.Value.y == _slotIndex.y && localClientID == selectedSlotIndex.Value.x + 1 && localClientID != _slotIndex.x + 1)
            {
                GameManager.Instance.TryAttackUnitRpc(localClientID, selectedSlotIndex.Value, _slotIndex);
            }
            
            selectedSlotIndex = null;
            isSelectedSlotOccupied = false;
        }
        else if (_slotIndex.x + 1 == localClientID)
        {
            selectedSlotIndex = _slotIndex;
            isSelectedSlotOccupied = (cardName != "");
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void RemoveCardFromHandRpc(RpcParams _rpcParams)
    {
        uiCards.Remove(clickedCard);
        Destroy(clickedCard.gameObject);
        clickedCard = null;
    }
    

    [Rpc(SendTo.SpecifiedInParams)]
    public void DrawCardRpc(NetworkCardData _card, RpcParams _rpcParams)
    {
        HandCardData drawnCard = new HandCardData(_card.cardName.ToString(), _card.cardType);
        var unitSO = GameManager.Instance.cardDict[drawnCard.cardName];
        UnitUICard uiCard = Instantiate(unitUICardPrefab, handObject.transform).GetComponent<UnitUICard>();
        uiCard.InitializeCard(this, (UnitCardSO)unitSO);
        uiCards.Add(uiCard);
        print(drawnCard.cardName);
    }

    public void HandleUICardClick(UnitUICard _uiCard)
    {
        if (clickedCard == null)
        {
            clickedCard = _uiCard;
            print(clickedCard.GetInfo());
        }
        else
        {
            if (_uiCard == clickedCard)
            {
                clickedCard = null;
                print("clickCard set null");
            }
            else
            {
                clickedCard = _uiCard;
                print(clickedCard.GetInfo());
            }
        }
    }

    public void HandleEndGame()
    {
        handObject.SetActive(false);
    }
}
