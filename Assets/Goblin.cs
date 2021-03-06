using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Goblin : MonoBehaviour
{
    /// IdleFsm
    // 시작하면 Idle 애니메이션
    // 플레이어가 근접하면 추격 


    // 추격 할 때 플레이어한테 공격 가능한 거리면 공격
    // 공격후 추격
    // 추격 공격

    Animator animator;
    IEnumerator Start()
    {

        animator = GetComponentInChildren<Animator>();
        player = Player.instance;


        currentFsm = IdleFSM;
        while (true)// 무한루프
        {
            yield return StartCoroutine(currentFsm());
            
        }

    }
    Func<IEnumerator> currentFsm;
    Player player;
    public float detectRange = 40;
    public float attackRange = 10;

    private void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, detectRange);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, attackRange);
    }
    private IEnumerator IdleFSM()
    {
        // 시작하면 Idle 애니메이션 재생
        animator.Play("Idle");

        /// IdleCo
        // 플레이어 근접하면 추격
        while (Vector3.Distance(transform.position, player.transform.position) > detectRange)
        {
            yield return null;
        }
        currentFsm = ChaseFSM;
    }
    public float speed = 34;
    private IEnumerator ChaseFSM()
    {
        animator.Play("Run");
        while (true)
        {
            Vector3 toPlayerDirection = player.transform.position - transform.position;
            toPlayerDirection.Normalize();
            yield return null;

        }
        }
    public float attackTime = 1;
    private IEnumerator AttackFSM()
    {
        animator.Play("Attack");
        yield return new WaitForSeconds(attackTime);  // 1초가 끝나면

        currentFsm = ChaseFSM;
    }

}