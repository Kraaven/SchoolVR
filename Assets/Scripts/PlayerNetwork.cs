using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class PlayerNetwork : NetworkBehaviour
{
    private NetworkVariable<int> randomNumber = new NetworkVariable<int>(1, NetworkVariableReadPermission.Everyone, NetworkVariableWritePermission.Owner);
    [SerializeField] private Transform SpawnedObjectPrefab;
    private Transform spawnObjectTransform;
    public override void OnNetworkSpawn()
    {
        randomNumber.OnValueChanged += (int previousValue, int newValue) =>
        {
            Debug.Log(OwnerClientId + " : RandomNUmber : " + randomNumber.Value);
        };
    }
    void Update()
    {
        if (!IsOwner) return;

        if (Input.GetKeyDown(KeyCode.G))
        {
            if (IsClient)
            {
                SpawnObjectFromClientSideServerRpc();
            }
            else
            {
                spawnObjectTransform = Instantiate(SpawnedObjectPrefab);
                spawnObjectTransform.GetComponent<NetworkObject>().Spawn(true);
            }
        }
        
        if (Input.GetKeyDown(KeyCode.J))
        {
            if (IsClient)
            {
                DestroyObjectFromClientSideServerRpc();
            }
            else
            {
                Destroy(spawnObjectTransform.gameObject);
                spawnObjectTransform.GetComponent<NetworkObject>().Despawn(true);
            }
        }
        
        Vector3 moveDir = new Vector3(0f, 0f, 0f); 
        if (Input.GetKey(KeyCode.W)) moveDir.z = +1f;
        if (Input. GetKey(KeyCode.S)) moveDir.z = -1f;
        if (Input. GetKey(KeyCode.A)) moveDir.x = -1f;
        if (Input. GetKey(KeyCode.D)) moveDir.x = +1f;
        float moveSpeed = 3f;
        transform.position += moveDir * moveSpeed * Time.deltaTime; 
    }

    [ServerRpc]
    private void SpawnObjectFromClientSideServerRpc()
    {
        spawnObjectTransform = Instantiate(SpawnedObjectPrefab);
        spawnObjectTransform.GetComponent<NetworkObject>().Spawn(true);
    }
    [ServerRpc]
    private void DestroyObjectFromClientSideServerRpc()
    {
        Destroy(spawnObjectTransform.gameObject);
        spawnObjectTransform.GetComponent<NetworkObject>().Despawn(true);
    }
}
