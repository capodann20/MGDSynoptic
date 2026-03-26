using Unity.Netcode;
using UnityEngine;

public class PlayerNetworkMovement : NetworkBehaviour
{
    [SerializeField] private float moveSpeed = 4f;
    [SerializeField] private float rotSpeed = 180f;

    [SerializeField] private float maxMoveDistancePerTrick = 0.35f;
    [SerializeField] private float minX = -6f;
    [SerializeField] private float maxX = 6f;
    [SerializeField] private float minZ = -4f;
    [SerializeField] private float maxZ = 4f;

    [SerializeField] private float positionSmoothSpeed = 12f;
    [SerializeField] private float rotationSmoothSpeed = 12f;

    private NetworkVariable<Vector3> networkPosition = new NetworkVariable<Vector3>(
        writePerm: NetworkVariableWritePermission.Server);
    private NetworkVariable<Quaternion> networkRotation = new NetworkVariable<Quaternion>(
        writePerm: NetworkVariableWritePermission.Server);

    private Vector3 lastServerPosition;

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            networkPosition.Value = transform.position;
            networkRotation.Value = transform.rotation;
            lastServerPosition = transform.position;
        }

    }
    private void Update()
    {
        if (!IsSpawned) return;
        if (IsOwner)
        {
            HandleOwnerInput();
            if (!IsServer)
            {
                SmoothRemotePlayer();
            }
        }
        else
        {
            SmoothRemotePlayer();
        }
    }

    private void HandleOwnerInput()
    {
        float moveInput = Input.GetAxis("Vertical");
        float turnInput = Input.GetAxis("Horizontal");

        Vector3 moveDirection = transform.forward * moveInput;
        float rotationAmount = turnInput * rotSpeed * Time.deltaTime;
        SubmitMovementServerRpc(moveDirection, rotationAmount);
    }

    [ServerRpc]
    private void SubmitMovementServerRpc(Vector3 moveDirection, float rotationAmount, ServerRpcParams rpcParams = default)
    {
        Vector3 proposedPosition = transform.position + moveDirection * moveSpeed * Time.deltaTime;
        Quaternion proposedRotation = transform.rotation * Quaternion.Euler(0f, rotationAmount, 0f);

        float moveDistance = Vector3.Distance(transform.position, proposedPosition);

        if (moveDistance > maxMoveDistancePerTrick)
        {
            Debug.LogWarning($"Rejected move from client {rpcParams.Receive.SenderClientId}: too far in one tick.");
            return;
        }

        proposedPosition.x = Mathf.Clamp(proposedPosition.x, minX, maxX);
        proposedPosition.z = Mathf.Clamp(proposedPosition.z, minZ, maxZ);

        transform.position = proposedPosition;
        transform.rotation = proposedRotation;

        networkPosition.Value = transform.position;
        networkRotation.Value = transform.rotation;
        lastServerPosition = transform.position;
    }

    private void SmoothRemotePlayer()
    {
        transform.position = Vector3.Lerp(
            transform.position,
            networkPosition.Value,
            positionSmoothSpeed * Time.deltaTime
            );

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            networkRotation.Value,
            rotationSmoothSpeed * Time.deltaTime
            );
    }







}
