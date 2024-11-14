using System.Collections.Generic;
using Unity.VisualScripting.Antlr3.Runtime.Misc;
using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using Unity.Netcode.Components;
using UnityEngine.Rendering;
using UnityEditor.PackageManager;
using System.Collections;

public class PlayerController : NetworkBehaviour
{
    private NetworkVariable<ulong> unique = new NetworkVariable<ulong>();

    [SerializeField] private Animator animator;

    [SerializeField] private BoxCollider2D boxCollider;
    private NetworkVariable<bool> bAttack = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bInteraction = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bHit = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bRun = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bDeath = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bMove = new NetworkVariable<bool>(false);

    private NetworkVariable<bool> bGun = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bHammer = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bBow = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bSword = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bScythe = new NetworkVariable<bool>(false);

    public float moveSpeed = 5f;

    private NetworkVariable<float> xdir = new NetworkVariable<float>(0f);
    private NetworkVariable<float> ydir = new NetworkVariable<float>(1f);

    [SerializeField] private NetworkVariable<bool> bStop = new NetworkVariable<bool>(false);

    private SpriteRenderer[] CharSpriteRender;

    [SerializeField] private Transform[] GunTransforms;
    [SerializeField] private Transform[] BowTransforms;
    [SerializeField] private Vector3[] gunOriginalPositions;
    [SerializeField] private Vector3[] bowOriginalPositions;


    private NetworkVariable<bool> bFlipX = new NetworkVariable<bool>(false);
    private NetworkVariable<bool> bFlipWait = new NetworkVariable<bool>(false);
    [SerializeField] private NetworkVariable<WeaponType> weaponType = new NetworkVariable<WeaponType>(WeaponType.Empty);

    #region A*
    public Vector2Int targetPosition; // 목표 위치
    private List<Node> path = new List<Node>(); // 경로 노드를 저장하는 리스트
    private int currentPathIndex = 0;

    private bool isMoving = false; // 현재 이동 상태를 나타내는 변수
    private Vector3 currentDirection; // 현재 이동 방향

    [SerializeField] private Pathfind pathfind;

    [SerializeField] private float reInputTime = 0.05f;
    private float inputTime = 0f;

    #endregion

    #region Network
    [SerializeField] private NetworkObject networkObject;
    #endregion

    public NetworkVariable<ulong> Unique { get { return unique; } set { unique = value; } }

    public PlayerStats playerStats;
    public NetworkVariable<bool> bInterAction = new NetworkVariable<bool>();

    // 로비가 있으면 없애도 되용
    private NetworkVariable<bool> bFirstWeaponType = new NetworkVariable<bool>(false);

    public bool OnClient() => !IsHost && IsClient;


    void Start()
    {
        //InterceptNetworkMessage.Instance.AddList(this);

        CharSpriteRender = GetComponentsInChildren<SpriteRenderer>(true);
        GameManage.Instance.RegisterPlayer(this);

        gunOriginalPositions = new Vector3[GunTransforms.Length];
        bowOriginalPositions = new Vector3[BowTransforms.Length];

        for (int i = 0; i < GunTransforms.Length; i++) gunOriginalPositions[i] = GunTransforms[i].localPosition;
        for (int i = 0; i < BowTransforms.Length; i++) bowOriginalPositions[i] = BowTransforms[i].localPosition;

        playerStats.OnTakeDamage += HitAnimSet;
        playerStats.OnDeathEvent += OnDeath;

        if(IsOwner)
            playerStats.OnDeathEvent += UIManager.Instance.OnDeathUI;

        #region Network Events

        bAttack.OnValueChanged += OnAttackChanged;
        bInteraction.OnValueChanged += OnInteractionChanged;
        bHit.OnValueChanged += OnHitChanged;
        bRun.OnValueChanged += OnRunChanged;
        bDeath.OnValueChanged += OnDeathChanged;
        bMove.OnValueChanged += OnMoveChanged;

        bGun.OnValueChanged += OnGunChanged;
        bHammer.OnValueChanged += OnHammerChanged;
        bBow.OnValueChanged += OnBowChanged;
        bSword.OnValueChanged += OnSwordChanged;
        bScythe.OnValueChanged += OnScytheChanged;

        xdir.OnValueChanged += OnDirectionXChanged;
        ydir.OnValueChanged += OnDirectionYChanged;

        bStop.OnValueChanged += OnStopChanged;

        bFlipX.OnValueChanged += OnFlipChanged;
        bFlipWait.OnValueChanged += OnFlipWaitChanged;
        weaponType.OnValueChanged += OnWeaponTypeChanged;

        bInterAction.OnValueChanged += OnInteractChanged;

        AttackXDir.OnValueChanged += OnAttackDirectionXChanged;
        AttackYDir.OnValueChanged += OnAttackDirectionYChanged;

        #endregion

        if (OnClient())
        {
            SetDirServerRPC(0f, -1f);
        }
        else if (IsHost)
        {
            xdir.Value = 0f;
            ydir.Value = -1f;
        }

        if (IsOwner)
        {
            this.gameObject.tag = "MyPlayer";

            UIManager.Instance.CameraFind(this.transform);
            UIManager.Instance.OnAttackAction += ActionAnimSet;
            UIManager.Instance.OnWepaonChange += WeaponSelect;
            UIManager.Instance.weapon.gameObject.SetActive(true);
            RandomTeleport(this.transform);
            UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Player, true, 0);
        }
        else
        {
            this.gameObject.tag = "Player";
        }
    }

    void Update()
    {
        if (GameManage.Instance.GameEnd)
            return;

        if (IsOwner)
        {
            if (playerStats.Death == true)
                return;

            if (inputTime > 0f)
                inputTime -= Time.deltaTime;

            // 상호 작용
            if (bInterAction.Value == true)
                return;

            if (inputTime <= 0f && UIManager.Instance.bJoystick)
                // 방향 키 입력 처리
                HandleDirectionInput(UIManager.Instance.JoyStickDirection);

            // 경로가 있을 때는 경로를 따라 이동
            if (path != null && path.Count > 0 && !bStop.Value)
            {
                MoveAlongPath();
            }
            else if (path != null && isMoving && !bStop.Value) // isMoving이 true인 경우에만 이동
            {
                MoveAlongPath();
            }

            // 언제든지 행동 입력 처리
            HandleActionInput();
        }
        else
        {
            // 다른 플레이어인 경우
            UpdateRemotePlayer();
        }
    }

    private void OnDeath()
    {
        boxCollider.enabled = false;
    }

    private void WeaponSelect(WeaponType type)
    {
        if (weaponType.Value == WeaponType.Empty)
        {
            if (OnClient())
            {
                SetWeaponTypeServerRpc(type);
                WeaponChange(type);
            }
            else if (IsHost)
            {
                weaponType.Value = type;
                WeaponChange(weaponType.Value);
            }
        }
        else
        {
            WeaponChange(weaponType.Value);
        }
    }

    private void UpdateRemotePlayer()
    {
        FlipPositionSet(bFlipX.Value);
        Flip(bFlipX.Value);
    }

    #region ServerRPC
    [ServerRpc(RequireOwnership = false)]
    private void SetAttackServerRpc(bool state) => bAttack.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetInteactionServerRPC(bool state) => bInteraction.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetHitServerRPC(bool state) => bHit.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetRunServerRPC(bool state) => bRun.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetDeathServerRPC(bool state) => bDeath.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetMoveServerRPC(bool state) => bMove.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetGunServerRPC(bool state) => bGun.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetHammerServerRPC(bool state) => bHammer.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetBowServerRPC(bool state) => bBow.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetSwordServerRPC(bool state) => bSword.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetScytheServerRPC(bool state) => bScythe.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetDirServerRPC(float x, float y)
    {
        xdir.Value = x;
        ydir.Value = y;
    }
    [ServerRpc(RequireOwnership = false)]
    private void SetStopServerRPC(bool state) => bStop.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetFlipServerRPC(bool state) => bFlipX.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetFlipWaitServerRPC(bool state) => bFlipWait.Value = state;
    [ServerRpc(RequireOwnership = false)]
    private void SetAttackDirServerRPC(float x, float y)
    {
        AttackXDir.Value = x;
        AttackYDir.Value = y;
    }
    [ServerRpc(RequireOwnership = false)]
    public void SetInterActionServerRpc(bool state) => bInterAction.Value = state;
    [ServerRpc(RequireOwnership = false)]
    public void SetWeaponTypeServerRpc(WeaponType state) => weaponType.Value = state;
    [ServerRpc(RequireOwnership = false)]
    public void SetWeaponFirstChangeServerRpc(bool state) => bFirstWeaponType.Value = state;
    #endregion

    #region Receive ServerRPC
    private void OnDirectionXChanged(float prevValue, float newValue)
    {
        Debug.Log($"OnDirectionXChanged Changed prev {prevValue} new {newValue}");
        animator.SetFloat("xDir", newValue);
    }

    private void OnDirectionYChanged(float prevValue, float newValue)
    {
        Debug.Log($"OnDirectionYChanged Changed prev {prevValue} new {newValue}");
        animator.SetFloat("yDir", newValue);
    }

    private void OnAttackChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsAttack", newValue);
    }

    private void OnInteractionChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsInteraction", newValue);
    }

    private void OnHitChanged(bool prevValue, bool newValue)
    {
        if (prevValue != newValue)
        {
            if (newValue) animator.SetTrigger("IsHit");
        }
    }

    private void OnRunChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsRun", newValue);
    }

    private void OnDeathChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsDead", newValue);
    }

    private void OnMoveChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsMove", newValue);
    }

    private void OnGunChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsGun", newValue);
    }

    private void OnHammerChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsHammer", newValue);
    }

    private void OnBowChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsBow", newValue);
    }

    private void OnSwordChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsSword", newValue);
    }

    private void OnScytheChanged(bool prevValue, bool newValue)
    {
        animator.SetBool("IsScythe", newValue);
    }

    private void OnStopChanged(bool prevValue, bool newValue)
    {
        Debug.Log($"OnStopChanged Changed prev {prevValue} new {newValue}");
    }

    private void OnFlipChanged(bool prevValue, bool newValue)
    {
        Flip(newValue);
        FlipPositionSet(newValue);
    }

    private void OnFlipWaitChanged(bool prevValue, bool newValue)
    {
        Debug.Log($"OnStopChanged Changed prev {prevValue} new {newValue}");
    }

    private void OnWeaponTypeChanged(WeaponType prevValue, WeaponType newValue)
    {
        WeaponChange(newValue);
    }

    private void OnInteractChanged(bool prevValue, bool newValue)
    {
        Debug.Log($"OnInteractChanged Changed prev {prevValue} new {newValue}");
    }

    private void OnAttackDirectionXChanged(float prevValue, float newValue)
    {
        Debug.Log($"OnAttackDirectionXChanged Changed prev {prevValue} new {newValue}");
    }

    private void OnAttackDirectionYChanged(float prevValue, float newValue)
    {
        Debug.Log($"OnAttackDirectionYChanged Changed prev {prevValue} new {newValue}");
    }



    #endregion

    private void HandleDirectionInput(JoyStickDirection Dirction = JoyStickDirection.Empty)
    {
        if (Dirction == JoyStickDirection.Left) { SetTargetPosition(Vector2Int.left, false); inputTime = reInputTime; isMoving = true; }
        else if (Dirction == JoyStickDirection.Right) { SetTargetPosition(Vector2Int.right, true); inputTime = reInputTime; isMoving = true; }
        else if (Dirction == JoyStickDirection.Up) { SetTargetPosition(Vector2Int.up, false); inputTime = reInputTime; isMoving = true; }
        else if (Dirction == JoyStickDirection.Down) { SetTargetPosition(Vector2Int.down, false); inputTime = reInputTime; isMoving = true; }
        else return;
    }

    private void SetTargetPosition(Vector2Int direction, bool flip)
    {
        if (xdir.Value != direction.x || ydir.Value != direction.y)
        {
            if (OnClient())
            {
                SetDirServerRPC(direction.x, direction.y);
            }
            else if (IsHost)
            {
                xdir.Value = direction.x; ydir.Value = direction.y;
            }
        }

        if (OnClient())
        {
            SetFlipServerRPC(flip);
        }
        else if (IsHost)
        {
            bFlipX.Value = flip;
        }

        if (bStop.Value == false)
        {
            Flip(bFlipX.Value);
            FlipPositionSet(bFlipX.Value);
        }
        else
        {
            // 무슨 행동중이니 이동 경로 계산은 안하고 방향만 체크 ㅇㅇ..
            if (OnClient())
            {
                SetFlipWaitServerRPC(true);
            }
            else if (IsHost)
            {
                bFlipWait.Value = true;
            }
            return;
        }

        // 만약 path가 Null 이라면
        if (path == null)
        {
            // 현재 그리드에서의 위치를 가져오고 방향에 따라 인덱스 계산
            Vector2Int tempPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                                                    Mathf.FloorToInt(transform.position.y));

            // 타겟 위치 계산
            Vector2Int tempTargetPos = tempPos + direction;

            // 그리드 범위 내에서만 경로 생성
            if (IsWithinGridBounds(tempTargetPos))
            {
                GeneratePathTo(tempTargetPos);
            }
            return;
        }

        if (inputTime > 0f)
            return;

        if (path.Count != 0)
            return;

        // 현재 그리드에서의 위치를 가져오고 방향에 따라 인덱스 계산
        Vector2Int currentPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                                                Mathf.FloorToInt(transform.position.y));

        // 타겟 위치 계산
        Vector2Int targetPos = currentPos + direction;

        // 그리드 범위 내에서만 경로 생성
        if (IsWithinGridBounds(targetPos))
        {
            GeneratePathTo(targetPos);
        }
    }

    // 그리드 범위 내에 있는지 확인하는 메서드
    private bool IsWithinGridBounds(Vector2Int position)
    {
        return GridManager.Instance.GridCheck(position);
    }

    private void GeneratePathTo(Vector2Int targetPos)
    {
        // A* 경로 탐색을 사용하여 목표 위치로의 경로 생성
        path = pathfind.FindPath(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)), targetPos);

        if (path != null && path.Count > 0)
        {
            currentPathIndex = 0;
        }
    }

    private void MoveAlongPath()
    {
        if (currentPathIndex >= path.Count) return;

        Node nextNode = path[currentPathIndex];
        Vector3 nextPos = new Vector3(nextNode.position.x, nextNode.position.y, 0); // Node의 위치를 Vector3로 변환
        Vector3 direction = (nextPos - transform.position).normalized;

        // 너무 많은 호출은 동기화에 오히려 방해 ;ㅅ;
        //WalkAndAnimSet(direction.x, direction.y);
        transform.Translate(direction * moveSpeed * Time.deltaTime);
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Player, true, 0);

        if (Vector3.Distance(transform.position, nextPos) < 0.1f)
        {
            Vector2Int roundedPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            transform.position = new Vector3(roundedPosition.x, roundedPosition.y, transform.position.z);

            currentPathIndex++;
        }

        // 경로가 끝나면 isMoving을 false로 설정
        if (currentPathIndex >= path.Count)
        {
            ClearPath(); // 경로 클리어
        }
    }

    private void HandleActionInput()
    {
        if (bStop.Value == false)
        {
            if (Input.GetKeyDown(KeyCode.A)) ActionAnimSet(ActionType.Attack);
            else if (Input.GetKeyDown(KeyCode.S)) ActionAnimSet(ActionType.Interaction);
            else if (Input.GetKeyDown(KeyCode.D)) ActionAnimSet(ActionType.Hit);
            else if (Input.GetKeyDown(KeyCode.G)) ActionAnimSet(ActionType.Dead);
            else if (UIManager.Instance.bJoystick)
            {
                ActionAnimSet(Input.GetKey(KeyCode.F) ? ActionType.Run : ActionType.Walk);
                //WalkAndAnimSet(xdir.Value, ydir.Value);
            }
            else ActionAnimSet(ActionType.Idle);
        }
    }

    private void HitAnimSet(int damage)
    {
        ActionAnimSet(ActionType.Hit);
        SoundManager.Instance.PlaySFX("Hit");
    }

    private void FlipPositionSet(bool bRight)
    {
        if (bGun.Value)
            for (int i = 0; i < GunTransforms.Length; i++)
                GunTransforms[i].localPosition = new Vector3(bRight ? -gunOriginalPositions[i].x : gunOriginalPositions[i].x, gunOriginalPositions[i].y, gunOriginalPositions[i].z);
        else if (bBow.Value)
            for (int i = 0; i < BowTransforms.Length; i++)
                BowTransforms[i].localPosition = new Vector3(bRight ? -bowOriginalPositions[i].x : bowOriginalPositions[i].x, bowOriginalPositions[i].y, bowOriginalPositions[i].z);
    }

    private void Flip(bool bRight)
    {
        foreach (SpriteRenderer spr in CharSpriteRender)
            spr.flipX = bRight;
    }

    public void ClearPath()
    {
        if (path != null)
            path.Clear();

        currentPathIndex = 0; // 인덱스 초기화
        inputTime = 0f;
        isMoving = false; // 이동 상태 초기화
    }

    private void DirectionRound()
    {
        float TempXdir = Mathf.RoundToInt(xdir.Value);
        float TempYdir = Mathf.RoundToInt(ydir.Value);

        if (OnClient())
        {
            SetDirServerRPC(TempXdir, TempYdir);
        }
        else if (IsHost)
        {
            xdir.Value = TempXdir; ydir.Value = TempYdir;
        }
    }

    public void OnEventStart()
    {
        if (OnClient())
        {
            SetStopServerRPC(true);
        }
        else if (IsHost)
        {
            bStop.Value = true;
        }

        ClearPath();
    }
    public void OnEventEnd()
    {
        if (OnClient())
        {
            SetStopServerRPC(false);
        }
        else if (IsHost)
        {
            bStop.Value = false;
        }

        if (bFlipWait.Value)
        {
            Flip(bFlipX.Value);
            FlipPositionSet(bFlipX.Value);
            if (OnClient())
            {
                SetFlipWaitServerRPC(false);
            }
            else if (IsHost)
            {
                bFlipWait.Value = false;
            }
        }

        ActionAnimSet(ActionType.Idle);
    }

    private void ActionAnimSet(ActionType type)
    {
        if (OnClient())
        {
            SetAttackServerRpc(false);
            SetInteactionServerRPC(false);
            SetHitServerRPC(false);
            SetRunServerRPC(false);
            SetDeathServerRPC(false);
            SetMoveServerRPC(false);
        }
        else if (IsHost)
        {
            bAttack.Value = false;
            bInteraction.Value = false;
            bHit.Value = false;
            bRun.Value = false;
            bDeath.Value = false;
            bMove.Value = false;
        }

        switch (type)
        {
            case ActionType.Idle:
                break;
            case ActionType.Walk:
                {
                    if (OnClient())
                    {
                        SetMoveServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bMove.Value = true;
                    }
                    break;
                }
            case ActionType.Run:
                {
                    if (OnClient())
                    {
                        SetRunServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bRun.Value = true;
                    }
                    break;
                }
            case ActionType.Dead:
                {
                    DirectionRound();
                    OnEventStart();
                    if (OnClient())
                    {
                        SetDeathServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bDeath.Value = true;
                    }
                    break;
                }
            case ActionType.Interaction:
                {
                    DirectionRound();
                    OnEventStart();
                    if (OnClient())
                    {
                        SetInteactionServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bInteraction.Value = true;
                    }
                    break;
                }
            case ActionType.Attack:
                {
                    DirectionRound();
                    OnEventStart();
                    if (OnClient())
                    {
                        SetAttackServerRpc(true);
                    }
                    else if (IsHost)
                    {
                        bAttack.Value = true;
                    }
                    break;
                }
            case ActionType.Hit:
                {
                    if (OnClient())
                    {
                        SetHitServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bHit.Value = true;
                    }
                    break;
                }
        }
        if (OnClient())
        {
            // ...어쩔 수 없는..
            if (bFirst == false)
            {
                animator.SetFloat("xDir", xdir.Value);
                animator.SetFloat("yDir", ydir.Value);
                bFirst = true;
            }
            SetAttackDirServerRPC(Mathf.RoundToInt(xdir.Value), Mathf.RoundToInt(ydir.Value));
        }
        else if (IsHost)
        {
            AttackXDir.Value = Mathf.RoundToInt(xdir.Value); AttackYDir.Value = Mathf.RoundToInt(ydir.Value);
        }
    }

    private void ActionAnimSet()
    {
        if (bMove.Value)
        {
            WalkAndAnimSet(xdir.Value, ydir.Value);
            return;
        }
        else if (bRun.Value)
        {

        }
        else if (bDeath.Value)
        {
            OnEventStart();
        }
        else if (bInteraction.Value)
        {
            OnEventStart();
        }
        else if (bAttack.Value)
        {
            OnEventStart();
        }
        else if (bHit.Value)
        {

        }
        else
            return;
    }

    private NetworkVariable<float> AttackXDir = new NetworkVariable<float>();
    private NetworkVariable<float> AttackYDir = new NetworkVariable<float>();
    private bool bFirst = false;

    public IEnumerator FireProjectile()
    {
        // 로컬 플레이어만 발사체 생성
        if (!IsOwner) yield break;

        bool isObjectReceived = false;
        GameObject projectile = null;

        // 이벤트 리스너 추가
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            projectile = obj;
            isObjectReceived = true;
        };

        // 오브젝트 풀에서 프리팹을 가져와서 플레이어의 자식으로 생성
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.Bullet,new TransformData(this.transform),this.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // 이벤트 리스너 제거
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            projectile = obj;
            isObjectReceived = false;
        };

        if (projectile != null)
        {
            Projectile arrowComponent = projectile.GetComponent<Projectile>();

            Vector3 direction = new Vector3(AttackXDir.Value, AttackYDir.Value, 0);
            arrowComponent.Initialize(direction,this);
            SoundManager.Instance.PlaySFX("Gun");
        }
    }

    // 활 공격
    public IEnumerator FireArrow()
    {
        if (!IsOwner) yield break;

        bool isObjectReceived = false;
        GameObject arrow = null;

        // 이벤트 리스너 추가
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            arrow = obj;
            isObjectReceived = true;
        };

        // 오브젝트 풀에서 프리팹을 가져와서 플레이어의 자식으로 생성
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.Arrow,new TransformData(this.transform), this.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // 이벤트 리스너 제거
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            arrow = obj;
            isObjectReceived = false;
        };

        if (arrow != null)
        {
            // ObjectPool.Instance.GetParantChangedServerRpc(arrow.transform, this.transform);
            // arrow.transform.localPosition = this.transform.localPosition;   // 플레이어 기준 위치 설정
            // arrow.SetActive(true);

            Projectile arrowComponent = arrow.GetComponent<Projectile>();

            Vector3 direction = new Vector3(AttackXDir.Value, AttackYDir.Value, 0);
            arrowComponent.Initialize(direction, this);
            SoundManager.Instance.PlaySFX("Bow");
        }
        else
        {
            Debug.Log($"Arrow Error");
        }
    }

    private void WalkAndAnimSet(float xDir, float yDir)
    {
        if (OnClient())
        {
            SetDirServerRPC(Mathf.RoundToInt(xDir), Mathf.RoundToInt(yDir));
        }
        else if (IsHost)
        {
            xdir.Value = Mathf.RoundToInt(xDir); ydir.Value = Mathf.RoundToInt(yDir);
        }
    }

    private void WeaponChange(WeaponType type)
    {
        if (OnClient())
        {
            SetGunServerRPC(false);
            SetHammerServerRPC(false);
            SetBowServerRPC(false);
            SetSwordServerRPC(false);
            SetScytheServerRPC(false);
        }
        else if(IsHost)
        {
            bGun.Value = false;
            bHammer.Value = false;
            bBow.Value = false;
            bSword.Value = false;
            bScythe.Value = false;
        }

        switch (type)
        {
            case WeaponType.Scythe:
                {
                    if (OnClient())
                    {
                        SetScytheServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bScythe.Value = true;
                    }
                    break;
                }
            case WeaponType.Hammer:
                {
                    if (OnClient())
                    {
                        SetHammerServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bHammer.Value = true;
                    }
                    break;
                }
            case WeaponType.Sword:
                {
                    if (OnClient())
                    {
                        SetSwordServerRPC(true);
                    }
                    else if (IsHost)
                    {
                        bSword.Value = true;
                    }
                    break;
                }
        }
    }

    private void RandomTeleport(Transform player)
    {
        // GridManager를 통해 랜덤한 이동 가능한 위치 가져오기
        Vector2Int randomPosition = GridManager.Instance.GetRandomWalkablePosition();

        // 랜덤 위치를 월드 좌표로 변환
        Vector3 targetWorldPosition = new Vector3(randomPosition.x, randomPosition.y, 0);

        // 플레이어를 랜덤 위치로 이동
        player.transform.position = targetWorldPosition;

        if (player.TryGetComponent(out PlayerController user))
        {
            user.ClearPath();
        }
    }

    public override void OnDestroy()
    {
        bAttack.OnValueChanged -= OnAttackChanged;
        bInteraction.OnValueChanged -= OnInteractionChanged;
        bHit.OnValueChanged -= OnHitChanged;
        bRun.OnValueChanged -= OnRunChanged;
        bDeath.OnValueChanged -= OnDeathChanged;
        bMove.OnValueChanged -= OnMoveChanged;

        bGun.OnValueChanged -= OnGunChanged;
        bHammer.OnValueChanged -= OnHammerChanged;
        bBow.OnValueChanged -= OnBowChanged;
        bSword.OnValueChanged -= OnSwordChanged;
        bScythe.OnValueChanged -= OnScytheChanged;

        xdir.OnValueChanged -= OnDirectionXChanged;
        ydir.OnValueChanged -= OnDirectionYChanged;

        bStop.OnValueChanged -= OnStopChanged;

        bFlipX.OnValueChanged -= OnFlipChanged;
        bFlipWait.OnValueChanged -= OnFlipWaitChanged;
        weaponType.OnValueChanged -= OnWeaponTypeChanged;

        bInterAction.OnValueChanged -= OnInteractChanged;

        AttackXDir.OnValueChanged -= OnAttackDirectionXChanged;
        AttackYDir.OnValueChanged -= OnAttackDirectionYChanged;
    }
}
