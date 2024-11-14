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
//    [SerializeField] private Vector3[] gunOriginalPositions; // 총 오브젝트들의 초기 위치 저장
//    [SerializeField] private Vector3[] bowOriginalPositions; // 활 오브젝트들의 초기 위치 저장


//    private FlipChanged OnFlipChanged;
//    private FlipPositionChanged OnFlipPositionChanged;

//    private bool bFlipX = false;
//    private bool bFlipWait = false;

//    #region A*
//    public Vector2Int targetPosition; // 목표 위치
//    private List<Node> path = new List<Node>(); // 경로 노드를 저장하는 리스트
//    private int currentPathIndex = 0;

//    private bool isMoving = false; // 현재 이동 상태를 나타내는 변수
//    private Vector3 currentDirection; // 현재 이동 방향

//    [SerializeField] private Pathfind pathfind;

//    [SerializeField] private float reInputTime = 1f;
//    private float inputTime = 0f;

//    #endregion

//    void Start()
//    {
//        // 초기값을 설정하기 전에 animator가 null인지 확인
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
//            // 방향 키 입력 처리
//            HandleDirectionInput();

//        // 경로가 있을 때는 경로를 따라 이동
//        if (path != null && path.Count > 0 && !bStop)
//        {
//            MoveAlongPath();
//        }
//        else if (path != null && isMoving && !bStop) // isMoving이 true인 경우에만 이동
//        {
//            MoveAlongPath();
//        }

//        // 언제든지 행동 입력 처리
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
//            // 무슨 행동중이니 이동 경로 계산은 안하고 방향만 체크 ㅇㅇ..
//            bFlipWait = true;
//            return;
//        }

//        // 만약 path가 Null 이라면
//        if(path == null)
//        {
//            // 현재 그리드에서의 위치를 가져오고 방향에 따라 인덱스 계산
//            Vector2Int tempPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
//                                                    Mathf.FloorToInt(transform.position.y));

//            // 타겟 위치 계산
//            Vector2Int tempTargetPos = tempPos + direction;

//            // 그리드 범위 내에서만 경로 생성
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

//        // 현재 그리드에서의 위치를 가져오고 방향에 따라 인덱스 계산
//        Vector2Int currentPos = new Vector2Int(Mathf.FloorToInt(transform.position.x),
//                                                Mathf.FloorToInt(transform.position.y));

//        // 타겟 위치 계산
//        Vector2Int targetPos = currentPos + direction;

//        // 그리드 범위 내에서만 경로 생성
//        if (IsWithinGridBounds(targetPos))
//        {
//            GeneratePathTo(targetPos);
//        }
//    }

//    // 그리드 범위 내에 있는지 확인하는 메서드
//    private bool IsWithinGridBounds(Vector2Int position)
//    {
//        return GridManager.Instance.GridCheck(position);
//        //return position.x >= 0 && position.x < GridManager.Instance.gridWidth && position.y >= 0 && position.y < GridManager.Instance.gridHeight;
//    }

//    private void GeneratePathTo(Vector2Int targetPos)
//    {
//        // A* 경로 탐색을 사용하여 목표 위치로의 경로 생성
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
//        Vector3 nextPos = new Vector3(nextNode.position.x, nextNode.position.y, 0); // Node의 위치를 Vector3로 변환
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

//        // 경로가 끝나면 isMoving을 false로 설정
//        if (currentPathIndex >= path.Count)
//        {
//            ClearPath(); // 경로 클리어
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
//        currentPathIndex = 0; // 인덱스 초기화
//        isMoving = false; // 이동 상태 초기화
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
