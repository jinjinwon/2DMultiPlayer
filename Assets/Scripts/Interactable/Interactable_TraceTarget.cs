using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

public class Interactable_TraceTarget : InteractableObject
{
    public float reuseTime = 15f;
    public KeyCode interactionKey = KeyCode.E;
    public List<PlayerInteractable> list_Players = new List<PlayerInteractable>();
    private List<GameObject> activeObjects = new List<GameObject>();

    private PlayerStats stats;
    private PlayerInteractable interactable;

    public SpriteRenderer spriteRenderer; // SpriteRenderer ����

    private bool isUsable = true;

    [Range(1, 15)]
    public int InterIndex;

    private void Start()
    {
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
    }

    public override void Interact(Transform player)
    {
        if (isUsable) // ��� ���� ������ ���� ��ȣ�ۿ�
        {
            StartCoroutine(HandleInteraction(player));
        }
    }

    private IEnumerator HandleInteraction(Transform player)
    {
        bool isObjectReceived = false;
        GameObject pooledObject = null;
        GameObject poolBuffObject = null;

        // ������Ʈ Ǯ���� �������� �����ͼ� �÷��̾��� �ڽ����� ����

        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            poolBuffObject = obj;
            isObjectReceived = true;
        };

        //ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.TraceTargetBuff, new TransformData(UIManager.Instance.minimapMarker.TraceTargetPos));

        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            poolBuffObject = obj;
            isObjectReceived = false;
        };

        if (poolBuffObject != null)
        {
            //ObjectPool.Instance.GetParantChangedServerRpc(poolBuffObject.transform, UIManager.Instance.minimapMarker.TraceTargetPos);

            //poolBuffObject.transform.SetParent(UIManager.Instance.minimapMarker.TraceTargetPos);     // �÷��̾��� �ڽ����� ����
            //poolBuffObject.transform.localPosition = Vector3.zero;   // �÷��̾� ���� ��ġ ����
            poolBuffObject.transform.localScale = Vector3.one;
            //poolBuffObject.SetActive(true);

            if(poolBuffObject.TryGetComponent(out BuffIcon buffIcon))
            {
                buffIcon.BuffAction(15f, TraceTargetOff);
            }
        }


        // �̺�Ʈ ������ �߰�
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = true;
        };

        // ������Ʈ Ǯ���� �������� �����ͼ� �÷��̾��� �ڽ����� ����
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.TraceTarget, new TransformData(player), player.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            pooledObject = obj;
            isObjectReceived = false;
        };

        if (pooledObject != null)
        {
            //ObjectPool.Instance.GetParantChangedServerRpc(pooledObject.transform, player.transform);
            //pooledObject.transform.SetParent(player.transform);     // �÷��̾��� �ڽ����� ����
            //pooledObject.transform.localPosition = Vector3.zero;   // �÷��̾� ���� ��ġ ����
            //pooledObject.SetActive(true);

            // Ȱ��ȭ�� ������Ʈ ��Ͽ� �߰�
            activeObjects.Add(pooledObject);
            TraceTargetOn();
            InterAction_Use();
            SoundManager.Instance.PlaySFX("TraceTarget");
            yield return new WaitForSeconds(1f); // 1�� ���

            // ��� �Ұ��� ���·� ����
            isUsable = false;
            UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, false, InterIndex);

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

            // ��� ���� ���·� ����
            isUsable = true;
            UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Item, true, InterIndex);
        }
    }
    public override void ReturnPool()
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
        if (interactable != null)
        {
            if(interactable.playerController.IsOwner)
                UIManager.Instance.OnInteraction(true, "This is not possible because the cooldown has not expired.", false);

            // 5�� �Ŀ� ������Ʈ Ǯ�� ��ȯ
            Invoke(nameof(ReturnPool), 1f);
            interactable.IsInteractable = false;
            interactable.OnPlayerInteractionDel -= Interact;
            interactable = null;
        }

        if (stats != null)
        {
            stats = null;
        }
    }

    #region ��� ���� �ؾ��ؿ�~
    public void TraceTargetOn()
    {
        Debug.Log("��ġ ���� On");
    }

    public void TraceTargetOff()
    {
        Debug.Log("��ġ ���� Off");
    }
    #endregion

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("MyPlayer"))
        {
            if (isUsable == false)
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
                }
            }
        }
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("MyPlayer"))
        {
            if (isUsable == false)
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
                        stats = null;
                    }
                }
            }
        }
    }
}
