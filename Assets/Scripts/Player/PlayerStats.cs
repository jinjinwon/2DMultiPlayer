using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using Unity.Netcode;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerStats : NetworkBehaviour
{
    public delegate void TakeDamage(int damage);
    public delegate void ChangeArmor();
    public delegate void DeathEvent();

    public int MaxArmor = 100;

    private int realHp;
    [SerializeField] private NetworkVariable<int> armor = new NetworkVariable<int>();                                                  // HP
    private int damage;                                                 // DAMAGE
    private bool bHarfArmor = false;                                    // ���� ����
    private bool bDeath = false;                                        // ���� ����

    public List<GameObject> activeObjects = new List<GameObject>();    // ����Ʈ ���� ����
    private PlayerController playerController;

    #region Color �����
    [SerializeField] private SpriteRenderer spriteRenderer;
    [SerializeField] private Color color1 = new Color(1f, 1f, 1f, 1f);
    [SerializeField] private Color color2 = new Color(0.992f, 0.678f, 0.678f, 1f);
    [SerializeField] private float lerpDuration = 1f;                   // ���� ��ȯ �ð�
    #endregion
    public int Armor
    {
        get
        {
            return realHp;
        }
        set
        {
            realHp = value;

            if(realHp <= (MaxArmor / 2)) HarfArmor = true;
            else HarfArmor = false;

            if (realHp <= 0)
            {
                realHp = 0;
                Death = true;
            }

            if(IsOwner)
                UIManager.Instance.OnHPChanged(MaxArmor, realHp);
        }
    }
    public int Damage
    {
        get
        {
            return damage;
        }
        set
        {
            damage = value;
        }
    }
    public bool HarfArmor
    {
        get
        {
            return bHarfArmor;
        }
        set
        {
            if (bHarfArmor == value)
                return;

            bHarfArmor = value;
            StartCoroutine(HarpArmorEffect());
            HarpArmorColor();
        }
    }
    public bool Death
    {
        get
        {
            return bDeath;
        }
        set
        {
            if (bDeath == value)
                return;

            bDeath = value;
            OnDeathEvent();
        }
    }

    public TakeDamage OnTakeDamage;
    public ChangeArmor OnChangeArmor;
    public DeathEvent OnDeathEvent;

    private void Start()
    {
        Armor = MaxArmor;
        OnTakeDamage += OnHit;
        OnChangeArmor += OnArmor;
        OnDeathEvent += OnDeath;
        OnDeathEvent += GameManage.Instance.GameCheck;
        armor.OnValueChanged += SetHpChangedClientRpc;

        playerController = GetComponent<PlayerController>();
    }

    // Test Code
    [SerializeField] private int testDamage;

    [ContextMenu("������ �ޱ� �׽�Ʈ")]
    public void OnDamage()
    {
        OnHit(testDamage);
    }


    private void OnHit(int damage) // => OnTakeDamage
    {
        if (IsOwner)
        {
            if (playerController.OnClient())
            {
                int iTemp = Armor - damage;

                if (iTemp < 0)
                    iTemp = 0;

                SetHpChangedServerRpc(iTemp);
            }
            else
            {
                int iTemp = Armor - damage;

                if (iTemp < 0)
                    iTemp = 0;

                armor.Value = iTemp;
            }
        }
    }

    [ServerRpc(RequireOwnership = false)]
    private void SetHpChangedServerRpc(int state)
    {
        Debug.Log($"ServerRpc called with state: {state}");
        armor.Value = state;
        Debug.Log($"Server updated armor.Value to: {armor.Value}");
        //SetHpChangedClientRpc();
    }

    [ClientRpc]
    private void SetHpChangedClientRpc(int prevValue, int newValue)
    {
        Debug.Log($"ClientRpc called. Armor value received: {newValue}");
        Armor = newValue;
    }


    private void OnDeath() // => OnDeathEvent
    {
        Armor = 0;
        StartCoroutine(FadeOutAndDisable());
    }

    private void OnArmor() // => OnChangeArmor
    {
        Debug.Log("OnArmor called");
        if (playerController.OnClient() && NetworkManager.Singleton.IsClient)
        {
            Debug.Log("Calling SetHpChangedServerRpc");
            SetHpChangedServerRpc(MaxArmor);
        }
        else if (NetworkManager.Singleton.IsServer)
        {
            Debug.Log("Directly setting armor on the server");
            armor.Value = MaxArmor;
            Armor = armor.Value;
        }
    }

    private IEnumerator HarpArmorEffect()  // => HarfArmor 
    {
        // ���� �÷��̾ �߻�ü ����
        if (playerController != null && playerController.IsOwner) yield break;

        if (bHarfArmor == false)
        {
            Invoke("EffectReturn", 1f);
        }
        else
        {
            bool isObjectReceived = false;
            GameObject pooledObject = null;

            // �̺�Ʈ ������ �߰�
            ObjectPool.Instance.OnObjectPooled += (obj) =>
            {
                pooledObject = obj;
                isObjectReceived = true;
            };

            ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.BloodState, new TransformData(this.transform), this.GetComponent<NetworkObject>().NetworkObjectId);


            yield return new WaitUntil(() => isObjectReceived);

            // �̺�Ʈ ������ ����
            ObjectPool.Instance.OnObjectPooled -= (obj) =>
            {
                pooledObject = obj;
                isObjectReceived = false;
            };
        }
    }

    private void HarpArmorColor()
    {
        if (bHarfArmor == false)
        {
            StopCoroutine("ColorLerpCoroutine");
            Invoke("EffectReturn", 1f);
            spriteRenderer.color = color1;
        }
        else
        {
            StartCoroutine("ColorLerpCoroutine");
        }
    }

    private void EffectReturn()
    {
        // �ڱ� �ڽ��� ������ ��� �ڽ� ������Ʈ�� ��ȸ
        foreach (Transform child in transform)
        {
            // �ڽ� �� NetworkObject�� ���� ������Ʈ�� ������ Ǯ�� ��ȯ
            NetworkObject networkObj = child.GetComponent<NetworkObject>();
            if (networkObj != null && networkObj != this.GetComponent<NetworkObject>()) // �ڱ� �ڽ� ����
            {
                if (IsOwner)
                {
                    ObjectPool.Instance.GetReturnPoolServerRpc(networkObj.NetworkObjectId);
                }
            }
        }
    }

    private IEnumerator ColorLerpCoroutine()
    {
        float timeElapsed = 0;
        bool isLerpingToColor2 = true;

        while (true)
        {
            // �ð� ��� �������� ���� ��ȯ
            spriteRenderer.color = Color.Lerp(
                isLerpingToColor2 ? color1 : color2,
                isLerpingToColor2 ? color2 : color1,
                timeElapsed / lerpDuration
            );

            timeElapsed += Time.deltaTime;

            if (timeElapsed >= lerpDuration)
            {
                // ���� ��ȯ �Ϸ� �� ���� ���� �� �ð� �ʱ�ȭ
                isLerpingToColor2 = !isLerpingToColor2;
                timeElapsed = 0;
            }

            yield return null;
        }
    }

    private IEnumerator FadeOutAndDisable()
    {
        float fadeDuration = 1f; // ������� �ð�
        float timeElapsed = 0f;

        // ��������Ʈ�� ���� ���� 0���� ������ ����
        Color spriteColor = spriteRenderer.color;

        while (timeElapsed < fadeDuration)
        {
            timeElapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(spriteColor.a, 0, timeElapsed / fadeDuration);
            spriteRenderer.color = new Color(spriteColor.r, spriteColor.g, spriteColor.b, alpha);
            yield return null;
        }

        // ������ �������� �� ��Ȱ��ȭ
        gameObject.SetActive(false);
    }
}
