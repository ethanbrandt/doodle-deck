using System;
using System.Collections.Generic;
using TMPro;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;

public class HandManager : NetworkBehaviour
{
    [SerializeField] GameObject handObject;
    [SerializeField] GameObject unitUICardPrefab;
    [SerializeField] GameObject spellUICardPrefab;
    
    private List<UICard> uiCards = new List<UICard>();

    private ClientUIManager clientUIManager;
    
    private Camera mainCam = null;

    private UICard clickedCard;

    private bool isPlayersTurn = false;

    private Vector2Int? selectedSlotIndex = null;

    public override void OnNetworkSpawn()
    {
        if (NetworkManager.LocalClientId > 2)
            gameObject.SetActive(false);
    }

    private void Start()
    {
        clientUIManager = FindFirstObjectByType<ClientUIManager>();
    }

    void Update()
    {
        if (IsClient)
        {
            if (!mainCam)
                mainCam = Camera.main;
            
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
                return;
            }

            if (Math.Abs(selectedSlotIndex.Value.y - _slotIndex.y) <= 2 && localClientID == selectedSlotIndex.Value.x + 1 && localClientID == _slotIndex.x + 1)
            {
                GameManager.Instance.TryMoveUnitRpc(localClientID, selectedSlotIndex.Value, _slotIndex);
            }
            else if (selectedSlotIndex.Value.y == _slotIndex.y && localClientID == selectedSlotIndex.Value.x + 1 && localClientID != _slotIndex.x + 1)
            {
                GameManager.Instance.TryAttackUnitRpc(localClientID, selectedSlotIndex.Value, _slotIndex);
            }
            
            selectedSlotIndex = null;
        }
        else if (_slotIndex.x + 1 == localClientID && cardName != "")
        {
            selectedSlotIndex = _slotIndex;
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
        print("DRAWN CARD: " + _card.cardName);
        HandCardData drawnCard = new HandCardData(_card.cardName.ToString(), _card.cardType);
        var unitSO = GameManager.Instance.cardDict[drawnCard.cardName];
        UnitUICard uiCard = Instantiate(unitUICardPrefab, handObject.transform).GetComponent<UnitUICard>();
        uiCard.InitializeCard(this, (UnitCardSO)unitSO);
        uiCards.Add(uiCard);
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void SetPlayerTurnRpc(bool _isPlayersTurn, RpcParams _rpcParams)
    {
        isPlayersTurn = _isPlayersTurn;

        clientUIManager.EnableNextTurnButton(isPlayersTurn);
        if (isPlayersTurn)
        {
            print("PLAYER'S TURN");
            //* RESET UNIT ACTIONS
        }
    }

    public void HandleUICardClick(UICard _uiCard)
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
