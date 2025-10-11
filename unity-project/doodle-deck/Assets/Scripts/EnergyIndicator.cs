using Unity.Netcode;
using UnityEngine;

public class EnergyIndicator : NetworkBehaviour
{
    [SerializeField] Material offMat;
    [SerializeField] Material onMat;

    private MeshRenderer meshRenderer;

    private bool IsOn { set { meshRenderer.material = value ? onMat : offMat; } }

    void Awake()
    {
        meshRenderer = GetComponent<MeshRenderer>();
        IsOn = false;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TurnOnRpc()
    {
        IsOn = true;
    }

    [Rpc(SendTo.ClientsAndHost)]
    public void TurnOffRpc()
    {
        IsOn = false;
    }
}
