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

        // ���������� ����
        if (NetworkManager.Singleton.IsServer)
        {
            SpawnObjectPool();
        }
    }

    private void SpawnObjectPool()
    {
        // �������� ObjectPool �������� ����
        if (objectPoolPrefab != null)
        {
            GameObject objectPool = Instantiate(objectPoolPrefab);

            ObjectPool poolScript = objectPool.GetComponent<ObjectPool>();
            poolScript.prefabInfos = prefabInfos;

            // ���������� ��Ʈ��ũ ��ü�� ��Ȱ��ȭ ���·� ����
            if (objectPool != null && !objectPool.GetComponent<NetworkObject>().IsSpawned)
            {
                objectPool.GetComponent<NetworkObject>().Spawn(true);
            }
        }
    }
}
