using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Unity.Netcode;
using UnityEngine;
using static ObjectPool;

public class ObjectPool : NetworkBehaviour
{
    private static ObjectPool _instance;
    public static ObjectPool Instance
    {
        get
        {
            if (_instance == null)
            {
                // ���� �ν��Ͻ��� ������ ������ ã�ų�, ���� ����
                _instance = FindObjectOfType<ObjectPool>();
                if (_instance == null)
                {
                    GameObject obj = new GameObject("ObjectPool");
                    _instance = obj.AddComponent<ObjectPool>();
                }
            }
            return _instance;
        }
    }

    [System.Serializable]
    public enum PrefabInfoType
    {
        JumpMachine,
        FullArmor,
        TraceTarget,
        BloodState,
        Arrow,
        Bullet,
        TraceTargetBuff,
    }

    [System.Serializable]
    public class PrefabInfo
    {
        public PrefabInfoType prefabName; 
        public GameObject prefab; 
    }

    public PrefabInfo[] prefabInfos;    
    public int poolSize = 20;

    private Dictionary<PrefabInfoType, List<GameObject>> pooledObjects;
    public event Action<GameObject> OnObjectPooled;

    private void Awake()
    {
        if (this.gameObject.TryGetComponent(out NetworkObject @object) == false)
            Destroy(this.gameObject);

        _instance = this;
        pooledObjects = new Dictionary<PrefabInfoType, List<GameObject>>();
        StartCoroutine(WaitForNetworkAndSpawn());


        Invoke("DelayDicAdd", 3f);
    }

    private void DelayDicAdd()
    {
        if (NetworkManager.Singleton.IsClient)
        {
            InterceptNetworkObject[] objects = GameObject.FindObjectsOfType<InterceptNetworkObject>();

            foreach (InterceptNetworkObject obj in objects)
            {
                AddDictionary(obj.type, obj.gameObject);
                obj.gameObject.SetActive(false);
            }
        }
    }

    public void AddDictionary(PrefabInfoType type, GameObject gameObject)
    {
        if (pooledObjects.ContainsKey(type) == true)
        {
            pooledObjects[type].Add(gameObject);
        }
        else
        {
            List<GameObject> objects = new List<GameObject>();
            objects.Add(gameObject);
            pooledObjects[type] = objects;
        }
    }

    IEnumerator WaitForNetworkAndSpawn()
    {
        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        // ���������� ������Ʈ Ǯ�� ����
        if (NetworkManager.Singleton.IsServer)
        {
            foreach (var prefabInfo in prefabInfos)
            {
                List<GameObject> objects = new List<GameObject>();

                for (int j = 0; j < poolSize; j++)
                {
                    GameObject obj = Instantiate(prefabInfo.prefab);
                    var networkObject = obj.GetComponent<NetworkObject>();

                    if (networkObject != null && !networkObject.IsSpawned)
                    {
                        networkObject.Spawn(false); // ��Ʈ��ũ���� ��Ȱ��ȭ ���·� ����
                    }
                    obj.SetActive(false);
                    objects.Add(obj);
                }

                pooledObjects[prefabInfo.prefabName] = objects;
            }
        }
    }


    #region ������Ʈ Ȱ��ȭ ��û
    [ServerRpc(RequireOwnership = false)]
    public void GetPooledObjectActiveSetServerRpc(ulong networkObjectId, bool isActive)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // ���°� �̹� ���ϴ� �������� Ȯ��
            if (networkObject.gameObject.activeInHierarchy != isActive)
            {
                networkObject.gameObject.SetActive(isActive);
                GetPooledObjectActiveSetClientRpc(networkObjectId, isActive);
            }
            else
            {
                Debug.Log($"�̹� ���ϴ� ���� ID: {networkObjectId}, Ȱ��ȭ ����: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"�ش� ��Ʈ��ũ ������Ʈ�� ���̵� �������� ã�� �� ���� {networkObjectId}");
        }
    }

    [ClientRpc]
    private void GetPooledObjectActiveSetClientRpc(ulong networkObjectId, bool isActive)
    {
        Debug.Log($"IsActive {isActive} Id {networkObjectId}");
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // ���°� �̹� ���ϴ� �������� Ȯ��
            if (networkObject.gameObject.activeInHierarchy != isActive)
            {
                networkObject.gameObject.SetActive(isActive);
            }
            else
            {
                Debug.Log($"�̹� ���ϴ� ���� ID: {networkObjectId}, Ȱ��ȭ ����: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"�ش� ��Ʈ��ũ ������Ʈ�� ���̵� Ŭ���̾�Ʈ���� ã�� �� ���� {networkObjectId}");
        }
    }
    #endregion

    #region ������Ʈ ���� ��û
    [ServerRpc(RequireOwnership = false)]
    public void GetPooledObjectServerRpc(PrefabInfoType prefabName, TransformData transform, ulong parentId = 0)
    {
        // �������� Ŭ���̾�Ʈ�� RPC�� ȣ���� ��츦 Ȯ��
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client))
        {
            Debug.Log($"Client {client.PlayerObject.name} GetPooledObjectServerRpc");
        }

        // �������� ������Ʈ Ȱ��ȭ �۾�
        var obj = GetAvailablePooledObject(prefabName);
        if (obj != null)
        {
            // �θ� ���� �� Ȱ��ȭ ó�� (�������� ó��)
            GetParantChangedServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, transform, parentId);
            GetPooledObjectActiveSetServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, true);

            //// Ŭ���̾�Ʈ���� ���� ���� �ݿ��� ��ٸ��� ���� �ణ�� ����
            //await Task.Delay(100);

            // ���Ŀ� Ŭ���̾�Ʈ�� ���� ����
            GetPooledObjectClientRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, transform, parentId);
        }
        else
        {
            Debug.LogError("Ǯ���� �ش� ������Ʈ�� ã�� �� ����");
        }
    }

    [ClientRpc]
    // ������Ʈ Ǯ���� ������Ʈ�� �������� �Լ�
    public void GetPooledObjectClientRpc(ulong networkObjectId, TransformData transform, ulong parentId = 0)
    {
        Debug.Log($"ClientRpc received for networkObjectId: {networkObjectId}");

        // ���޹��� ��Ʈ��ũ ������Ʈ ID�� ���� ������Ʈ�� ������
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // �θ� ���� �� Ȱ��ȭ �۾�
            GetParantChangedServerRpc(networkObjectId, transform, parentId);
            GetPooledObjectActiveSetServerRpc(networkObjectId, true);
            OnObjectPooled?.Invoke(networkObject.gameObject);
        }
        else
        {
            Debug.LogWarning($"��Ʈ��ũ ���̵� Ŭ���̾�Ʈ���� ã�� �� ���� ID {networkObjectId}");
        }
    }
    private GameObject GetAvailablePooledObject(PrefabInfoType prefabName)
    {
        if (!pooledObjects.ContainsKey(prefabName))
        {
            Debug.LogError("Invalid prefab name");
            return null;
        }

        if (pooledObjects.ContainsKey(prefabName))
        {
            foreach (GameObject obj in pooledObjects[prefabName])
            {
                if (!obj.activeInHierarchy)
                {
                    return obj;
                }
            }
        }
        return null;
    }
    #endregion

    #region ������Ʈ �θ� ���� ��û
    [ServerRpc(RequireOwnership = false)]
    public void GetParantChangedServerRpc(ulong networkObjectId, TransformData parant, ulong parentId = 0)
    {
        GetParantChangedClientRpc(networkObjectId, parant, parentId);
    }

    [ClientRpc]
    public void GetParantChangedClientRpc(ulong networkObjectId, TransformData parant, ulong parentId = 0)
    {
        if (IsHost)
        {
            if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
            {
                // ���� �θ�� ��û�� �θ� �ٸ� ��쿡�� ����
                if (networkObject.transform.parent == null || networkObject.transform.parent.GetComponent<NetworkObject>().NetworkObjectId != parentId)
                {
                    Debug.Log($"ishost {IsHost}");
                    parant.ApplyToTransform(parentId, networkObject.transform, IsHost);
                }
                else
                {
                    Debug.Log($"�̹� �θ� ����Ǿ��� ID: {networkObjectId}");
                }
            }
        }
    }
    #endregion

    #region ������Ʈ ��ȯ �� �θ� ���� ��û
    [ServerRpc(RequireOwnership = false)]
    public void GetReturnPoolServerRpc(ulong networkObjectId)
    {
        GetReturnPoolClientRpc(networkObjectId);
    }

    [ClientRpc]
    public void GetReturnPoolClientRpc(ulong networkObjectId)
    {
        Debug.Log($"GetReturnPoolClientRpc Id {networkObjectId}");

        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // �θ� ���� �� ��Ȱ��ȭ ��û �� �ߺ� �۾� ����
            GetParantChangedServerRpc(networkObjectId, new TransformData(this.transform), this.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            if (networkObject.gameObject.activeInHierarchy)
            {
                GetPooledObjectActiveSetServerRpc(networkObjectId, false);
            }
            else
            {
                Debug.Log($"�̹� ��Ȱ��ȭ ������ ID: {networkObjectId}");
            }
        }
        else
        {
            Debug.LogWarning($"�ش� ��Ʈ��ũ ������Ʈ ã�� �� ���� {networkObjectId}");
        }
    }
    #endregion

}
