using System.Threading.Tasks;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using Unity.Netcode;
using Unity.Networking.Transport.Relay;
using Unity.Netcode.Transports.UTP;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
   public static RelayManager Instance { get; private set; }

   private const string ConnectionType = "dtls";

   private void Awake()
   {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        
   }

   public async Task<string> StartHosWithRelayAsync (int maxConnections)
   {
        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(maxConnections);
        string joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        var relayServerData = AllocationUtils.ToRelayServerData(allocation, ConnectionType);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);

       

        Debug.Log("host started with relay . join code : " + joinCode);
        Debug.Log("About to call StartHost");
        bool hostStarted = NetworkManager.Singleton.StartHost();
        Debug.Log("StartHost returned: " + hostStarted);

        if (!hostStarted)
        {
            Debug.LogError("Failed to start host.");
            return null;
        }

        Debug.Log("Host started with Relay. Join code: " + joinCode);

        return joinCode;
   }

    public async Task<bool> StartClientWithRelayAsync(string joinCode)
    {
        JoinAllocation allocation = await RelayService.Instance.JoinAllocationAsync(joinCode);
        var RelayServerData = AllocationUtils.ToRelayServerData(allocation, ConnectionType);
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(RelayServerData);
        

        Debug.Log("About to call StartClient");
        bool clientStarted = NetworkManager.Singleton.StartClient();
        Debug.Log("StartClient returned: " + clientStarted);

        if (!clientStarted)
        {
            Debug.LogError("Failed to start client.");
            return false;
        }
        Debug.Log(" client started with relay ");
        return true;

    }




}
