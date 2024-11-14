using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;
using UnityEngine.UI;
using System.Collections.Generic;
using UnityEditor.PackageManager;
using static ObjectPool;

public class SceneTransitionManager : NetworkBehaviour
{
    public int aiCount;
    public NetworkObject ai;
    public NetworkObject[] networkObjects;
    public NetworkObject networkPrefab;
    public static SceneTransitionManager Instance { get; private set; }

    public Slider loadingBar;
    public Image LoadGround;
    public int expectedClientCount = 2;
    private float checkInterval = 1.0f;
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject); 
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void TransitionToEmptyScene()
    {
        if (IsServer)
        {
            StartCoroutine(LoadEmptyScene());
        }
    }

    private IEnumerator LoadEmptyScene()
    {
        ActivateLoadingUI(true);

        // 씬을 비동기적으로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("EmptyScene");
        asyncLoad.allowSceneActivation = false;

        // 로딩이 완료될 때까지 진행 상태를 업데이트
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = 1f;
                }
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
        yield return StartCoroutine(CheckClientConnections());
    }

    private IEnumerator CheckClientConnections()
    {
        // 모든 클라이언트가 연결될 때까지 대기
        while (NetworkManager.Singleton.ConnectedClients.Count < expectedClientCount)
        {
            Debug.Log("Waiting for all clients to connect...");
            yield return new WaitForSeconds(checkInterval);
        }
        TransitionToGameScene();
    }

    private void TransitionToGameScene()
    {
        // 서버가 클라이언트들에게 GameScene으로 이동 명령
        StartSceneTransitionServerRpc("GameScene");
    }

    [ServerRpc(RequireOwnership = false)]
    private void StartSceneTransitionServerRpc(string sceneName)
    {
        StartSceneTransitionClientRpc(sceneName);
    }

    [ClientRpc]
    private void StartSceneTransitionClientRpc(string sceneName)
    {
        StartCoroutine(LoadSceneWithProgress(sceneName)); // 각 클라이언트에서 로딩 게이지와 함께 씬 로드
    }

    private IEnumerator LoadSceneWithProgress(string sceneName)
    {
        ActivateLoadingUI(true);

        // 씬을 비동기적으로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // 로딩 완료 전까지 씬 전환을 막음

        // 로딩이 완료될 때까지 진행 상태를 업데이트
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (loadingBar != null)
            {
                loadingBar.value = progress; // 로딩 게이지 업데이트
            }

            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = 1f; // 게이지를 100%로 채움
                }
                asyncLoad.allowSceneActivation = true; // 씬 활성화
            }

            yield return null;
        }

        // 씬 로드 완료 후 필요한 초기화 수행
        if (IsServer)
        {
            SpawnPlayerNetworkObjects();
            SpawnNetworkObjects();
            SpawnAiObjects();
        }

        ActivateLoadingUI(false);
    }

    private void DespawnAllNetworkObjects()
    {
        // Dictionary의 값만을 복사하여 안전하게 순회
        var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        foreach (var obj in spawnedObjects)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Despawn(false); // false로 설정하여 완전히 제거하지 않고 Despawn만 수행
            }
        }
    }

    private void SpawnAllNetworkObjects()
    {
        // Dictionary의 값만을 복사하여 안전하게 순회
        var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        foreach (var obj in spawnedObjects)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Spawn(false); // false로 설정하여 완전히 제거하지 않고 Despawn만 수행
            }
        }
    }

    private void SpawnNetworkObjects()
    {
        foreach (var netObj in networkObjects)
        {
            if (netObj != null)
            {
                var instance = Instantiate(netObj);
                instance.Spawn();
                Debug.Log($"{instance.name}이(가) 스폰되었습니다.");
            }
        }
    }

    private void SpawnAiObjects()
    {
        for (int j = 0; j < aiCount; j++)
        {
            var obj = Instantiate(ai);
            obj.Spawn();
            Debug.Log($"{obj.name}이(가) 스폰되었습니다.");
        }
    }

    private void SpawnPlayerNetworkObjects()
    {
        // 게임 씬이 로드되었을 때, 서버에서 각 클라이언트의 플레이어 오브젝트 수동 스폰
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject == null)
            {
                var playerInstance = Instantiate(networkPrefab);
                playerInstance.SpawnAsPlayerObject(client.ClientId);

                SetOwnerClientRpc(client.ClientId, playerInstance.NetworkObjectId);
            }
        }
    }

    [ClientRpc]
    private void SetOwnerClientRpc(ulong clientId, ulong networkObjectId)
    {
        // 클라이언트 측에서 네트워크 오브젝트 소유자 설정
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject) && networkObject.IsOwner)
        {
            Debug.Log($"Client {clientId} owns NetworkObject {networkObjectId}");
            // 필요한 경우 소유자의 UI 업데이트 또는 설정 적용
        }
    }

    private void ActivateLoadingUI(bool isActive)
    {
        if (loadingBar != null) loadingBar.gameObject.SetActive(isActive);
        if (LoadGround != null) LoadGround.gameObject.SetActive(isActive);
    }

    #region 로비씬 이동
    [ServerRpc(RequireOwnership = false)]
    private void EndMatchRequestServerRpc()
    {
        // 서버에서 매칭 종료 처리
        EndMatchOnServer();
    }

    private void EndMatchOnServer()
    {
        // 모든 클라이언트에게 매칭 종료를 알림
        EndMatchOnClientsClientRpc();

        // 네트워크 연결 종료
        NetworkManager.Singleton.Shutdown();

        // 로비 씬으로 전환
        StartCoroutine(TransitionToLobbyAfterEndMatch());
    }


    public void RequestTransitionToLobbyScene()
    {
        if (IsServer)
        {
            // 서버에서 직접 로비 씬 전환을 시작
            EndMatchOnServer();
        }
        else
        {
            // 클라이언트에서 서버로 로비 씬 전환 요청
            EndMatchRequestServerRpc();
        }
    }

    [ClientRpc]
    private void EndMatchOnClientsClientRpc()
    {
        // 클라이언트 측에서 네트워크 연결 종료
        NetworkManager.Singleton.Shutdown();

        // 로비 씬으로 돌아가기
        StartCoroutine(TransitionToLobbyAfterEndMatch());
    }

    private IEnumerator TransitionToLobbyAfterEndMatch()
    {
        ActivateLoadingUI(true);
        NetworkManager.Singleton.Shutdown();

        // 로비 씬을 비동기적으로 로드
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("LobbyScene");
        asyncLoad.allowSceneActivation = false;

        // 로딩이 완료될 때까지 진행 상태를 업데이트
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (loadingBar != null)
            {
                loadingBar.value = progress;
            }

            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = 1f;
                }
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
        ActivateLoadingUI(false);
    }
    #endregion
}
