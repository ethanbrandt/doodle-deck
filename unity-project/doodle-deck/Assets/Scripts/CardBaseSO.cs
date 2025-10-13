using UnityEngine;

public abstract class CardBaseSO : ScriptableObject
{
    public string cardName;
    public Sprite cardImage;
    [TextArea(10, 10)]
    public string cardDescription;
    [Range(1,10)]
    public int energyCost;
}
