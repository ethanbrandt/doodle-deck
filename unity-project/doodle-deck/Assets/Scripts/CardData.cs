using System;
using Unity.Collections;
using Unity.Netcode;

[Serializable]
public struct HandCardData
{
    public string cardName;
    public CardType cardType;

    public HandCardData(string _cardName, CardType _cardType)
    {
        cardName = _cardName;
        cardType = _cardType;
    }
    
    public NetworkCardData ToNetworkCardData()
    {
        return new NetworkCardData(cardName, cardType);
    }
}

public struct NetworkCardData : INetworkSerializable
{
    public FixedString128Bytes cardName;
    public CardType cardType;

    public NetworkCardData(string _cardName, CardType _cardType)
    {
        cardName = _cardName;
        cardType = _cardType;
    }
    
    public void NetworkSerialize<T>(BufferSerializer<T> serializer) where T : IReaderWriter
    {
        serializer.SerializeValue(ref cardName);
        serializer.SerializeValue(ref cardType);
    }
}

public enum CardType
{
    Unit,
    Spell,
    SwiftSpell
}

[Serializable]
public struct CardTrait
{
    public TraitsEnum trait;
    public int val;
}

public enum TraitsEnum
{
    LightFooted,
    HealingTrail,
    DriveBy,
    Opportunistic,
    Immobile,
    ProtectiveAura,
    InspiringAura,
    Shielding,
    Cleave,
    Vampiric,
    SneakAttack,
    Braced,
    GrandEntrance,
    LastLaugh,
    ExplosiveExit,
    PartingGift,
    Overtime,
    LeftLauncher,
    RightLauncher
}

public enum StatusEffect
{
    Fatigued,
    Intimidated,
    Warded
}