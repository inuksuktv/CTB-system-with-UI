using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroStateMachine : UnitStateMachine
{
    protected override void ChooseAction()
    {
        BSM.heroesToManage.Clear();
        BSM.heroesToManage.Add(gameObject);
        turnState = TurnState.Idle;
    }

    protected override void DieAndCleanup()
    {
        if (!alive) { return; }
        else {
            tag = "DeadUnit";

            BSM.heroesInBattle.Remove(gameObject);
            BSM.combatants.Remove(gameObject);

            GetComponent<SpriteRenderer>().color = Color.black;

            // Recalculate the turnQueue. Is it finally time to grapple with the GUI?

            alive = false;

            BSM.battleState = BattleStateMachine.BattleState.VictoryCheck;
        }
    }
}
