using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Interactable_JumpMachine : NetworkBehaviour
{
    public float reuseTime = 15f;
    public KeyCode interactionKey = KeyCode.E;
    public List<PlayerInteractable> list_Players = new List<PlayerInteractable> ();
    private List<GameObject> activeObjects = new List<GameObject>();
    public SpriteRenderer spriteRenderer; // SpriteRenderer ����
    [Range(1,15)]
    public int InterIndex;

    private NetworkVariable<bool> isUsable = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkObject.IsSpawned)
        {
            NetworkObject.Spawn();
            Debug.Log($"{gameObject.name}�� �������� �ʾ� �������� �����մϴ�");
        }
        else
        {
            Debug.Log($"{gameObject.name}�� �̹� ������ �����Դϴ�");
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager�� �ʱ�ȭ���� �ʾҽ��ϴ�");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("NetworkManager�� ���� ������� �ʾҽ��ϴ�");
        }

        isUsable.OnValueChanged += OnIsUsableChanged;
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
    }

    public void Interact(Transform player)
    {
        if (isUsable.Value) // ��� ���� ������ ���� ��ȣ�ۿ�
        {
            StartCoroutine(HandleInteraction(player));
        }
    }

    public void OnIsUsableChanged(bool prevValue, bool newValue)
    {
        if (prevValue == newValue)
            return;
        else
        {
            if(newValue)
            {
                UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
            }
            else
            {
                StartCoroutine(NotUsableAction());
                UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, false, InterIndex);
            }
        }
    }

    private IEnumerator NotUsableAction()
    {
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < reuseTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / reuseTime);
            color.a = alpha;
            spriteRenderer.color = color;

            elapsedTime += Time.deltaTime;
            yield return null;
        }
        color.a = 1f;
        spriteRenderer.color = color;

        SetUsableServerRpc(true);
    }

    private IEnumerator HandleInteraction(Transform player)
    {
        bool isObjectReceived = false;
        GameObject pooledObject = null;

        // �̺�Ʈ ������ �߰�
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = true;
        };

        // ������Ʈ Ǯ���� �������� �����ͼ� �÷��̾��� �ڽ����� ����
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.JumpMachine, new TransformData(player),player.GetComponent<NetworkObject>().NetworkObjectId);

        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = false;
        };

        if (pooledObject != null)
        {
            // Ȱ��ȭ�� ������Ʈ ��Ͽ� �߰�
            activeObjects.Add(pooledObject);

            yield return new WaitForSeconds(1f);

            RandomTeleport(player); // �Ű������� �����Ͽ� ȣ��

            // ��� �Ұ��� ���·� ����
            SetUsableServerRpc(false);
        }
    }

    private void RandomTeleport(Transform player)
    {
        Vector2Int randomPosition = GridManager.Instance.GetRandomWalkablePosition();

        Vector3 targetWorldPosition = new Vector3(randomPosition.x, randomPosition.y, 0);

        player.transform.position = targetWorldPosition;

        if(player.TryGetComponent(out PlayerController user))
        {
            user.ClearPath();
        }

        SoundManager.Instance.PlaySFX("Teleport");
        Invoke(nameof(ReturnPool), 1f);
    }

    public void ReturnPool()
    {
        // Ȱ��ȭ�� ������Ʈ�� ���� ���, ���� ������ ������Ʈ�� ��ȯ
        if (activeObjects.Count > 0)
        {
            GameObject pooledObject = activeObjects[0];
            activeObjects.RemoveAt(0);

            ObjectPool.Instance.GetReturnPoolServerRpc(pooledObject.GetComponent<NetworkObject>().NetworkObjectId);
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("MyPlayer"))
        {
            if (isUsable.Value == false)
            {
                UIManager.Instance.OnInteraction(true, "This is not possible because the cooldown has not expired.", false);
                return;
            }
            else
            {
                if (other.TryGetComponent(out PlayerInteractable player))
                {
                    player.StrMessage = interactionKey.ToString();
                    player.IsInteractable = true;
                    player.OnPlayerInteractionDel += Interact;
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("MyPlayer"))
        {
            if (isUsable.Value == false)
            {
                UIManager.Instance.OnInteraction(false, "", false);
                return;
            }
            else
            {
                UIManager.Instance.OnInteraction(false, "", false);

                if (other.TryGetComponent(out PlayerInteractable player))
                {
                    UIManager.Instance.OnInteraction(false, "", false);
                    // 5�� �Ŀ� ������Ʈ Ǯ�� ��ȯ
                    player.IsInteractable = false;
                    player.OnPlayerInteractionDel -= Interact;
                }
            }
        }
    }


    #region ServerRpc
    [ServerRpc(RequireOwnership = false)]
    public void SetUsableServerRpc(bool state)
    {
        isUsable.Value = state;
    }
    #endregion
}
