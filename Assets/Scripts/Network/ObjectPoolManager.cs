using System.Collections;
using Unity.Netcode;
using UnityEngine;
using static ObjectPool;

public class ObjectPoolManager : MonoBehaviour
{
    public GameObject objectPoolPrefab;  
    public PrefabInfo[] prefabInfos;     

    private void Start()
    {
        StartCoroutine(WaitObjectPool());
    }

    private IEnumerator WaitObjectPool()
    {
        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // 서버에서만 실행
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnObjectPool();
        }
    }

    private void SpawnObjectPool()
    {
        // 서버에서 ObjectPool 프리팹을 스폰
        if (objectPoolPrefab != null)
        {
            GameObject objectPool = Instantiate(objectPoolPrefab);

            ObjectPool poolScript = objectPool.GetComponent<ObjectPool>();
            poolScript.prefabInfos = prefabInfos;

            // 서버에서만 네트워크 객체를 비활성화 상태로 스폰
            if (objectPool != null && !objectPool.GetComponent<NetworkObject>().IsSpawned)
            {
                objectPool.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }
}
