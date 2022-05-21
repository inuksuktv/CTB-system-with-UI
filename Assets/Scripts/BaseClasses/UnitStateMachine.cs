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

    [SerializeField] protected AttackHandler myAttack;
    public GameObject attackTarget;
    protected bool actionStarted;
    private float animationSpeed = 20f;
    protected Vector2 startPosition;
    [SerializeField] protected Attack extendingAttack;
    public bool wasExtended;

    [SerializeField] private GameObject damagePopup;
    [SerializeField] private RectTransform battleCanvas;
    [SerializeField] private GameObject dualStatePE;
    protected GameObject dualStateEffect;
    [SerializeField] protected int turnCounter;
    protected readonly int dualStateTurns = 1;

    protected bool alive = true;

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

        // Check if we should end dualState.
        if (turnCounter == 0 && dualStateEffect != null) { 
            Destroy(dualStateEffect);
            stateCharge = 0;
            dualState = false;
            wasExtended = false;
        }
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

            if (dualStateEffect != null) {
                Destroy(dualStateEffect);
            }

            alive = false;

            BSM.battleState = BattleStateMachine.BattleState.VictoryCheck;
        }
    }

    private IEnumerator TimeForAction()
    {
        if (actionStarted) {
            yield break;
        }

        actionStarted = true;

        // If your neighbour is in dualState, repeat their last action.
        Collider2D[] hitColliders = Physics2D.OverlapCircleAll(transform.position, 2f);
        foreach (Collider2D unit in hitColliders) {
            UnitStateMachine script = unit.transform.GetComponent<UnitStateMachine>();

            if (unit.gameObject != gameObject && script.dualState == true) {
                script.StartCoroutine("ExtraAttack");
            }
        }

        Vector2 targetPosition = new Vector2(myAttack.target.transform.position.x, myAttack.target.transform.position.y);
        while (MoveToTarget(targetPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);

        initiative -= BSM.turnThreshold;

        DoDamage(myAttack);

        // Count turns inside of dualState.
        if (dualState == true) {
            if (myAttack.chosenAttack == extendingAttack && wasExtended == false) {
                wasExtended = true;
            }
            else {
                turnCounter--;
            }
        }

        // Add stateCharge and enter dualState.
        stateCharge = Mathf.Clamp(stateCharge + myAttack.chosenAttack.stateCharge, 0, 100);

        if (stateCharge == 100 && dualState == false) {
            dualState = true;
            dualStateEffect = Instantiate(dualStatePE, transform);
            turnCounter = dualStateTurns;
        }

        Vector2 firstPosition = startPosition;
        while (MoveBack(firstPosition)) { yield return null; }

        actionStarted = false;

        turnState = TurnState.Idle;

        BSM.battleState = BattleStateMachine.BattleState.AdvanceTime;
    }

    public IEnumerator ExtraAttack()
    {
        myAttack.target = BSM.enemiesInBattle[Random.Range(0, BSM.enemiesInBattle.Count)];

        Vector2 targetPosition = new Vector2(myAttack.target.transform.position.x, myAttack.target.transform.position.y);
        while (MoveToTarget(targetPosition)) { yield return null; }

        yield return new WaitForSeconds(0.5f);
        
        DoDamage(myAttack);

        Vector2 firstPosition = startPosition;
        while (MoveBack(firstPosition)) { yield return null; }
    }

    private void DoDamage (AttackHandler attackHandler)
    {
        // Calculate attack damage. Double it in dualState.
        float calcDamage = currentATK + attackHandler.chosenAttack.attackDamage;
        if (dualState) {
            calcDamage *= 2;
        }

        // Add tokens and deal damage.
        UnitStateMachine target = attackHandler.target.GetComponent<UnitStateMachine>();
        target.fireTokens += attackHandler.chosenAttack.fireTokens;
        target.waterTokens += attackHandler.chosenAttack.waterTokens;
        target.earthTokens += attackHandler.chosenAttack.earthTokens;
        target.skyTokens += attackHandler.chosenAttack.skyTokens;

        target.TakeDamage(calcDamage);
    }

    private void TakeDamage(float attackDamage)
    {
        float calcDamage = attackDamage - currentDEF;
        currentHP -= calcDamage;
        if (currentHP <= 0) {
            currentHP = 0;
            turnState = TurnState.Dead;
        }

        // Create a damagePopup and place it over the target.
        GameObject textPopup = Instantiate(damagePopup, battleCanvas);

        Vector2 screenPoint = Camera.main.WorldToScreenPoint(transform.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(battleCanvas.GetComponent<RectTransform>(), screenPoint, null, out Vector2 canvasPoint);
        textPopup.GetComponent<RectTransform>().localPosition = canvasPoint;

        textPopup.GetComponent<Text>().text = calcDamage.ToString();
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
