using UnityEngine;

[CreateAssetMenu(fileName = "DeckSO", menuName = "Scriptable Objects/DeckSO")]
public class DeckSO : ScriptableObject
{
    [SerializeField] CardBaseSO[] deck;

    public NetworkCardData[] GetNetowrkDeck()
    {
        NetworkCardData[] networkDeck = new NetworkCardData[deck.Length];

        for (int i = 0; i < deck.Length; i++)
        {
            CardType cardType = CardType.Unit;
            if (deck[i] is SpellCardSO)
                cardType = ((SpellCardSO)deck[i]).isSwift ? CardType.SwiftSpell : CardType.Spell;
            
            networkDeck[i] = new NetworkCardData(deck[i].cardName, cardType);
        }

        return networkDeck;
    }
}
