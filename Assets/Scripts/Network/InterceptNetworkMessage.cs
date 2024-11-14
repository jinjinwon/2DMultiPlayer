using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class InterceptNetworkMessage : MonoSingleton<InterceptNetworkMessage>
{
    public List<PlayerController> players = new List<PlayerController>();


    public void AddList(PlayerController player)
    {
        if(players.Contains(player) == false)
            players.Add(player);
    }

    [ServerRpc(RequireOwnership = false)]
    public void InterceptMessageServerRpc(ObjectPool.PrefabInfoType type, Transform transform)
    {
        Debug.Log("InterceptMessageServerRpc called");
        InterceptMessageClientRpc(type, transform);
    }

    [ClientRpc]
    public void InterceptMessageClientRpc(ObjectPool.PrefabInfoType type, Transform transform)
    {
        Debug.Log("InterceptMessageClientRpc called");

        for (int i = 0; i < players.Count; i++)
        {
            if (players[i].transform == transform)
            {
                ObjectPool.Instance.GetPooledObjectServerRpc(type, new TransformData(transform),transform.GetComponent<NetworkObject>().NetworkObjectId);
                break;
            }
        }
    }
}
