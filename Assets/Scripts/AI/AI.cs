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

    // 행동 상태
    public NetworkVariable<bool> bMove = new NetworkVariable<bool>(false);

    // 무기 상태
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
            Debug.Log($"{gameObject.name}이 스폰되지 않아 수동으로 스폰합니다.");
        }
        else
        {
            Debug.Log($"{gameObject.name}은 이미 스폰된 상태입니다.");
        }
    }


    void Start()
    {
        StartCoroutine(NetworkWait());
    }

    private IEnumerator NetworkWait()
    {
        // 네트워크가 준비될 때까지 기다립니다.
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

        // 초기 무기 상태 설정 (예: Sword)
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
                // 경로가 없을 때는 Idle 상태로 설정
                ActionAnimSetClientRpc(ActionType.Idle);
            }
        }
    }

    [ClientRpc]
    private void SetRandomTargetPositionClientRpc()
    {
        Vector2Int randomTarget = GridManager.Instance.GetRandomWalkablePosition(); // 임의의 경로 설정
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
            // 방향을 업데이트
            xdir.Value = direction.x;
            ydir.Value = direction.y;
        }

        // 상하 이동인 경우 xdir을 0으로 설정
        if (Mathf.Abs(xdir.Value) > Mathf.Abs(ydir.Value))
        {
            if(HostCheck)
                ydir.Value = 0; // 상하 이동 시 ydir은 0
        }
        // 좌우 이동인 경우 ydir을 0으로 설정
        else
        {
            if(HostCheck)
                xdir.Value = 0; // 좌우 이동 시 xdir은 0
        }

        // 이동 중에만 FlipX를 업데이트
        if (isMoving.Value)
        {
            if (direction.x < 0)
            {
                if(HostCheck)
                    bFlipX.Value = false; // 왼쪽을 보고 있으면 flip
            }
            else if (direction.x > 0)
            {
                if (HostCheck)
                    bFlipX.Value = true; // 오른쪽을 보고 있으면 flip하지 않음
            }
        }

        transform.Translate(direction * moveSpeed.Value * Time.deltaTime);
        UpdateAnimatorClientRpc(); // 애니메이션 업데이트 호출

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
        animator.SetFloat("xDir", xdir.Value); // Idle 시 xdir을 0으로 설정
        animator.SetFloat("yDir", ydir.Value); // Idle 시 ydir을 0으로 설정
        animator.SetBool("IsMove", isMoving.Value);
        animator.SetBool("IsBow", bBow.Value);
        animator.SetBool("IsHammer", bHammer.Value);
        animator.SetBool("IsSword", bSword.Value);
        animator.SetBool("IsScythe", bScythe.Value);
        animator.SetBool("IsGun", bGun.Value);

        // 캐릭터의 FlipX 설정
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

        ActionAnimSetClientRpc(ActionType.Idle); // Idle 상태로 전환
    }

    [ClientRpc]
    public void WeaponChangeClientRpc(WeaponType type)
    {
        if (HostCheck)
        {
            // 무기 상태 변경 메서드
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
        UpdateAnimatorClientRpc(); // 애니메이션 업데이트
    }

    [ClientRpc]
    private void ActionAnimSetClientRpc(ActionType type)
    {
        if (HostCheck)
        {
            // 행동 애니메이션 설정
            if (type == ActionType.Idle)
            {
                isMoving.Value = false;
                FlipPositionSetClientRpc(bFlipX.Value);
                //xdir = 0; // Idle 시 xdir을 0으로 설정
                //ydir = 0; // Idle 시 ydir을 0으로 설정
            }
            else
            {
                isMoving.Value = true;
            }
        }
        UpdateAnimatorClientRpc(); // 애니메이션 업데이트
    }

    public bool HostCheck => IsHost;
}
