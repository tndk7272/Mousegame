using NaughtyAttributes;
using System.Collections;
using UnityEngine;

public class Player : MonoBehaviour
{
    public static Player instance;
    private void Awake()
    {
        instance = this;
    }
    public float speed = 5;
    float normalSpeed;
    public float walkDistance = 12;
    public float stopDistance = 7;
    public Transform mousePointer;
    public Transform spriteTr;



    Plane plane = new Plane(new Vector3(0, 1, 0), 0); // 무한한 플랜을 만든다
    SpriteTrailRenderer.SpriteTrailRenderer spriteTrailRenderer;


    private void Start()
    {
        normalSpeed = speed;  // 스피드 값 초기화
        animator = GetComponentInChildren<Animator>();
        spriteTr = GetComponentInChildren<SpriteRenderer>().transform;
        spriteTrailRenderer = GetComponentInChildren<SpriteTrailRenderer.SpriteTrailRenderer>();
        spriteTrailRenderer.enabled = false;
        //agent = GetComponent<NavMeshAgent>;

    }

    void Update()
    {
        if(State!= StateType.Attack)
        {
            Move();

            Jump();

        }

        bool isSucceedDash = Dash();

        Attack(isSucceedDash);
    }
    private void Attack(bool isSucceedDash)
    {
        if (isSucceedDash)
            return;
        // 마우스 왼쪽 버튼 뗏을때 공격
        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            StartCoroutine(AttackCo());
        }

    }
    public float attackTime = 1;
    private IEnumerator AttackCo()
    {
        State = StateType.Attack;
        yield return new WaitForSeconds(attackTime);
        State = StateType.Idle;
    }

    [Foldout("Dash")] public float dashCoolTime = 2;
    float nextDashableTime; // 다음 대시 가능한 시간
    [Foldout("Dash")] float dashableDistance = 10;
    [Foldout("Dash")] float dashableTime = 0.4f; // 0.4초 안에 마우스 떼는걸 인식
    float mouseDownTime;
    Vector3 mouseDownPosition;


    private bool Dash()
    {
        // 마우스 누른 시점 위치랑 뗀 위치랑 찾아내자
        if (Input.GetKeyDown(KeyCode.Mouse0)) // 마우스 왼쪽클릭했을때 시간을 기억하자
        {
            mouseDownTime = Time.time;  //- 선언된 시점에서 카운트가 시작된다.
            mouseDownPosition = Input.mousePosition; // (1920,1080) 
        }


        if (Input.GetKeyUp(KeyCode.Mouse0))
        {
            bool isDashDrag = IsSucceesDashDrag();
            if (isDashDrag)
            {

                StartCoroutine(DashCo());
                return true;
            }
        }
        return false;

    }
    [Foldout("Dash")] public float dashTime = 0.3f;
    [Foldout("Dash")] public float dashSpeedMultiplySpeed = 4f;
    Vector3 dashDirection;

    private IEnumerator DashCo()
    {
        // 드래그 방향으로 이동할건지 
        // 플레이어의 현재 이동 방향에서 x 방향으로 이동할건지
        // dashDirection x 방향만 사용./
        spriteTrailRenderer.enabled = true;
        dashDirection = Input.mousePosition - mouseDownPosition;

        dashDirection.y = 0;
        dashDirection.z = 0;
        dashDirection.Normalize();
        speed = normalSpeed * dashSpeedMultiplySpeed;
        State = StateType.Dash;
        yield return new WaitForSeconds(dashTime);
        speed = normalSpeed;
        State = StateType.Idle;
    }

    private bool IsSucceesDashDrag() // 드래그 함수
    {
        //ㅅ ㅣ간 체크  
        float dragTime = Time.time - mouseDownTime;  // 얼만큼의 시간이 걸렸는ㄴ지 
        if (dragTime > dashableTime)
            return false;

        // 거리체크
        float dragDistance = Vector3.Distance(mouseDownPosition, Input.mousePosition);
        if (dragDistance < dashableDistance)
            return false;

        return true;
    }

    [BoxGroup("Jump")] public AnimationCurve jumpYac;
    private void Jump()
    {
        if (jumpState == JumpStateType.Jump)
            return;
        if (Input.GetKeyDown(KeyCode.Mouse1))
        {
            StartCoroutine(JumpCo());
        }
    }
    public enum JumpStateType
    {
        Ground,
        Jump,
    }
    public enum StateType
    {
        Idle,
        Walk,
        JumpUp,
        JumpDown,
        Dash,
        Attack,

    }


    [SerializeField] StateType state = StateType.Idle;
    StateType State
    {
        get { return state; }
        set
        {
            if (state == value)
                return;

            if (EditorOption.Options[OptionType.Player상태변화로그]) // 유니티 상단 UI에 체크 해제 하는 UI 추가하는 코드
                Debug.Log($"state:{state}=> value:{value}"); // 점프했을때 애니메이션이 무슨상태인지 로그로 출력

            state = value;
            animator.Play(state.ToString());
        }
    }
    Animator animator;
    JumpStateType jumpState;
    [BoxGroup("Jump")] float jumpYMultiply = 1;
    [BoxGroup("Jump")] float jumpTimeMultiply = 1;
    private IEnumerator JumpCo()
    {
        jumpState = JumpStateType.Jump;
        State = StateType.JumpUp;
        float jumpStartTime = Time.time;
        float jumpDuration = jumpYac[jumpYac.length - 1].time;
        jumpDuration *= jumpTimeMultiply;
        float jumpEndTime = jumpStartTime + jumpDuration;
        float sumEvaluateTime = 0;
        float previousY = 0;
        //agent.enabled = false;

        while (Time.time < jumpEndTime)
        {
            float y = jumpYac.Evaluate(sumEvaluateTime / jumpTimeMultiply);
            y *= jumpYMultiply;
            transform.Translate(0, y, 0);
            yield return null;

            if (previousY > y)
            {
                // 점프 했으면 떨어지는 모션으로 바꾸자 !
                State = StateType.JumpDown;
            }
            previousY = y;


            sumEvaluateTime += Time.deltaTime;
        }
        // agent.enabled = true;
        jumpState = JumpStateType.Ground;
        State = StateType.Idle;
    }

    private void Move()
    {
        if (Time.timeScale == 0)
            return;


        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

        if (plane.Raycast(ray, out float enter))
        {

            Vector3 hitPoint = ray.GetPoint(enter);
            mousePointer.position = hitPoint;
            float distance = Vector3.Distance(hitPoint, transform.position);

            float moveableDistance = stopDistance;
            // State 가 Walk 일때는 7(stopDistance) 사용
            // Idle 에서 Walk 로 갈때는 12(walkDistance) 사용
            if (State == StateType.Idle)
                moveableDistance = walkDistance;

            Vector3 dir = hitPoint - transform.position;
            dir.Normalize();

            if (distance > moveableDistance)
            {

                if (State == StateType.Dash)
                    dir = dashDirection;

                transform.Translate(dir * speed * Time.deltaTime, Space.World);


                if (CangeableState())
                    State = StateType.Walk;
            }
            else
            {
                if (CangeableState())
                    State = StateType.Idle;
            }
            //방향(dir)에 따라서
            //오른쪽이라면 Y : 0, sprite X : 45
            //왼쪽이라면 Y : 180, sprite X : -45
            bool isRightSide = dir.x > 0;
            if (isRightSide)
            {
                transform.rotation = Quaternion.Euler(Vector3.zero);
                spriteTr.rotation = Quaternion.Euler(45, 0, 0);
            }
            else
            {
                transform.rotation = Quaternion.Euler(0, 180, 0);
                spriteTr.rotation = Quaternion.Euler(-45, 180, 0);
            }
            bool CangeableState()
            {
                if (jumpState == JumpStateType.Jump)
                    return false;

                if (state == StateType.Dash)
                    return false;
                if (state == StateType.Attack)
                    return false;

                return true;
            }
        }
    }


}