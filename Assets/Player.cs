using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    public float speed = 5;
    float normalSpeed;
    public float walkDistance = 12;
    public float stopDistance = 7;
    public Transform mousePointer;
    public Transform spriteTr;
    Plane plane = new Plane(new Vector3(0, 1, 0), 0);
    SpriteTrailRenderer.SpriteTrailRenderer spriteTrailRenderer;


    private void Start()
    {
        normalSpeed = speed;  // 스피드 값 초기화
        animator = GetComponentInChildren<Animator>();
        spriteTr = GetComponentInChildren<SpriteRenderer>().transform;
        spriteTrailRenderer = GetComponentInChildren<SpriteTrailRenderer.SpriteTrailRenderer>();
        spriteTrailRenderer.enabled = false;

    }

    void Update()
    {
        Move();

        Jump();

        Dash();
    }

    [Header("Dash")]
    public float dashCoolTime = 2;
    float nextDashableTime; // 다음 대시 가능한 시간
    public float dashableDistance = 10;
    public float dashableTime = 0.4f; // 0.4초 안에 마우스 떼는걸 인식

    float mouseDownTime;  
    Vector3 mouseDownPosition; 


    private void Dash()
    {
        // 마우스 누른 시점 위치랑 뗀 위치랑 찾아내자
        if (Input.GetKeyDown(KeyCode.Mouse0)) // 마우스 왼쪽클릭했을때 시간을 기억하자
        {
            mouseDownTime = Time.time;  //- 선언된 시점에서 카운트가 시작된다.
            mouseDownPosition = Input.mousePosition; // (1920,1080) 
        }

        if (nextDashableTime < Time.time)
        {
            if (Input.GetKeyUp(KeyCode.Mouse0))
            {
                bool isDashDrag = IsSucceesDashDrag();
                if (isDashDrag)
                {
                    nextDashableTime = Time.time + dashCoolTime;
                    StartCoroutine(DashCo());
                }
            }
        }
    }
    public float dashTime = 0.3f;
    public float dashSpeedMultiplySpeed = 4f;
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
    [Space(100)]
    public AnimationCurve jumpYac;
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
    public float jumpYMultiply = 1;
    public float jumpTimeMultiply = 1;
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

            if (distance > moveableDistance)
            {
                var dir = hitPoint - transform.position;
                dir.Normalize();

                if (State == StateType.Dash)
                    dir = dashDirection;
                transform.Translate(dir * speed * Time.deltaTime, Space.World);

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

                if (CangeableState())
                    State = StateType.Walk;
            }
            else
            {
                if (CangeableState())
                    State = StateType.Idle;
            }
            bool CangeableState()
            {
                if (jumpState == JumpStateType.Jump)
                    return false;

                if (state == StateType.Dash)
                    return false;

                return true;
            }
        }
    }


}