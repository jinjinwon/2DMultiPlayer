using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;

public class Interactable_FullArmor : NetworkBehaviour
{
    public float reuseTime = 15f;
    public KeyCode interactionKey = KeyCode.E;
    public List<PlayerInteractable> list_Players = new List<PlayerInteractable>();
    [SerializeField] private List<GameObject> activeObjects = new List<GameObject>();

    private PlayerStats stats;
    private PlayerInteractable interactable;

    public SpriteRenderer spriteRenderer; // SpriteRenderer 참조

    private NetworkVariable<bool> isUsable = new NetworkVariable<bool>(true);

    [Range(1, 15)]
    public int InterIndex;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager가 초기화되지 않았습니다.");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("NetworkManager가 아직 연결되지 않았습니다.");
        }

        isUsable.OnValueChanged += OnIsUsableChanged;
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
    }

    public override void OnNetworkSpawn()
    {
        base.OnNetworkSpawn();

        if (!NetworkObject.IsSpawned)
        {
            NetworkObject.Spawn();
            Debug.Log($"{gameObject.name}이 스폰되지 않아 수동으로 스폰합니다.");
        }
        else
        {
            Debug.Log($"{gameObject.name}은 이미 스폰된 상태입니다.");
        }
    }

    public void Interact(Transform player)
    {
        if (isUsable.Value) 
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
            if (newValue)
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
        // reuseTime 동안 알파 값 증가
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < reuseTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / reuseTime);
            color.a = alpha;
            spriteRenderer.color = color;

            elapsedTime += Time.deltaTime;
            yield return null; // 매 프레임 대기
        }

        // reuseTime이 끝난 후 완전히 불투명하게 설정
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
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.FullArmor, new TransformData(player), player.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // 이벤트 리스너 제거
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = false;
        };
        if (pooledObject != null)
        {
            // ObjectPool.Instance.GetParantChangedServerRpc(pooledObject.transform, player.transform);
            //pooledObject.transform.SetParent(player.transform);     // 플레이어의 자식으로 설정
            //pooledObject.transform.localPosition = Vector3.zero;   // 플레이어 기준 위치 설정
            //pooledObject.SetActive(true);

            // 활성화된 오브젝트 목록에 추가
            activeObjects.Add(pooledObject);
            if(stats != null) stats.OnChangeArmor();
            SoundManager.Instance.PlaySFX("FullArmor");
            yield return new WaitForSeconds(1f); // 1초 대기

            // 사용 불가능 상태로 변경
            SetUsableServerRpc(false);
            //UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, false, InterIndex);

            //// reuseTime 동안 알파 값 증가
            //float elapsedTime = 0f;
            //Color color = spriteRenderer.color;

            //while (elapsedTime < reuseTime)
            //{
            //    float alpha = Mathf.Lerp(0f, 1f, elapsedTime / reuseTime);
            //    color.a = alpha;
            //    spriteRenderer.color = color;

            //    elapsedTime += Time.deltaTime;
            //    yield return null; // 매 프레임 대기
            //}

            //// reuseTime이 끝난 후 완전히 불투명하게 설정
            //color.a = 1f;
            //spriteRenderer.color = color;

            //// 사용 가능 상태로 변경
            //isUsable = true;
            //UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
        }
    }
    public void ReturnPool()
    {
        // 활성화된 오브젝트가 있을 경우, 가장 오래된 오브젝트를 반환
        if (activeObjects.Count > 0)
        {
            GameObject pooledObject = activeObjects[0];
            activeObjects.RemoveAt(0);

            ObjectPool.Instance.GetReturnPoolServerRpc(pooledObject.GetComponent<NetworkObject>().NetworkObjectId);         // 풀로 반환
        }
    }

    public void InterAction_Use()
    {
        if(interactable != null)
        {
            if (interactable.playerController.IsOwner)
                UIManager.Instance.OnInteraction(true, "This is not possible because the cooldown has not expired.", false);

            // 5초 후에 오브젝트 풀로 반환
            Invoke(nameof(ReturnPool), 1f);
            interactable.IsInteractable = false;
            interactable.OnPlayerInteractionDel -= Interact;
            interactable = null;
        }

        if(stats != null)
        {
            stats.OnChangeArmor -= InterAction_Use;
            stats = null;
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
                    interactable = player;
                    interactable.StrMessage = interactionKey.ToString();
                    interactable.IsInteractable = true;
                    interactable.OnPlayerInteractionDel += Interact;
                }

                if (other.TryGetComponent(out PlayerStats playerStats))
                {
                    stats = playerStats;
                    stats.OnChangeArmor += InterAction_Use;
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
                    if (interactable != null)
                    {
                        // 5초 후에 오브젝트 풀로 반환
                        Invoke(nameof(ReturnPool), 1f);
                        interactable.IsInteractable = false;
                        interactable.OnPlayerInteractionDel -= Interact;
                        interactable = null;
                    }
                }

                if (other.TryGetComponent(out PlayerStats playerStats))
                {
                    if (stats != null)
                    {
                        stats.OnChangeArmor -= InterAction_Use;
                        stats = null;
                    }
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
