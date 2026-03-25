using Unity.Netcode;
using UnityEngine;

public class PlayerSpawn : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        Debug.Log($"Player spawned. OwnerClientId = {OwnerClientId}, IsOwner= {IsOwner}, IsServer= {IsServer}");
    }
}
