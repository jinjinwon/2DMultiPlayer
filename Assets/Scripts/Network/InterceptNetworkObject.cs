using Unity.Netcode;
using Unity.Services.Matchmaker.Models;
using UnityEditor;
using UnityEngine;
using static ObjectPool;

public class InterceptNetworkObject : NetworkBehaviour
{
    public PrefabInfoType type;
    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        // Ŭ���̾�Ʈ������ ��Ȱ��ȭ ���·� ����
        //if (!IsServer)
        //{
        //    gameObject.SetActive(false); // ������ �ƴ� ��� ������Ʈ ��Ȱ��ȭ
        //    //ObjectPool.Instance.AddDictionary(type, this.gameObject);
        //}
    }

    public override void OnDestroy()
    {
        string str = StackTraceUtility.ExtractStackTrace();
        Debug.Log(StackTraceUtility.ExtractStackTrace());
    }
}
