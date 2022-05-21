using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroStateMachine : UnitStateMachine
{
    protected override void ChooseAction()
    {
        BSM.heroesToManage.Clear();
        BSM.heroesToManage.Add(gameObject);

        if (turnCounter == 0 && dualStateEffect != null) {
            Destroy(dualStateEffect);
            stateCharge = 0;
            dualState = false;
            wasExtended = false;
        }

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

            if (dualStateEffect != null) {
                Destroy(dualStateEffect);
            }

            alive = false;

            BSM.battleState = BattleStateMachine.BattleState.VictoryCheck;
        }
    }
}
