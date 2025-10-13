using Unity.Netcode;
using UnityEngine;

public class DebugUIManager : MonoBehaviour
{
    [SerializeField] GameObject startButtons;
    [SerializeField] GameObject debugButtons;
    
    private EnergyManager energyManager;
    
    void Start()
    {
        energyManager = FindFirstObjectByType<EnergyManager>();
        GetComponent<Canvas>().enabled = true;
        debugButtons.SetActive(false);
    }

    public void StartServer()
    {
        startButtons.SetActive(false);
        debugButtons.SetActive(true);
        NetworkManager.Singleton.StartServer();
    }

    public void StartClient()
    {
        startButtons.SetActive(false);
        GetComponent<Canvas>().enabled = false;
        NetworkManager.Singleton.StartClient();
    }

    public void IncrementMaxEnergy()
    {
        energyManager.IncrementMaxEnergy();
    }

    public void ResetCurrentEnergy()
    {
        energyManager.ResetCurrentEnergy();
    }

    public void UsePlayer1Energy()
    {
        energyManager.UsePlayer1Energy(1);
    }

    public void UsePlayer2Energy()
    {
        energyManager.UsePlayer2Energy(1);
    }

    public void DrawPlayer1Card()
    {
        GameManager.Instance.DrawPlayer1Card();
    }

    public void DrawPlayer2Card()
    {
        GameManager.Instance.DrawPlayer2Card();
    }
}
