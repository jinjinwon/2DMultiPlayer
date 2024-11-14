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
    public SpriteRenderer spriteRenderer; // SpriteRenderer 참조
    [Range(1,15)]
    public int InterIndex;

    private NetworkVariable<bool> isUsable = new NetworkVariable<bool>(true);

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkObject.IsSpawned)
        {
            NetworkObject.Spawn();
            Debug.Log($"{gameObject.name}이 스폰되지 않아 수동으로 스폰합니다");
        }
        else
        {
            Debug.Log($"{gameObject.name}은 이미 스폰된 상태입니다");
        }
    }

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager가 초기화되지 않았습니다");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("NetworkManager가 아직 연결되지 않았습니다");
        }

        isUsable.OnValueChanged += OnIsUsableChanged;
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
    }

    public void Interact(Transform player)
    {
        if (isUsable.Value) // 사용 가능 상태일 때만 상호작용
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

        // 이벤트 리스너 추가
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = true;
        };

        // 오브젝트 풀에서 프리팹을 가져와서 플레이어의 자식으로 생성
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.JumpMachine, new TransformData(player),player.GetComponent<NetworkObject>().NetworkObjectId);

        yield return new WaitUntil(() => isObjectReceived);

        // 이벤트 리스너 제거
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = false;
        };

        if (pooledObject != null)
        {
            // 활성화된 오브젝트 목록에 추가
            activeObjects.Add(pooledObject);

            yield return new WaitForSeconds(1f);

            RandomTeleport(player); // 매개변수를 전달하여 호출

            // 사용 불가능 상태로 변경
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
        // 활성화된 오브젝트가 있을 경우, 가장 오래된 오브젝트를 반환
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
                    // 5초 후에 오브젝트 풀로 반환
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
