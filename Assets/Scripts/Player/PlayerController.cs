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
    public Vector2Int targetPosition; // ��ǥ ��ġ
    private List<Node> path = new List<Node>(); // ��� ��带 �����ϴ� ����Ʈ
    private int currentPathIndex = 0;

    private bool isMoving = false; // ���� �̵� ���¸� ��Ÿ���� ����
    private Vector3 currentDirection; // ���� �̵� ����

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

    // �κ� ������ ���ֵ� �ǿ�
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

            // ��ȣ �ۿ�
            if (bInterAction.Value == true)
                return;

            if (inputTime <= 0f && UIManager.Instance.bJoystick)
                // ���� Ű �Է� ó��
                HandleDirectionInput(UIManager.Instance.JoyStickDirection);

            // ��ΰ� ���� ���� ��θ� ���� �̵�
            if (path != null && path.Count > 0 && !bStop.Value)
            {
                MoveAlongPath();
            }
            else if (path != null && isMoving && !bStop.Value) // isMoving�� true�� ��쿡�� �̵�
            {
                MoveAlongPath();
            }

            // �������� �ൿ �Է� ó��
            HandleActionInput();
        }
        else
        {
            // �ٸ� �÷��̾��� ���
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
            // ���� �ൿ���̴� �̵� ��� ����� ���ϰ� ���⸸ üũ ����..
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

        // ���� path�� Null �̶��
        if (path == null)
        {
            // ���� �׸��忡���� ��ġ�� �������� ���⿡ ���� �ε��� ���
            Vector2Int tempPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                                                    Mathf.FloorToInt(transform.position.y));

            // Ÿ�� ��ġ ���
            Vector2Int tempTargetPos = tempPos + direction;

            // �׸��� ���� �������� ��� ����
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

        // ���� �׸��忡���� ��ġ�� �������� ���⿡ ���� �ε��� ���
        Vector2Int currentPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
                                                Mathf.FloorToInt(transform.position.y));

        // Ÿ�� ��ġ ���
        Vector2Int targetPos = currentPos + direction;

        // �׸��� ���� �������� ��� ����
        if (IsWithinGridBounds(targetPos))
        {
            GeneratePathTo(targetPos);
        }
    }

    // �׸��� ���� ���� �ִ��� Ȯ���ϴ� �޼���
    private bool IsWithinGridBounds(Vector2Int position)
    {
        return GridManager.Instance.GridCheck(position);
    }

    private void GeneratePathTo(Vector2Int targetPos)
    {
        // A* ��� Ž���� ����Ͽ� ��ǥ ��ġ���� ��� ����
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
        Vector3 nextPos = new Vector3(nextNode.position.x, nextNode.position.y, 0); // Node�� ��ġ�� Vector3�� ��ȯ
        Vector3 direction = (nextPos - transform.position).normalized;

        // �ʹ� ���� ȣ���� ����ȭ�� ������ ���� ;��;
        //WalkAndAnimSet(direction.x, direction.y);
        transform.Translate(direction * moveSpeed * Time.deltaTime);
        UIManager.Instance.minimapMarker.OnMiniMapUpdate(transform.position, MinimapType.Player, true, 0);

        if (Vector3.Distance(transform.position, nextPos) < 0.1f)
        {
            Vector2Int roundedPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
            transform.position = new Vector3(roundedPosition.x, roundedPosition.y, transform.position.z);

            currentPathIndex++;
        }

        // ��ΰ� ������ isMoving�� false�� ����
        if (currentPathIndex >= path.Count)
        {
            ClearPath(); // ��� Ŭ����
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

        currentPathIndex = 0; // �ε��� �ʱ�ȭ
        inputTime = 0f;
        isMoving = false; // �̵� ���� �ʱ�ȭ
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
            // ...��¿ �� ����..
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
        // ���� �÷��̾ �߻�ü ����
        if (!IsOwner) yield break;

        bool isObjectReceived = false;
        GameObject projectile = null;

        // �̺�Ʈ ������ �߰�
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            projectile = obj;
            isObjectReceived = true;
        };

        // ������Ʈ Ǯ���� �������� �����ͼ� �÷��̾��� �ڽ����� ����
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.Bullet,new TransformData(this.transform),this.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
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

    // Ȱ ����
    public IEnumerator FireArrow()
    {
        if (!IsOwner) yield break;

        bool isObjectReceived = false;
        GameObject arrow = null;

        // �̺�Ʈ ������ �߰�
        ObjectPool.Instance.OnObjectPooled += (obj) =>
        {
            arrow = obj;
            isObjectReceived = true;
        };

        // ������Ʈ Ǯ���� �������� �����ͼ� �÷��̾��� �ڽ����� ����
        ObjectPool.Instance.GetPooledObjectServerRpc(ObjectPool.PrefabInfoType.Arrow,new TransformData(this.transform), this.GetComponent<NetworkObject>().NetworkObjectId);


        yield return new WaitUntil(() => isObjectReceived);

        // �̺�Ʈ ������ ����
        ObjectPool.Instance.OnObjectPooled -= (obj) =>
        {
            arrow = obj;
            isObjectReceived = false;
        };

        if (arrow != null)
        {
            // ObjectPool.Instance.GetParantChangedServerRpc(arrow.transform, this.transform);
            // arrow.transform.localPosition = this.transform.localPosition;   // �÷��̾� ���� ��ġ ����
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
        // GridManager�� ���� ������ �̵� ������ ��ġ ��������
        Vector2Int randomPosition = GridManager.Instance.GetRandomWalkablePosition();

        // ���� ��ġ�� ���� ��ǥ�� ��ȯ
        Vector3 targetWorldPosition = new Vector3(randomPosition.x, randomPosition.y, 0);

        // �÷��̾ ���� ��ġ�� �̵�
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
