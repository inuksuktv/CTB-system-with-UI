using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UnitStateMachine : BaseUnit
{
    private BattleStateMachine BSM;

    public enum TurnState
    {
        Idle,
        Choosing,
        Acting,
        Dead
    }
    public TurnState turnState;

    void Start()
    {
        turnState = TurnState.Idle;
        BSM = GameObject.Find("BattleManager").GetComponent<BattleStateMachine>();
    }

    void Update()
    {
        switch (turnState) {
            case TurnState.Idle:

                break;

            case TurnState.Choosing:

                Debug.Log("Told " + gameObject  + " to act.");

                break;

            case TurnState.Acting:

                initiative -= BSM.turnThreshold;
                BSM.battleState = BattleStateMachine.BattleState.VictoryCheck;
                turnState = TurnState.Idle;

                break;

            case TurnState.Dead:



                break;
        }
    }
}
