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

        // ���� �񵿱������� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("EmptyScene");
        asyncLoad.allowSceneActivation = false;

        // �ε��� �Ϸ�� ������ ���� ���¸� ������Ʈ
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
        // ��� Ŭ���̾�Ʈ�� ����� ������ ���
        while (NetworkManager.Singleton.ConnectedClients.Count < expectedClientCount)
        {
            Debug.Log("Waiting for all clients to connect...");
            yield return new WaitForSeconds(checkInterval);
        }
        TransitionToGameScene();
    }

    private void TransitionToGameScene()
    {
        // ������ Ŭ���̾�Ʈ�鿡�� GameScene���� �̵� ���
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
        StartCoroutine(LoadSceneWithProgress(sceneName)); // �� Ŭ���̾�Ʈ���� �ε� �������� �Բ� �� �ε�
    }

    private IEnumerator LoadSceneWithProgress(string sceneName)
    {
        ActivateLoadingUI(true);

        // ���� �񵿱������� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        asyncLoad.allowSceneActivation = false; // �ε� �Ϸ� ������ �� ��ȯ�� ����

        // �ε��� �Ϸ�� ������ ���� ���¸� ������Ʈ
        while (!asyncLoad.isDone)
        {
            float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            if (loadingBar != null)
            {
                loadingBar.value = progress; // �ε� ������ ������Ʈ
            }

            if (asyncLoad.progress >= 0.9f)
            {
                if (loadingBar != null)
                {
                    loadingBar.value = 1f; // �������� 100%�� ä��
                }
                asyncLoad.allowSceneActivation = true; // �� Ȱ��ȭ
            }

            yield return null;
        }

        // �� �ε� �Ϸ� �� �ʿ��� �ʱ�ȭ ����
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
        // Dictionary�� ������ �����Ͽ� �����ϰ� ��ȸ
        var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        foreach (var obj in spawnedObjects)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Despawn(false); // false�� �����Ͽ� ������ �������� �ʰ� Despawn�� ����
            }
        }
    }

    private void SpawnAllNetworkObjects()
    {
        // Dictionary�� ������ �����Ͽ� �����ϰ� ��ȸ
        var spawnedObjects = new List<NetworkObject>(NetworkManager.Singleton.SpawnManager.SpawnedObjects.Values);
        foreach (var obj in spawnedObjects)
        {
            if (obj != null && obj.IsSpawned)
            {
                obj.Spawn(false); // false�� �����Ͽ� ������ �������� �ʰ� Despawn�� ����
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
                Debug.Log($"{instance.name}��(��) �����Ǿ����ϴ�.");
            }
        }
    }

    private void SpawnAiObjects()
    {
        for (int j = 0; j < aiCount; j++)
        {
            var obj = Instantiate(ai);
            obj.Spawn();
            Debug.Log($"{obj.name}��(��) �����Ǿ����ϴ�.");
        }
    }

    private void SpawnPlayerNetworkObjects()
    {
        // ���� ���� �ε�Ǿ��� ��, �������� �� Ŭ���̾�Ʈ�� �÷��̾� ������Ʈ ���� ����
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
        // Ŭ���̾�Ʈ ������ ��Ʈ��ũ ������Ʈ ������ ����
        if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject networkObject) && networkObject.IsOwner)
        {
            Debug.Log($"Client {clientId} owns NetworkObject {networkObjectId}");
            // �ʿ��� ��� �������� UI ������Ʈ �Ǵ� ���� ����
        }
    }

    private void ActivateLoadingUI(bool isActive)
    {
        if (loadingBar != null) loadingBar.gameObject.SetActive(isActive);
        if (LoadGround != null) LoadGround.gameObject.SetActive(isActive);
    }

    #region �κ�� �̵�
    [ServerRpc(RequireOwnership = false)]
    private void EndMatchRequestServerRpc()
    {
        // �������� ��Ī ���� ó��
        EndMatchOnServer();
    }

    private void EndMatchOnServer()
    {
        // ��� Ŭ���̾�Ʈ���� ��Ī ���Ḧ �˸�
        EndMatchOnClientsClientRpc();

        // ��Ʈ��ũ ���� ����
        NetworkManager.Singleton.Shutdown();

        // �κ� ������ ��ȯ
        StartCoroutine(TransitionToLobbyAfterEndMatch());
    }


    public void RequestTransitionToLobbyScene()
    {
        if (IsServer)
        {
            // �������� ���� �κ� �� ��ȯ�� ����
            EndMatchOnServer();
        }
        else
        {
            // Ŭ���̾�Ʈ���� ������ �κ� �� ��ȯ ��û
            EndMatchRequestServerRpc();
        }
    }

    [ClientRpc]
    private void EndMatchOnClientsClientRpc()
    {
        // Ŭ���̾�Ʈ ������ ��Ʈ��ũ ���� ����
        NetworkManager.Singleton.Shutdown();

        // �κ� ������ ���ư���
        StartCoroutine(TransitionToLobbyAfterEndMatch());
    }

    private IEnumerator TransitionToLobbyAfterEndMatch()
    {
        ActivateLoadingUI(true);
        NetworkManager.Singleton.Shutdown();

        // �κ� ���� �񵿱������� �ε�
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync("LobbyScene");
        asyncLoad.allowSceneActivation = false;

        // �ε��� �Ϸ�� ������ ���� ���¸� ������Ʈ
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
