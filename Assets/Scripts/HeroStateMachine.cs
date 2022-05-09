using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HeroStateMachine : UnitStateMachine
{
    protected override void ChooseAction()
    {
        attackTarget = BSM.enemiesInBattle[Random.Range(0, BSM.enemiesInBattle.Count)];
        turnState = TurnState.Acting;
    }
}
