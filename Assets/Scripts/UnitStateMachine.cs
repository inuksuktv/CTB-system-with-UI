using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class UnitStateMachine : BaseUnit, IPointerClickHandler
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

    private AttackHandler myAttack;
    public GameObject attackTarget;
    protected bool actionStarted;
    private float animationSpeed = 20f;
    protected Vector2 startPosition;

    private string oldInfoText;

    protected bool alive = true;

    void OnMouseEnter()
    {
        if (BSM.isChoosingTarget && gameObject.CompareTag("Unit")) {
            /*Text infoText = BSM.infoBox.transform.Find("Text").gameObject.GetComponent<Text>();
            oldInfoText = infoText.text;
            infoText.text = gameObject.name;*/
        }
    }

    void OnMouseExit()
    {
        if (BSM.isChoosingTarget && gameObject.CompareTag("Unit")) {
            /*Text infoText = BSM.infoBox.transform.Find("Text").gameObject.GetComponent<Text>();
            infoText.text = oldInfoText;*/
        }
    }
    void IPointerClickHandler.OnPointerClick(PointerEventData eventData)
    {
        if (BSM.isChoosingTarget && gameObject.CompareTag("Unit")) {
            BSM.TargetInput(gameObject);
        }
    }

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

                DieAndCleanup();

                break;
        }
    }

    protected virtual void ChooseAction()
    {
        myAttack = new AttackHandler();
        myAttack.target = BSM.heroesInBattle[Random.Range(0, BSM.heroesInBattle.Count)];
        myAttack.chosenAttack = attackList[Random.Range(0, attackList.Count)];
        turnState = TurnState.Acting;
    }

    protected virtual void DieAndCleanup()
    {
        if (!alive) { return; }
        else {
            tag = "DeadUnit";

            BSM.enemiesInBattle.Remove(gameObject);
            BSM.combatants.Remove(gameObject);

            GetComponent<SpriteRenderer>().color = Color.black;

            // Recalculate the turnQueue. Is it finally time to grapple with the GUI?
            BSM.turnQueue.Remove(gameObject);

            alive = false;

            BSM.ClearActivePanel();

            BSM.battleState = BattleStateMachine.BattleState.VictoryCheck;
        }
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted) {
            yield break;
        }

        actionStarted = true;

        Vector2 targetPosition = new Vector2(myAttack.target.transform.position.x, myAttack.target.transform.position.y);
        while (MoveToTarget(targetPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);

        initiative -= BSM.turnThreshold;

        DoDamage(myAttack);

        Vector2 firstPosition = startPosition;
        while (MoveBack(firstPosition)) { yield return null; }

        actionStarted = false;

        turnState = TurnState.Idle;

        BSM.battleState = BattleStateMachine.BattleState.AdvanceTime;
    }

    private void DoDamage (AttackHandler attackHandler)
    {
        float calcDamage = currentATK + attackHandler.chosenAttack.attackDamage;
        attackHandler.target.GetComponent<UnitStateMachine>().TakeDamage(calcDamage);
        Debug.Log(unitName + " deals " + calcDamage + " damage to " + attackHandler.target.GetComponent<UnitStateMachine>().unitName + " with " + attackHandler.chosenAttack.attackName);
    }

    private void TakeDamage(float damageAmount)
    {
        currentHP -= damageAmount;
        if (currentHP <= 0) {
            currentHP = 0;
            turnState = TurnState.Dead;
        }
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

    public void CollectAttack(AttackHandler attack)
    {
        myAttack = attack;
        turnState = TurnState.Acting;
    }
}
