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

        // 클라이언트에서만 비활성화 상태로 유지
        //if (!IsServer)
        //{
        //    gameObject.SetActive(false); // 서버가 아닌 경우 오브젝트 비활성화
        //    //ObjectPool.Instance.AddDictionary(type, this.gameObject);
        //}
    }

    public override void OnDestroy()
    {
        string str = StackTraceUtility.ExtractStackTrace();
        Debug.Log(StackTraceUtility.ExtractStackTrace());
    }
}
