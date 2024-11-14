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

    public SpriteRenderer spriteRenderer; // SpriteRenderer ����

    private NetworkVariable<bool> isUsable = new NetworkVariable<bool>(true);

    [Range(1, 15)]
    public int InterIndex;

    private void Start()
    {
        if (NetworkManager.Singleton == null)
        {
            Debug.LogError("NetworkManager�� �ʱ�ȭ���� �ʾҽ��ϴ�.");
            return;
        }

        if (!NetworkManager.Singleton.IsListening)
        {
            Debug.LogError("NetworkManager�� ���� ������� �ʾҽ��ϴ�.");
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
            Debug.Log($"{gameObject.name}�� �������� �ʾ� �������� �����մϴ�.");
        }
        else
        {
            Debug.Log($"{gameObject.name}�� �̹� ������ �����Դϴ�.");
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
        // reuseTime ���� ���� �� ����
        float elapsedTime = 0f;
        Color color = spriteRenderer.color;

        while (elapsedTime < reuseTime)
        {
            float alpha = Mathf.Lerp(0f, 1f, elapsedTime / reuseTime);
            color.a = alpha;
            spriteRenderer.color = color;

            elapsedTime += Time.deltaTime;
            yield return null; // �� ������ ���
        }

        // reuseTime�� ���� �� ������ �������ϰ� ����
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
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.FullArmor, new TransformData(player), player.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = false;
        };
        if (pooledObject != null)
        {
            // ObjectPool.Instance.GetParantChangedServerRpc(pooledObject.transform, player.transform);
            //pooledObject.transform.SetParent(player.transform);     // �÷��̾��� �ڽ����� ����
            //pooledObject.transform.localPosition = Vector3.zero;   // �÷��̾� ���� ��ġ ����
            //pooledObject.SetActive(true);

            // Ȱ��ȭ�� ������Ʈ ��Ͽ� �߰�
            activeObjects.Add(pooledObject);
            if(stats != null) stats.OnChangeArmor();
            SoundManager.Instance.PlaySFX("FullArmor");
            yield return new WaitForSeconds(1f); // 1�� ���

            // ��� �Ұ��� ���·� ����
            SetUsableServerRpc(false);
            //UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, false, InterIndex);

            //// reuseTime ���� ���� �� ����
            //float elapsedTime = 0f;
            //Color color = spriteRenderer.color;

            //while (elapsedTime < reuseTime)
            //{
            //    float alpha = Mathf.Lerp(0f, 1f, elapsedTime / reuseTime);
            //    color.a = alpha;
            //    spriteRenderer.color = color;

            //    elapsedTime += Time.deltaTime;
            //    yield return null; // �� ������ ���
            //}

            //// reuseTime�� ���� �� ������ �������ϰ� ����
            //color.a = 1f;
            //spriteRenderer.color = color;

            //// ��� ���� ���·� ����
            //isUsable = true;
            //UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
        }
    }
    public void ReturnPool()
    {
        // Ȱ��ȭ�� ������Ʈ�� ���� ���, ���� ������ ������Ʈ�� ��ȯ
        if (activeObjects.Count > 0)
        {
            GameObject pooledObject = activeObjects[0];
            activeObjects.RemoveAt(0);

            ObjectPool.Instance.GetReturnPoolServerRpc(pooledObject.GetComponent<NetworkObject>().NetworkObjectId);         // Ǯ�� ��ȯ
        }
    }

    public void InterAction_Use()
    {
        if(interactable != null)
        {
            if (interactable.playerController.IsOwner)
                UIManager.Instance.OnInteraction(true, "This is not possible because the cooldown has not expired.", false);

            // 5�� �Ŀ� ������Ʈ Ǯ�� ��ȯ
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
                        // 5�� �Ŀ� ������Ʈ Ǯ�� ��ȯ
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
