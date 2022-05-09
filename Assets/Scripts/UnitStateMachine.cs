using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStateMachine : BaseUnit
{
    protected BattleStateMachine BSM;

    public enum TurnState
    {
        Idle,
        Choosing,
        Acting,
        Dead
    }
    public TurnState turnState;

    public GameObject attackTarget;
    protected bool actionStarted;
    private float animationSpeed = 20f;
    protected Vector2 startPosition;

    void Start()
    {
        turnState = TurnState.Idle;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
        startPosition = transform.position;
    }

    void Update()
    {
        switch (turnState) {
            case TurnState.Idle:

                break;

            case TurnState.Choosing:

                ChooseAction();

                break;

            case TurnState.Acting:

                StartCoroutine(TimeForAction());

                break;

            case TurnState.Dead:



                break;
        }
    }

    protected virtual void ChooseAction()
    {
        attackTarget = BSM.heroesInBattle[Random.Range(0, BSM.heroesInBattle.Count)];
        turnState = TurnState.Acting;
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted) {
            yield break;
        }

        actionStarted = true;

        Vector2 targetPosition = new Vector2(attackTarget.transform.position.x, attackTarget.transform.position.y);
        while (MoveToTarget(targetPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);

        Vector2 firstPosition = startPosition;
        while (MoveBack(firstPosition)) { yield return null; }

        BSM.battleState = BattleStateMachine.BattleState.AdvanceTime;

        actionStarted = false;

        initiative -= BSM.turnThreshold;

        turnState = TurnState.Idle;
    }

    private bool MoveToTarget(Vector2 target)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, animationSpeed * Time.deltaTime);
        return (Vector2.Distance(transform.position, target) > 2f);
    }

    private bool MoveBack(Vector2 target)
    {
        transform.position = Vector2.MoveTowards(transform.position, target, animationSpeed * Time.deltaTime);
        return !(Mathf.Approximately(transform.position.x - target.x, 0) && Mathf.Approximately(transform.position.y - target.y, 0));
    }
}
