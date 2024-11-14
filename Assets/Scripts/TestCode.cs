//using System;
//using System.Collections.Generic;
//using UnityEditor.ShaderKeywordFilter;
//using UnityEngine;
//using UnityEngine.Analytics;
//using UnityEngine.UI;
//using static TestCode;

//public class TestCode : MonoBehaviour
//{
//    private delegate void FlipChanged(bool bFlipX);
//    private delegate void FlipPositionChanged(bool bFlipX);

//    [SerializeField] private Animator animator;

//    private bool bAttack = false;
//    private bool bInteraction = false;
//    private bool bHit = false;
//    private bool bRun = false;
//    private bool bDeath = false;
//    private bool bMove = false;

//    private bool bGun = false;
//    private bool bHammer = false;
//    private bool bBow = false;
//    private bool bSword = false;
//    private bool bScythe = false;


//    public float moveSpeed = 5f;

//    private float xdir;
//    private float ydir;

//    [SerializeField] private bool bStop = false;

//    private SpriteRenderer[] CharSpriteRender;

//    [SerializeField] private Transform[] GunTransforms;
//    [SerializeField] private Transform[] BowTransforms;
//    [SerializeField] private Vector3[] gunOriginalPositions; // �� ������Ʈ���� �ʱ� ��ġ ����
//    [SerializeField] private Vector3[] bowOriginalPositions; // Ȱ ������Ʈ���� �ʱ� ��ġ ����


//    private FlipChanged OnFlipChanged;
//    private FlipPositionChanged OnFlipPositionChanged;

//    private bool bFlipX = false;
//    private bool bFlipWait = false;

//    #region A*
//    public Vector2Int targetPosition; // ��ǥ ��ġ
//    private List<Node> path = new List<Node>(); // ��� ��带 �����ϴ� ����Ʈ
//    private int currentPathIndex = 0;

//    private bool isMoving = false; // ���� �̵� ���¸� ��Ÿ���� ����
//    private Vector3 currentDirection; // ���� �̵� ����

//    [SerializeField] private Pathfind pathfind;

//    [SerializeField] private float reInputTime = 1f;
//    private float inputTime = 0f;

//    #endregion

//    void Start()
//    {
//        // �ʱⰪ�� �����ϱ� ���� animator�� null���� Ȯ��
//        if (animator != null)
//        {
//            animator.SetFloat("xDir", 0f);
//            animator.SetFloat("yDir", -1f);

//            Debug.LogError("Animator is assigned!");
//        }
//        else
//        {
//            Debug.LogError("Animator is not assigned!");
//        }


//        CharSpriteRender = GetComponentsInChildren<SpriteRenderer>(true);

//        gunOriginalPositions = new Vector3[GunTransforms.Length];
//        bowOriginalPositions = new Vector3[BowTransforms.Length];

//        for (int i = 0; i < GunTransforms.Length; i++) gunOriginalPositions[i] = GunTransforms[i].localPosition;
//        for (int i = 0; i < BowTransforms.Length; i++) bowOriginalPositions[i] = BowTransforms[i].localPosition;


//        OnFlipChanged += Flip;
//        OnFlipPositionChanged += FlipPositionSet;
//    }


//    void Update()
//    {
//        if (inputTime > 0f)
//            inputTime -= Time.deltaTime;

//        if (inputTime <= 0f)
//            // ���� Ű �Է� ó��
//            HandleDirectionInput();

//        // ��ΰ� ���� ���� ��θ� ���� �̵�
//        if (path != null && path.Count > 0 && !bStop)
//        {
//            MoveAlongPath();
//        }
//        else if (path != null && isMoving && !bStop) // isMoving�� true�� ��쿡�� �̵�
//        {
//            MoveAlongPath();
//        }

//        // �������� �ൿ �Է� ó��
//        HandleActionInput();
//    }

//    private void HandleDirectionInput()
//    {
//        if (Input.GetKey(KeyCode.LeftArrow)) { SetTargetPosition(Vector2Int.left, false);       inputTime = reInputTime;        isMoving = true; }
//        else if (Input.GetKey(KeyCode.RightArrow)) { SetTargetPosition(Vector2Int.right, true); inputTime = reInputTime;        isMoving = true; }
//        else if (Input.GetKey(KeyCode.UpArrow)) { SetTargetPosition(Vector2Int.up, false);      inputTime = reInputTime;        isMoving = true; }
//        else if (Input.GetKey(KeyCode.DownArrow)) { SetTargetPosition(Vector2Int.down, false);  inputTime = reInputTime;        isMoving = true; }
//    }

//    private void SetTargetPosition(Vector2Int direction,bool flip)
//    {
//        xdir = direction.x;
//        ydir = direction.y;
//        bFlipX = flip;

//        if (bStop == false)
//        {
//            OnFlipChanged(bFlipX);
//            OnFlipPositionChanged(bFlipX);
//        }
//        else
//        {
//            // ���� �ൿ���̴� �̵� ��� ����� ���ϰ� ���⸸ üũ ����..
//            bFlipWait = true;
//            return;
//        }

//        // ���� path�� Null �̶��
//        if(path == null)
//        {
//            // ���� �׸��忡���� ��ġ�� �������� ���⿡ ���� �ε��� ���
//            Vector2Int tempPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
//                                                    Mathf.FloorToInt(transform.position.y));

//            // Ÿ�� ��ġ ���
//            Vector2Int tempTargetPos = tempPos + direction;

//            // �׸��� ���� �������� ��� ����
//            if (IsWithinGridBounds(tempTargetPos))
//            {
//                GeneratePathTo(tempTargetPos);
//            }
//            return;
//        }

//        if (inputTime > 0f)
//            return;

//        if (path.Count != 0)
//            return;

//        // ���� �׸��忡���� ��ġ�� �������� ���⿡ ���� �ε��� ���
//        Vector2Int currentPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
//                                                Mathf.FloorToInt(transform.position.y));

//        // Ÿ�� ��ġ ���
//        Vector2Int targetPos = currentPos + direction;

//        // �׸��� ���� �������� ��� ����
//        if (IsWithinGridBounds(targetPos))
//        {
//            GeneratePathTo(targetPos);
//        }
//    }

//    // �׸��� ���� ���� �ִ��� Ȯ���ϴ� �޼���
//    private bool IsWithinGridBounds(Vector2Int position)
//    {
//        return GridManager.Instance.GridCheck(position);
//        //return position.x >= 0 && position.x < GridManager.Instance.gridWidth && position.y >= 0 && position.y < GridManager.Instance.gridHeight;
//    }

//    private void GeneratePathTo(Vector2Int targetPos)
//    {
//        // A* ��� Ž���� ����Ͽ� ��ǥ ��ġ���� ��� ����
//        path = pathfind.FindPath(new Vector2Int(Mathf.FloorToInt(transform.position.x), Mathf.FloorToInt(transform.position.y)), targetPos);

//        if (path != null && path.Count > 0)
//        {
//            currentPathIndex = 0;
//        }
//    }

//    private void MoveAlongPath()
//    {
//        if (currentPathIndex >= path.Count) return;

//        Node nextNode = path[currentPathIndex];
//        Vector3 nextPos = new Vector3(nextNode.position.x, nextNode.position.y, 0); // Node�� ��ġ�� Vector3�� ��ȯ
//        Vector3 direction = (nextPos - transform.position).normalized;

//        WalkAndAnimSet(direction.x, direction.y);
//        transform.Translate(direction * moveSpeed * Time.deltaTime);
//        //transform.position = Vector3.MoveTowards(transform.position, nextPos, moveSpeed * Time.deltaTime);

//        if (Vector3.Distance(transform.position, nextPos) < 0.01f)
//        {
//            Vector2Int roundedPosition = new Vector2Int(Mathf.RoundToInt(transform.position.x), Mathf.RoundToInt(transform.position.y));
//            transform.position = new Vector3(roundedPosition.x, roundedPosition.y, transform.position.z);

//            currentPathIndex++;
//        }

//        // ��ΰ� ������ isMoving�� false�� ����
//        if (currentPathIndex >= path.Count)
//        {
//            ClearPath(); // ��� Ŭ����
//        }
//    }

//    private void SetDirection(float x, float y, bool flip)
//    {
//        xdir = x;
//        ydir = y;
//        bFlipX = flip;

//        if (bStop == false)
//        {
//            OnFlipChanged(bFlipX);
//            OnFlipPositionChanged(bFlipX);
//        }
//        else
//        {
//            bFlipWait = true;
//        }
//    }

//    private void HandleActionInput()
//    {
//        if (bStop == false)
//        {
//            if (Input.GetKeyDown(KeyCode.A)) ActionAnimSet(ActionType.Attack);
//            else if (Input.GetKeyDown(KeyCode.S)) ActionAnimSet(ActionType.Interaction);
//            else if (Input.GetKeyDown(KeyCode.D)) ActionAnimSet(ActionType.Hit);
//            else if (Input.GetKeyDown(KeyCode.G)) ActionAnimSet(ActionType.Dead);
//            else if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.UpArrow) || Input.GetKey(KeyCode.DownArrow))
//            {
//                ActionAnimSet(Input.GetKey(KeyCode.F) ? ActionType.Run : ActionType.Walk);
//                WalkAndAnimSet(xdir, ydir);
//            }
//            else ActionAnimSet(ActionType.Idle);

//            if (Input.GetKeyDown(KeyCode.Z)) WeaponChange(WeaponType.Gun);
//            if (Input.GetKeyDown(KeyCode.X)) WeaponChange(WeaponType.Hammer);
//            if (Input.GetKeyDown(KeyCode.C)) WeaponChange(WeaponType.Bow);
//            if (Input.GetKeyDown(KeyCode.V)) WeaponChange(WeaponType.Sword);
//            if (Input.GetKeyDown(KeyCode.B)) WeaponChange(WeaponType.Scythe);
//        }
//    }


//    private void FlipPositionSet(bool bRight)
//    {
//        if (bGun)
//            for (int i = 0; i < GunTransforms.Length; i++)
//                GunTransforms[i].localPosition = new Vector3(bRight ? -gunOriginalPositions[i].x : gunOriginalPositions[i].x, gunOriginalPositions[i].y, gunOriginalPositions[i].z);
//        else if (bBow)
//            for (int i = 0; i < BowTransforms.Length; i++)
//                BowTransforms[i].localPosition = new Vector3(bRight ? -bowOriginalPositions[i].x : bowOriginalPositions[i].x, bowOriginalPositions[i].y, bowOriginalPositions[i].z);
//    }

//    private void Flip(bool bRight)
//    {
//        foreach (SpriteRenderer spr in CharSpriteRender)
//            spr.flipX = bRight;
//    }

//    private void ClearPath()
//    {
//        path.Clear();
//        currentPathIndex = 0; // �ε��� �ʱ�ȭ
//        isMoving = false; // �̵� ���� �ʱ�ȭ
//    }

//    private void DirectionRound()
//    {
//        xdir = Mathf.RoundToInt(xdir);
//        ydir = Mathf.RoundToInt(ydir);

//        animator.SetFloat("xDir", xdir);
//        animator.SetFloat("yDir", ydir);
//    }

//    public void OnEventStart()
//    {
//        bStop = true;
//        ClearPath();
//    }
//    public void OnEventEnd()
//    {
//        bStop = false;

//        if(bFlipWait)
//        {
//            OnFlipChanged(bFlipX);
//            OnFlipPositionChanged(bFlipX);
//            bFlipWait = false;
//        }

//        ActionAnimSet(ActionType.Idle);
//    }

//    private void ActionAnimSet(ActionType type)
//    {
//        bAttack = bInteraction = bHit = bRun = bDeath = bMove = false;

//        switch (type)
//        {
//            case ActionType.Idle:
//                break;
//            case ActionType.Walk:
//                bMove = true;
//                break;
//            case ActionType.Run:
//                bRun = true;
//                break;
//            case ActionType.Dead:
//                DirectionRound();
//                OnEventStart();
//                bDeath = true;
//                break;
//            case ActionType.Interaction:
//                DirectionRound();
//                OnEventStart();
//                bInteraction = true;
//                break;
//            case ActionType.Attack:
//                DirectionRound();
//                OnEventStart();
//                bAttack = true;
//                break;
//            case ActionType.Hit:
//                bHit = true;
//                break;
//        }

//        UpdateAnimator();
//    }

//    private void WalkAndAnimSet(float xDir, float yDir)
//    {
//        animator.SetFloat("xDir", xDir);
//        animator.SetFloat("yDir", yDir);
//        //Vector2 moveDirection = new Vector2(xDir, yDir).normalized;
//        //transform.Translate(moveDirection * moveSpeed * Time.deltaTime, Space.World);
//    }

//    private void WeaponChange(WeaponType type)
//    {
//        Debug.Log($"Weapon changed to: {type}");
//        bGun = bHammer = bBow = bSword = bScythe = false;
//        switch (type)
//        {
//            case WeaponType.Gun:
//                {
//                    bGun = true;
//                    OnFlipPositionChanged(bFlipX);
//                    break;
//                }
//            case WeaponType.Scythe: bScythe = true; break;
//            case WeaponType.Bow:
//                {
//                    bBow = true;
//                    OnFlipPositionChanged(bFlipX);
//                    break;
//                }
//            case WeaponType.Hammer: bHammer = true; break;
//            case WeaponType.Sword: bSword = true; break;
//        }
//        UpdateAnimator();
//    }

//    private void UpdateAnimator()
//    {
//        animator.SetBool("IsMove", bMove);
//        animator.SetBool("IsDead", bDeath);
//        animator.SetBool("IsRun", bRun);
//        animator.SetBool("IsAttack", bAttack);
//        animator.SetBool("IsInteraction", bInteraction);
//        if (bHit) animator.SetTrigger("IsHit");

//        animator.SetBool("IsBow", bBow);
//        animator.SetBool("IsHammer", bHammer);
//        animator.SetBool("IsSword", bSword);
//        animator.SetBool("IsScythe", bScythe);
//        animator.SetBool("IsGun", bGun);
//    }
//}
