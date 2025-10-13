using UnityEngine;

public class SlotHandler : MonoBehaviour
{
    [SerializeField] int slotNum;
    [SerializeField] bool isPlayer1Side;

    public Vector2Int HandleClick()
    {
        int x = isPlayer1Side ? 0 : 1;
        return new Vector2Int(x, slotNum);
    }
}
