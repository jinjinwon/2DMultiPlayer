using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.Netcode;
using UnityEngine;

public class AI : NetworkBehaviour
{
    private delegate void FlipChanged(bool bFlipX);
    private delegate void FlipPositionChanged(bool bFlipX);

    [SerializeField] Animator animator;

    // �ൿ ����
    public NetworkVariable<bool> bMove = new NetworkVariable<bool>(false);

    // ���� ����
    public NetworkVariable<bool> bGun = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> bHammer = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> bBow = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> bSword = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> bScythe = new NetworkVariable<bool>(false);


    public NetworkVariable<float> moveSpeed = new NetworkVariable<float>(5f);
    private NetworkVariable<float> xdir = new NetworkVariable<float>();
    private NetworkVariable<float> ydir = new NetworkVariable<float>();

    private SpriteRenderer[] CharSpriteRender;

    [SerializeField] private Transform[] GunTransforms;
    [SerializeField] private Transform[] BowTransforms;
    [SerializeField] private Vector3[] gunOriginalPositions;
    [SerializeField] private Vector3[] bowOriginalPositions;

    private NetworkVariable<bool> bFlipX = new NetworkVariable<bool>(false);

    #region A*
    private List<Node> path = new List<Node>();
    private NetworkVariable<int> currentPathIndex = new NetworkVariable<int>();
    private NetworkVariable<bool> isMoving = new NetworkVariable<bool>(false);
    private NetworkVariable<Vector3> currentDirection = new NetworkVariable<Vector3>();
    private NetworkVariable<Vector2Int> targetPosition = new NetworkVariable<Vector2Int>();

    [SerializeField] private Pathfind pathfind;

    private NetworkVariable<float> moveTimer = new NetworkVariable<float>(2f);
    private NetworkVariable<float> moveInterval = new NetworkVariable<float>(2f);
    #endregion

    public NetworkVariable<WeaponType> type = new NetworkVariable<WeaponType>(WeaponType.Sword);

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


    void Start()
    {
        StartCoroutine(NetworkWait());
    }

    private IEnumerator NetworkWait()
    {
        // ��Ʈ��ũ�� �غ�� ������ ��ٸ��ϴ�.
        while (!NetworkManager.Singleton.IsListening)
        {
            yield return null;
        }

        animator.SetFloat("xDir", 0f);
        animator.SetFloat("yDir", -1f);

        CharSpriteRender = GetComponentsInChildren<SpriteRenderer>(true);

        gunOriginalPositions = new Vector3[GunTransforms.Length];
        bowOriginalPositions = new Vector3[BowTransforms.Length];

        for (int i = 0; i < GunTransforms.Length; i++) gunOriginalPositions[i] = GunTransforms[i].localPosition;
        for (int i = 0; i < BowTransforms.Length; i++) bowOriginalPositions[i] = BowTransforms[i].localPosition;

        // �ʱ� ���� ���� ���� (��: Sword)
        WeaponChangeClientRpc(type.Value);

        SetRandomTargetPositionClientRpc();
    }

    void Update()
    {
        if (HostCheck)
        {
            if (!isMoving.Value)
            {
                moveTimer.Value -= Time.deltaTime;
                if (moveTimer.Value <= 0f)
                {
                    SetRandomTargetPositionClientRpc();
                    moveTimer = moveInterval;
                }
            }

            if (path != null && path.Count > 0)
            {
                MoveAlongPathClientRpc();
            }
            else
            {
                // ��ΰ� ���� ���� Idle ���·� ����
                ActionAnimSetClientRpc(ActionType.Idle);
            }
        }
    }

    [ClientRpc]
    private void SetRandomTargetPositionClientRpc()
    {
        Vector2Int randomTarget = GridManager.Instance.GetRandomWalkablePosition(); // ������ ��� ����
        GeneratePathToClientRpc(randomTarget);
    }

    [ClientRpc]
    private void GeneratePathToClientRpc(Vector2Int targetPos)
    {
        path = pathfind.FindPath(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)), targetPos);

        if (path != null && path.Count > 0 && HostCheck)
        {
            currentPathIndex.Value = 0;
            isMoving.Value = true;
        }
    }

    [ClientRpc]
    private void MoveAlongPathClientRpc()
    {
        if (currentPathIndex.Value >= path.Count) return;

        Node nextNode = path[currentPathIndex.Value];
        Vector3 nextPos = new Vector3(nextNode.position.x, nextNode.position.y, 0);
        Vector3 direction = (nextPos - transform.position).normalized;

        if (HostCheck)
        {
            // ������ ������Ʈ
            xdir.Value = direction.x;
            ydir.Value = direction.y;
        }

        // ���� �̵��� ��� xdir�� 0���� ����
        if (Mathf.Abs(xdir.Value) > Mathf.Abs(ydir.Value))
        {
            if(HostCheck)
                ydir.Value = 0; // ���� �̵� �� ydir�� 0
        }
        // �¿� �̵��� ��� ydir�� 0���� ����
        else
        {
            if(HostCheck)
                xdir.Value = 0; // �¿� �̵� �� xdir�� 0
        }

        // �̵� �߿��� FlipX�� ������Ʈ
        if (isMoving.Value)
        {
            if (direction.x < 0)
            {
                if(HostCheck)
                    bFlipX.Value = false; // ������ ���� ������ flip
            }
            else if (direction.x > 0)
            {
                if (HostCheck)
                    bFlipX.Value = true; // �������� ���� ������ flip���� ����
            }
        }

        transform.Translate(direction * moveSpeed.Value * Time.deltaTime);
        UpdateAnimatorClientRpc(); // �ִϸ��̼� ������Ʈ ȣ��

        if (Vector3.Distance(transform.position, nextPos) < 0.1f)
        {
            if (HostCheck)
                currentPathIndex.Value++;
        }

        if (currentPathIndex.Value >= path.Count)
        {
            ClearPathClientRpc();
        }
    }

    [ClientRpc]
    private void UpdateAnimatorClientRpc()
    {
        animator.SetFloat("xDir", xdir.Value); // Idle �� xdir�� 0���� ����
        animator.SetFloat("yDir", ydir.Value); // Idle �� ydir�� 0���� ����
        animator.SetBool("IsMove", isMoving.Value);
        animator.SetBool("IsBow", bBow.Value);
        animator.SetBool("IsHammer", bHammer.Value);
        animator.SetBool("IsSword", bSword.Value);
        animator.SetBool("IsScythe", bScythe.Value);
        animator.SetBool("IsGun", bGun.Value);

        // ĳ������ FlipX ����
        foreach (var sprite in CharSpriteRender)
        {
            sprite.flipX = bFlipX.Value;
        }
    }

    [ClientRpc]
    private void FlipPositionSetClientRpc(bool bRight)
    {
        if (bGun.Value)
            for (int i = 0; i < GunTransforms.Length; i++)
                GunTransforms[i].localPosition = new Vector3(bRight ? -gunOriginalPositions[i].x : gunOriginalPositions[i].x, gunOriginalPositions[i].y, gunOriginalPositions[i].z);
        else if (bBow.Value)
            for (int i = 0; i < BowTransforms.Length; i++)
                BowTransforms[i].localPosition = new Vector3(bRight ? -bowOriginalPositions[i].x : bowOriginalPositions[i].x, bowOriginalPositions[i].y, bowOriginalPositions[i].z);
    }

    [ClientRpc]
    private void ClearPathClientRpc()
    {
        path.Clear();

        if (HostCheck)
        {
            currentPathIndex.Value = 0;
            isMoving.Value = false;
        }

        ActionAnimSetClientRpc(ActionType.Idle); // Idle ���·� ��ȯ
    }

    [ClientRpc]
    public void WeaponChangeClientRpc(WeaponType type)
    {
        if (HostCheck)
        {
            // ���� ���� ���� �޼���
            bGun.Value = bHammer.Value = bBow.Value = bSword.Value = bScythe.Value = false;
            switch (type)
            {
                //case WeaponType.Gun:
                //    bGun.Value = true;
                //    break;
                case WeaponType.Scythe:
                    bScythe.Value = true;
                    break;
                //case WeaponType.Bow:
                //    bBow.Value = true;
                //    break;
                case WeaponType.Hammer:
                    bHammer.Value = true;
                    break;
                case WeaponType.Sword:
                    bSword.Value = true;
                    break;
            }
        }
        UpdateAnimatorClientRpc(); // �ִϸ��̼� ������Ʈ
    }

    [ClientRpc]
    private void ActionAnimSetClientRpc(ActionType type)
    {
        if (HostCheck)
        {
            // �ൿ �ִϸ��̼� ����
            if (type == ActionType.Idle)
            {
                isMoving.Value = false;
                FlipPositionSetClientRpc(bFlipX.Value);
                //xdir = 0; // Idle �� xdir�� 0���� ����
                //ydir = 0; // Idle �� ydir�� 0���� ����
            }
            else
            {
                isMoving.Value = true;
            }
        }
        UpdateAnimatorClientRpc(); // �ִϸ��̼� ������Ʈ
    }

    public bool HostCheck => IsHost;
}
