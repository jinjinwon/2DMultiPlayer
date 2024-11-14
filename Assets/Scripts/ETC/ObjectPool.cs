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
                // 아직 인스턴스가 없으면 씬에서 찾거나, 새로 생성
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

        // 서버에서만 오브젝트 풀을 생성
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
                        networkObject.Spawn(false); // 네트워크에서 비활성화 상태로 스폰
                    }
                    obj.SetActive(false);
                    objects.Add(obj);
                }

                pooledObjects[prefabInfo.prefabName] = objects;
            }
        }
    }


    #region 오브젝트 활성화 요청
    [ServerRpc(RequireOwnership = false)]
    public void GetPooledObjectActiveSetServerRpc(ulong networkObjectId, bool isActive)
    {
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // 상태가 이미 원하는 상태인지 확인
            if (networkObject.gameObject.activeInHierarchy != isActive)
            {
                networkObject.gameObject.SetActive(isActive);
                GetPooledObjectActiveSetClientRpc(networkObjectId, isActive);
            }
            else
            {
                Debug.Log($"이미 원하는 상태 ID: {networkObjectId}, 활성화 여부: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"해당 네트워크 오브젝트의 아이디를 서버에서 찾을 수 없음 {networkObjectId}");
        }
    }

    [ClientRpc]
    private void GetPooledObjectActiveSetClientRpc(ulong networkObjectId, bool isActive)
    {
        Debug.Log($"IsActive {isActive} Id {networkObjectId}");
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // 상태가 이미 원하는 상태인지 확인
            if (networkObject.gameObject.activeInHierarchy != isActive)
            {
                networkObject.gameObject.SetActive(isActive);
            }
            else
            {
                Debug.Log($"이미 원하는 상태 ID: {networkObjectId}, 활성화 여부: {isActive}");
            }
        }
        else
        {
            Debug.LogWarning($"해당 네트워크 오브젝트의 아이디를 클라이언트에서 찾을 수 없음 {networkObjectId}");
        }
    }
    #endregion

    #region 오브젝트 생성 요청
    [ServerRpc(RequireOwnership = false)]
    public void GetPooledObjectServerRpc(PrefabInfoType prefabName, TransformData transform, ulong parentId = 0)
    {
        // 서버에서 클라이언트가 RPC를 호출한 경우를 확인
        if (NetworkManager.Singleton.ConnectedClients.TryGetValue(NetworkManager.Singleton.LocalClientId, out var client))
        {
            Debug.Log($"Client {client.PlayerObject.name} GetPooledObjectServerRpc");
        }

        // 서버에서 오브젝트 활성화 작업
        var obj = GetAvailablePooledObject(prefabName);
        if (obj != null)
        {
            // 부모 설정 및 활성화 처리 (서버에서 처리)
            GetParantChangedServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, transform, parentId);
            GetPooledObjectActiveSetServerRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, true);

            //// 클라이언트에서 변경 사항 반영을 기다리기 위해 약간의 지연
            //await Task.Delay(100);

            // 이후에 클라이언트로 상태 전파
            GetPooledObjectClientRpc(obj.GetComponent<NetworkObject>().NetworkObjectId, transform, parentId);
        }
        else
        {
            Debug.LogError("풀에서 해당 오브젝트를 찾을 수 없음");
        }
    }

    [ClientRpc]
    // 오브젝트 풀에서 오브젝트를 가져오는 함수
    public void GetPooledObjectClientRpc(ulong networkObjectId, TransformData transform, ulong parentId = 0)
    {
        Debug.Log($"ClientRpc received for networkObjectId: {networkObjectId}");

        // 전달받은 네트워크 오브젝트 ID를 통해 오브젝트를 가져옴
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out var networkObject))
        {
            // 부모 설정 및 활성화 작업
            GetParantChangedServerRpc(networkObjectId, transform, parentId);
            GetPooledObjectActiveSetServerRpc(networkObjectId, true);
            OnObjectPooled?.Invoke(networkObject.gameObject);
        }
        else
        {
            Debug.LogWarning($"네트워크 아이디를 클라이언트에서 찾을 수 없음 ID {networkObjectId}");
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

    #region 오브젝트 부모 변경 요청
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
                // 현재 부모와 요청된 부모가 다른 경우에만 변경
                if (networkObject.transform.parent == null || networkObject.transform.parent.GetComponent<NetworkObject>().NetworkObjectId != parentId)
                {
                    Debug.Log($"ishost {IsHost}");
                    parant.ApplyToTransform(parentId, networkObject.transform, IsHost);
                }
                else
                {
                    Debug.Log($"이미 부모가 변경되었음 ID: {networkObjectId}");
                }
            }
        }
    }
    #endregion

    #region 오브젝트 반환 및 부모 변경 요청
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
            // 부모 변경 및 비활성화 요청 시 중복 작업 방지
            GetParantChangedServerRpc(networkObjectId, new TransformData(this.transform), this.gameObject.GetComponent<NetworkObject>().NetworkObjectId);
            if (networkObject.gameObject.activeInHierarchy)
            {
                GetPooledObjectActiveSetServerRpc(networkObjectId, false);
            }
            else
            {
                Debug.Log($"이미 비활성화 상태임 ID: {networkObjectId}");
            }
        }
        else
        {
            Debug.LogWarning($"해당 네트워크 오브젝트 찾을 수 없음 {networkObjectId}");
        }
    }
    #endregion

}
