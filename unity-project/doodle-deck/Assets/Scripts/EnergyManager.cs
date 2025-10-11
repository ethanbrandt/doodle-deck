using System;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class EnergyManager : NetworkBehaviour
{
    [SerializeField] GameObject energyIndicator;
    private int player1CurrentEnergy;
    private int player2CurrentEnergy;

    private List<EnergyIndicator> player1EnergyIndicators;
    private List<EnergyIndicator> player2EnergyIndicators;

    public int Player1MaxEnergy { get { return player1EnergyIndicators.Count; } }
    public int Player2MaxEnergy { get { return player2EnergyIndicators.Count; } }
    
    public int Player1CurrentEnergy { get { return player1CurrentEnergy; } }
    public int Player2CurrentEnergy { get { return player2CurrentEnergy; } }

    public void InitializeEnergy()
    {
        if (!IsServer)
            return;
        
        player1EnergyIndicators = new List<EnergyIndicator>();
        player2EnergyIndicators = new List<EnergyIndicator>();
        
        SpawnPlayer1EnergyIndicator(0);
        
        SpawnPlayer2EnergyIndicator(0);
        SpawnPlayer2EnergyIndicator(1);
    }

    public void IncrementMaxEnergy()
    {
        if (Player1MaxEnergy < 10)
            SpawnPlayer1EnergyIndicator(Player1MaxEnergy);
        if (Player2MaxEnergy < 10)
            SpawnPlayer2EnergyIndicator(Player2MaxEnergy);
    }

    private void SpawnPlayer1EnergyIndicator(int i)
    {
        Vector3 pos = i < 5 ? new Vector3(4.5f + (0.3f * i), -1.46f, 0.5f) : new Vector3(4.5f + (0.3f * (i - 5)), -1.46f, 0);
        
        GameObject spawnedObject = Instantiate(energyIndicator, pos, Quaternion.identity);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        player1EnergyIndicators.Add(spawnedObject.GetComponent<EnergyIndicator>());
    }

    private void SpawnPlayer2EnergyIndicator(int i)
    {
        Vector3 pos = i < 5 ? new Vector3(-4.5f - (0.3f * i), -1.46f, 3.5f) : new Vector3(-4.5f - (0.3f * (i - 5)), -1.46f, 4);
        
        GameObject spawnedObject = Instantiate(energyIndicator, pos, Quaternion.identity);
        spawnedObject.GetComponent<NetworkObject>().Spawn(true);
        player2EnergyIndicators.Add(spawnedObject.GetComponent<EnergyIndicator>());
    }

    public void ResetCurrentEnergy()
    {
        player1CurrentEnergy = Player1MaxEnergy;
        player2CurrentEnergy = Player2MaxEnergy;

        foreach (var indicator in player1EnergyIndicators)
            indicator.TurnOnRpc();

        foreach (var indicator in player2EnergyIndicators)
            indicator.TurnOnRpc();
    }

    public bool UsePlayer1Energy(int _energyUsed)
    {
        if (player1CurrentEnergy - _energyUsed < 0)
            return false;
        
        player1CurrentEnergy -= _energyUsed;
        for (int i = Player1MaxEnergy - 1; i >= player1CurrentEnergy; i--)
            player1EnergyIndicators[i].TurnOffRpc();
        
        return true;
    }
    
    public bool UsePlayer2Energy(int _energyUsed)
    {
        if (player2CurrentEnergy - _energyUsed < 0)
            return false;
        
        player2CurrentEnergy -= _energyUsed;
        for (int i = Player2MaxEnergy - 1; i >= player2CurrentEnergy; i--)
            player2EnergyIndicators[i].TurnOffRpc();

        return true;
    }
}
