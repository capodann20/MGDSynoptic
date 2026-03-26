using Unity.Netcode;
using UnityEngine;

public class PlayerMarker : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        var renderer = GetComponent<Renderer>();
        if (renderer == null) return;

        renderer.material.color = IsOwner ? Color.green : Color.red;
    }

}
