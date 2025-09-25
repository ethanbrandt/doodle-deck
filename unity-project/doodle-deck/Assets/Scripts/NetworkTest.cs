using Unity.Netcode;
using UnityEngine;

public class NetworkTest : NetworkBehaviour 
{
    void Start()
    {
        
    }

    void Update()
    {
        if (IsClient)
        {
            if (Input.GetKeyDown(KeyCode.A))
            {
                TestServerRpc("super cool message");
                print("pressed A on client");
            }
        }
    }

    [Rpc(SendTo.Server)]
    private void TestServerRpc(string message)
    {
        print("Test Server RPCed");
        print(message);
    }
}
